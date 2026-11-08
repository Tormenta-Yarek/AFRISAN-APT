using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Npgsql;

namespace Afrisan.Nuevo.Api.Persistencia;

public sealed class StorageOptions
{
    public bool Nube { get; init; }
    public required Uri Url { get; init; }
    public required string Bucket { get; init; }
    public string SecretKey { get; init; }="";
    public static StorageOptions Load(IConfiguration config,IHostEnvironment environment,bool configurar=false,string? clave=null)
    {
        var provider=config["Storage:Proveedor"]??"Disco";
        if(provider is not("Disco" or "Supabase"))throw new InvalidOperationException("Proveedor de fotografías desconocido.");
        var nube=configurar||provider=="Supabase";
        var owner=config.GetConnectionString("Supabase");
        var ownerUser=string.IsNullOrWhiteSpace(owner)?"":new NpgsqlConnectionStringBuilder(owner).Username??"";
        var project=ownerUser.Contains('.')?ownerUser[(ownerUser.IndexOf('.')+1)..]:"";
        var raw=config["Storage:Url"]??(project.Length>0?$"https://{project}.supabase.co":"https://sin-configurar.supabase.co");
        if(!Uri.TryCreate(raw,UriKind.Absolute,out var url)||url.UserInfo.Length>0||url.AbsolutePath!="/"||url.Query.Length>0||url.Fragment.Length>0)
            throw new InvalidOperationException("La URL de Storage debe ser la raíz del proyecto Supabase.");
        var local=environment.IsDevelopment()&&config.GetValue<bool>("Persistencia:PruebasLocales")&&url.Host is "127.0.0.1" or "localhost";
        if(nube&&!local&&(url.Scheme!="https"||project.Length==0||url.Host!=$"{project}.supabase.co"))
            throw new InvalidOperationException("Storage debe usar HTTPS y el mismo proyecto de la base configurada.");
        var bucket=config["Storage:Bucket"]??"afrisan-evidencias";
        if(!Regex.IsMatch(bucket,"^[a-z0-9][a-z0-9-]{2,62}$")||bucket=="servidor-privado")throw new InvalidOperationException("Nombre de bucket de fotografías inválido.");
        var key=clave??config["Storage:SecretKey"]??"";
        if(nube)ValidarClave(key);
        return new(){Nube=nube,Url=url,Bucket=bucket,SecretKey=key};
    }
    public static void ValidarClave(string key)
    {
        if(key.StartsWith("sb_secret_",StringComparison.Ordinal)&&key.Length>20)return;
        if(key.StartsWith("eyJ",StringComparison.Ordinal))
        {
            try {var part=key.Split('.')[1];part=part.Replace('-','+').Replace('_','/');part=part.PadRight((part.Length+3)/4*4,'=');using var json=JsonDocument.Parse(Convert.FromBase64String(part));if(json.RootElement.GetProperty("role").GetString()=="service_role")return;}catch(Exception ex) when(ex is FormatException or JsonException or IndexOutOfRangeException or KeyNotFoundException){ }
        }
        throw new InvalidOperationException("Introduce una clave secreta de servidor sb_secret_ (o service_role heredada), no una clave pública ni la contraseña PostgreSQL.");
    }
}

