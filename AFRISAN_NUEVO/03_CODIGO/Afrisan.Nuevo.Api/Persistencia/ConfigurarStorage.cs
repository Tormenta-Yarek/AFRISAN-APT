using System.Security.Cryptography;
using System.Text.Json;

namespace Afrisan.Nuevo.Api.Persistencia;

public sealed record FotoReferencia(string Ruta,string Mime,string Sha256,long Bytes);
public static class ConfigurarStorage
{
    internal static async Task<FotoReferencia[]> ReferenciasLocales(PgDatabase database)
    {
        await using var unit=await database.Open();await unit.Exec("SET TRANSACTION READ ONLY");
        var rows=(await unit.Query<FotoReferencia>("""
            SELECT "RutaObjeto" AS "Ruta","ContentType" AS "Mime","SHA256" AS "Sha256","Bytes"
            FROM afrisan."EvidenciaPesaje" WHERE "BucketPrivado"='servidor-privado'
            """)).ToList();
        foreach(var json in await unit.Query<string>("SELECT \"FotosBorrador\"::text AS \"Value\" FROM afrisan.\"OperacionTerreno\""))
            foreach(var photo in JsonSerializer.Deserialize<FotoArchivo[]>(json,new JsonSerializerOptions(JsonSerializerDefaults.Web))??[])
                if(photo.Bucket=="servidor-privado")rows.Add(new(photo.Ruta,photo.ContentType,photo.Sha256,photo.Bytes));
        return rows.GroupBy(r=>r.Ruta).Select(group=>{
            var first=group.First();if(group.Any(r=>r.Sha256!=first.Sha256||r.Mime!=first.Mime||r.Bytes!=first.Bytes))throw new ReglaInventarioException("Hay referencias incompatibles para un archivo local. No se activó Storage.",409);return first;
        }).ToArray();
    }
    public static async Task Ejecutar(IConfiguration config,IHostEnvironment environment,PgDatabase database,FotosPrivadas locales,Action<string,string,string>? guardar=null)
    {
        var key=config["Storage:SecretKey"];
        if(string.IsNullOrWhiteSpace(key))
        {
            Console.WriteLine("En Supabase: Settings > API Keys > Secret keys. Usa una clave de servidor dedicada a AFRISAN.");
            Console.WriteLine("La clave permite acceso privilegiado al proyecto; solo se usará en el backend y se guardará en User Secrets.");
            Console.Write("Clave secreta de servidor (entrada oculta): ");key=PrepararPersistencia.LeerPassword();
        }
        key=key.Trim();
        var options=StorageOptions.Load(config,environment,true,key);
        using var handler=new HttpClientHandler{AllowAutoRedirect=false};using var http=new HttpClient(handler){Timeout=TimeSpan.FromSeconds(30)};
        var storage=new SupabaseFotos(http,options);await storage.VerificarBucket(true);
        var refs=await ReferenciasLocales(database);var count=0;
        foreach(var photo in refs)
        {
            if(locales.ExisteLocal(photo.Ruta))
            {
                var bytes=await locales.LeerLocal(photo.Ruta,photo.Sha256);
                if(bytes.Length!=photo.Bytes)throw new ReglaInventarioException("El tamaño de una foto local no coincide. Se conservó el modo anterior.",503);
                await storage.Guardar(photo.Ruta,photo.Mime,bytes,photo.Sha256);
            }
            else await storage.Leer(photo.Ruta,photo.Mime,photo.Sha256); // Reanudación de una copia ya verificada.
            count++;
        }
        // Solo se crea y elimina un objeto temporal propio. Ninguna evidencia se borra.
        var prueba="diagnostico/"+Guid.NewGuid().ToString("N")+".png";
        var sample=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9Zl1sAAAAASUVORK5CYII=");
        try
        {
            await storage.Guardar(prueba,"image/png",sample,Convert.ToHexString(SHA256.HashData(sample)).ToLowerInvariant());
            await storage.ComprobarPrivacidad(prueba);
        }
        finally{await storage.EliminarPrueba(prueba);}
        guardar??=PrepararPersistencia.SaveSecret;
        guardar("Storage","Url",options.Url.AbsoluteUri);
        guardar("Storage","Bucket",options.Bucket);
        guardar("Storage","SecretKey",key);
        guardar("Storage","Proveedor","Supabase"); // Activar únicamente tras verificar todas las copias.
        Console.WriteLine($"Storage privado configurado. Fotografías locales referenciadas copiadas/verificadas: {count}.");
        Console.WriteLine("Carga, descarga, hash y rechazo del acceso público comprobados. Los originales locales se conservan.");
        Console.WriteLine("Reinicia la API para usar Storage en las nuevas fotografías.");
    }
}
