using System.Security.Cryptography;
using Afrisan.Nuevo.Contratos;
namespace Afrisan.Nuevo.Api.Persistencia;

public sealed record FotoArchivo(Guid Id,string Grupo,string Codigo,string Nombre,string ContentType,
    long Bytes,string Ruta,string Sha256,DateTimeOffset Fecha,string Usuario,string Bucket="servidor-privado")
{
    public FotoResponse Response()=>new(Id,Grupo,Codigo,Nombre,ContentType,Bytes,Fecha,Usuario);
}

// Almacenamiento privado del servidor, fuera de wwwroot y sin URLs públicas.
// Storage privado conserva las rutas/hash. Disco se mantiene para desarrollo
// y para copiar los originales durante la activación, sin borrarlos.
public sealed class FotosPrivadas(PersistenciaOptions options,StorageOptions storage,SupabaseFotos nube)
{
    public async Task<FotoArchivo> Guardar(Guid ot,Guid id,string grupo,string codigo,string nombre,string mime,string base64,string usuario,FotoArchivo? existente=null)
    {
        if(id==Guid.Empty||string.IsNullOrWhiteSpace(nombre)||nombre.Length>150) throw new ReglaInventarioException("Identificador o nombre de fotografía inválido.");
        byte[] bytes;
        try { bytes=Convert.FromBase64String(base64); } catch(FormatException) {throw new ReglaInventarioException("La fotografía no contiene Base64 válido.");}
        if(bytes.Length is <12 or >4*1024*1024) throw new ReglaInventarioException("La foto debe tener entre 12 bytes y 4 MiB.");
        var valid=mime switch {
            "image/png"=>bytes.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}),
            "image/jpeg"=>bytes[0]==255&&bytes[1]==216&&bytes[2]==255,
            "image/webp"=>bytes.AsSpan(0,4).SequenceEqual("RIFF"u8)&&bytes.AsSpan(8,4).SequenceEqual("WEBP"u8), _=>false};
        if(!valid) throw new ReglaInventarioException("El contenido de la imagen no coincide con su tipo permitido.");
        var sha=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var limpio=Path.GetFileName(nombre.Replace('\\','/'));
        if(existente is not null)
        {
            if(existente.Id!=id||existente.Grupo!=grupo||existente.Codigo!=codigo||existente.Nombre!=limpio||existente.ContentType!=mime||existente.Bytes!=bytes.Length||existente.Sha256!=sha)
                throw new ReglaInventarioException("El UUID de la fotografía ya corresponde a otro contenido.",409);
            await Leer(existente.Ruta,mime,sha,existente.Bucket);return existente;
        }
        var ruta=$"{options.Instancia}/{ot:N}/{id:N}-{sha}.bin";
        if(storage.Nube)
        {
            await nube.Guardar(ruta,mime,bytes,sha);
            return new(id,grupo,codigo,limpio,mime,bytes.Length,ruta,sha,DateTimeOffset.UtcNow,usuario,storage.Bucket);
        }
        var target=Ruta(ruta);Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var temporal=target+"."+Guid.NewGuid().ToString("N")+".tmp";
        try {
            await File.WriteAllBytesAsync(temporal,bytes);
            try{File.Move(temporal,target,false);}
            catch(IOException) when(File.Exists(target)) {
                var previous=await File.ReadAllBytesAsync(target);
                if(!CryptographicOperations.FixedTimeEquals(SHA256.HashData(previous),SHA256.HashData(bytes)))throw new ReglaInventarioException("La fotografía almacenada no coincide.",409);
            }
        } finally {if(File.Exists(temporal))File.Delete(temporal);}
        return new(id,grupo,codigo,limpio,mime,bytes.Length,ruta,sha,DateTimeOffset.UtcNow,usuario);
    }
    public async Task<(byte[] Bytes,string Mime)> Leer(string ruta,string mime,string sha,string bucket="servidor-privado")
    {
        if(storage.Nube)
        {
            // Alias histórico: los archivos locales referenciados se copian al
            // bucket nuevo antes de activar la nube; no se reescriben hechos inmutables.
            if(bucket!=storage.Bucket&&bucket!="servidor-privado")throw new ReglaInventarioException("Bucket de evidencia no reconocido.",503);
            return(await nube.Leer(ruta,mime,sha),mime);
        }
        if(bucket!="servidor-privado")throw new ReglaInventarioException("Esta evidencia requiere Storage privado configurado.",503);
        return(await LeerLocal(ruta,sha),mime);
    }
    internal bool ExisteLocal(string ruta)=>File.Exists(Ruta(ruta));
    internal async Task<byte[]> LeerLocal(string ruta,string sha)
    {
        var target=Ruta(ruta);
        if(!File.Exists(target)) throw new ReglaInventarioException("El archivo privado no está disponible en el almacenamiento del servidor.",503);
        var bytes=await File.ReadAllBytesAsync(target);
        if(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()!=sha) throw new ReglaInventarioException("La integridad del archivo privado no coincide.",503);
        return bytes;
    }
    private string Ruta(string relative)
    {
        var root=Path.GetFullPath(options.FotosPath).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
        var path=Path.GetFullPath(Path.Combine(root,relative));
        if(!path.StartsWith(root,StringComparison.OrdinalIgnoreCase)||Path.IsPathRooted(relative)) throw new ReglaInventarioException("Ruta privada inválida.");
        return path;
    }
}