// REST solo desde el backend. No se crean enlaces públicos o firmados para el navegador.
public sealed class SupabaseFotos(HttpClient client,StorageOptions options)
{
    private const int MaxBytes=4*1024*1024;
    private static readonly string[] Mimes=["image/png","image/jpeg","image/webp"];
    private HttpRequestMessage Request(HttpMethod method,string path,bool auth=true)
    {
        var request=new HttpRequestMessage(method,new Uri(options.Url,"storage/v1/"+path));
        if(auth)
        {
            request.Headers.Add("apikey",options.SecretKey);
            // Las claves sb_secret no son JWT; solo viajan en apikey.
            if(options.SecretKey.StartsWith("eyJ",StringComparison.Ordinal))request.Headers.Authorization=new("Bearer",options.SecretKey);
        }
        return request;
    }
    private async Task<HttpResponseMessage> Send(HttpRequestMessage request)
    {
        try{return await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead);}
        catch(Exception ex)when(ex is HttpRequestException or OperationCanceledException){throw new ReglaInventarioException("No se pudo contactar el almacenamiento privado. El guardado de la foto no fue confirmado.",503);}
    }
    private static ReglaInventarioException Error(HttpStatusCode status)=>new(status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
        ?"Storage rechazó las credenciales del servidor. Revisa la configuración privada."
        :"Storage no confirmó la operación. Conserva el borrador y vuelve a intentar.",503);
    public async Task VerificarBucket(bool crear=false)
    {
        using var request=Request(HttpMethod.Get,"bucket/"+options.Bucket);
        using var response=await Send(request);
        if(response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest&&crear)
        {
            using var create=Request(HttpMethod.Post,"bucket");
            create.Content=JsonContent.Create(new{id=options.Bucket,name=options.Bucket,@public=false,file_size_limit=MaxBytes,allowed_mime_types=Mimes});
            using var created=await Send(create);
            if(!created.IsSuccessStatusCode&&created.StatusCode!=HttpStatusCode.Conflict)throw Error(created.StatusCode);
            await VerificarBucket();return;
        }
        if(!response.IsSuccessStatusCode)throw Error(response.StatusCode);
        try
        {
            using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());var bucket=json.RootElement;
            if(bucket.GetProperty("id").GetString()!=options.Bucket||bucket.GetProperty("public").GetBoolean())
                throw new ReglaInventarioException("El bucket de evidencias no es privado. No se habilitaron las fotografías en la nube.",503);
            if(!bucket.TryGetProperty("file_size_limit",out var size)||!size.TryGetInt64(out var limit)||limit!=MaxBytes)
                throw new ReglaInventarioException("El bucket debe tener un límite de 4 MiB por fotografía.",503);
            if(!bucket.TryGetProperty("allowed_mime_types",out var types)||types.ValueKind!=JsonValueKind.Array||!types.EnumerateArray().Select(x=>x.GetString()).Order().SequenceEqual(Mimes.Order()))
                throw new ReglaInventarioException("El bucket debe admitir únicamente JPEG, PNG y WebP.",503);
        }
        catch(Exception ex)when(ex is JsonException or KeyNotFoundException or InvalidOperationException){throw new ReglaInventarioException("La configuración del bucket privado no se pudo verificar.",503);}
    }
    internal static string RutaObjeto(string path)
    {
        if(path.Length is <1 or >700||path.Split('/').Any(s=>s is "" or "." or "..")||!Regex.IsMatch(path,"^[a-zA-Z0-9/_\\.-]+$"))throw new ReglaInventarioException("Ruta de fotografía inválida.");
        return string.Join('/',path.Split('/').Select(Uri.EscapeDataString));
    }
    public async Task Guardar(string path,string mime,byte[] bytes,string hash)
    {
        await VerificarBucket();
        using var request=Request(HttpMethod.Post,$"object/{options.Bucket}/{RutaObjeto(path)}");
        request.Headers.Add("x-upsert","false");request.Content=new ByteArrayContent(bytes);request.Content.Headers.ContentType=new(mime);
        request.Headers.CacheControl=new(){NoStore=true};
        using var response=await Send(request);
        if(!response.IsSuccessStatusCode)
        {
            var duplicate=response.StatusCode==HttpStatusCode.Conflict;
            if(response.StatusCode==HttpStatusCode.BadRequest)
            {
                try {using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());duplicate=json.RootElement.TryGetProperty("error",out var err)&&err.GetString()=="Duplicate"||json.RootElement.TryGetProperty("code",out var code)&&code.GetString() is "Duplicate" or "ResourceAlreadyExists";}
                catch(JsonException){ }
            }
            if(!duplicate)throw Error(response.StatusCode);
        }
        var saved=await Leer(path,mime,hash);
        if(saved.Length!=bytes.Length)throw new ReglaInventarioException("La foto almacenada no coincide con la enviada.",503);
    }
    public async Task<byte[]> Leer(string path,string mime,string hash)
    {
        await VerificarBucket();
        using var request=Request(HttpMethod.Get,$"object/authenticated/{options.Bucket}/{RutaObjeto(path)}");
        using var response=await Send(request);
        if(!response.IsSuccessStatusCode)throw Error(response.StatusCode);
        if(response.Content.Headers.ContentLength>MaxBytes||response.Content.Headers.ContentType?.MediaType!=mime)throw new ReglaInventarioException("El tamaño o formato de la foto almacenada no coincide.",503);
        await using var source=await response.Content.ReadAsStreamAsync();using var output=new MemoryStream();var buffer=new byte[65536];int read;
        while((read=await source.ReadAsync(buffer))>0){if(output.Length+read>MaxBytes)throw new ReglaInventarioException("La foto almacenada supera el tamaño permitido.",503);output.Write(buffer,0,read);}
        var bytes=output.ToArray();
        if(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()!=hash)throw new ReglaInventarioException("La integridad de la fotografía privada no coincide.",503);
        return bytes;
    }
    public async Task ComprobarPrivacidad(string path)
    {
        using var request=Request(HttpMethod.Get,$"object/public/{options.Bucket}/{RutaObjeto(path)}",false);
        using var response=await Send(request);
        if(response.IsSuccessStatusCode)throw new ReglaInventarioException("La prueba detectó acceso público a la fotografía. No se activó Storage.",503);
        if(response.StatusCode is not(HttpStatusCode.NotFound or HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden))throw Error(response.StatusCode);
    }
    public async Task EliminarPrueba(string path)
    {
        if(!path.StartsWith("diagnostico/",StringComparison.Ordinal))throw new InvalidOperationException("Solo se eliminan los objetos temporales de diagnóstico.");
        using var request=Request(HttpMethod.Delete,"object/"+options.Bucket);request.Content=JsonContent.Create(new{prefixes=new[]{path}});
        using var response=await Send(request);if(!response.IsSuccessStatusCode)throw Error(response.StatusCode);
    }
}
