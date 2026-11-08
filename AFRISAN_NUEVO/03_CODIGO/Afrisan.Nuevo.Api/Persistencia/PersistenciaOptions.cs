using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Afrisan.Nuevo.Api.Persistencia;

public sealed class PersistenciaOptions
{
    public required string ConnectionString { get; init; }
    public required byte[] JwtKey { get; init; }
    public required string FotosPath { get; init; }
    public string Instancia { get; private set; }="";
    public static PersistenciaOptions Load(IConfiguration config,IHostEnvironment environment)
    {
        var connection=config.GetConnectionString("AfrisanRuntime");
        var key=config["Persistencia:JwtKey"];
        var storage=config["Persistencia:FotosPath"];
        if(string.IsNullOrWhiteSpace(connection)||string.IsNullOrWhiteSpace(key)||string.IsNullOrWhiteSpace(storage))
            throw new InvalidOperationException("Falta preparar la persistencia. Ejecuta Preparar-Persistencia.ps1 para crear la conexión restringida y el administrador inicial.");
        var parsed=new NpgsqlConnectionStringBuilder(connection){IncludeErrorDetail=false,PersistSecurityInfo=false};
        var pruebas=environment.IsDevelopment()&&config.GetValue<bool>("Persistencia:PruebasLocales")&&parsed.Host is "localhost" or "127.0.0.1";
        if(!pruebas) parsed.SslMode=SslMode.VerifyFull;
        parsed.Timeout=10;parsed.CommandTimeout=30;
        var bytes=Convert.FromBase64String(key);
        if(bytes.Length<64) throw new InvalidOperationException("La clave de firma persistente debe contener al menos 64 bytes.");
        var full=Path.GetFullPath(storage);
        if(full.Contains(Path.DirectorySeparatorChar+"wwwroot"+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||Path.GetFileName(full).Equals("wwwroot",StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("El almacenamiento de fotos debe estar fuera de wwwroot.");
        Directory.CreateDirectory(full);
        return new(){ConnectionString=parsed.ConnectionString,JwtKey=bytes,FotosPath=full};
    }
    public async Task Inicializar(PgDatabase database)
    {
        await using var u=await database.Open();
        var role=await u.One<string>("SELECT current_user::text AS \"Value\"");
        if(role!="afrisan_api") throw new InvalidOperationException("La API persistente requiere la identidad restringida afrisan_api.");
        var oid=await u.One<long>("SELECT oid::bigint AS \"Value\" FROM pg_namespace WHERE nspname='afrisan'");
        // Cambia si se reinstala el esquema; permanece entre reinicios y procesos.
        var cs=new NpgsqlConnectionStringBuilder(ConnectionString);
        Instancia=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{cs.Host}/{cs.Database}/afrisan/{oid}"))).ToLowerInvariant();
        await u.Query<string>("SELECT \"HuellaCreacion\" AS \"Value\" FROM afrisan.\"OrdenTrabajo\" LIMIT 0");
        await u.Query<string>("SELECT \"DatosBorrador\"::text AS \"Value\" FROM afrisan.\"OperacionTerreno\" LIMIT 0");
    }
}

public static class PrepararPersistencia
{
    public static async Task Ejecutar(IConfiguration config)
    {
        var owner=config.GetConnectionString("Supabase")??throw new InvalidOperationException("Primero ejecuta Configurar-Supabase.ps1.");
        var parsed=new NpgsqlConnectionStringBuilder(owner){SslMode=SslMode.VerifyFull,IncludeErrorDetail=false,PersistSecurityInfo=false};
        await using var connection=new NpgsqlConnection(parsed.ConnectionString);await connection.OpenAsync();
        using var stream=typeof(PrepararPersistencia).Assembly.GetManifestResourceStream("AFRISAN.Persistencia.sql")!;
        var sql=await new StreamReader(stream).ReadToEndAsync();
        sql=string.Join('\n',sql.Split('\n').Where(l=>l.Trim() is not("BEGIN;" or "COMMIT;")));
        var previous=config.GetConnectionString("AfrisanRuntime");
        var saved=string.IsNullOrWhiteSpace(previous)?null:new NpgsqlConnectionStringBuilder(previous);
        var runtimeUser="afrisan_api";
        if(parsed.Host?.EndsWith(".pooler.supabase.com",StringComparison.OrdinalIgnoreCase)==true)
        {
            var poolerUser=parsed.Username??"";
            var separator=poolerUser.IndexOf('.');
            if(separator<0||separator==poolerUser.Length-1) throw new InvalidOperationException("El usuario del Session pooler debe incluir el identificador del proyecto.");
            runtimeUser+="."+poolerUser[(separator+1)..];
        }
        var password=saved is not null&&(saved.Username==runtimeUser||saved.Username=="afrisan_api")&&saved.Host==parsed.Host&&saved.Database==parsed.Database&&!string.IsNullOrWhiteSpace(saved.Password)
            ?saved.Password:Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        await using(var tx=await connection.BeginTransactionAsync())
        {
            await using(var cmd=new NpgsqlCommand(sql,connection,tx)) await cmd.ExecuteNonQueryAsync();
            // format(%L) produce un literal seguro; la contraseña nunca va en argumentos ni salida.
            await using var literal=new NpgsqlCommand("SELECT format('ALTER ROLE afrisan_api LOGIN PASSWORD %L',@password)",connection,tx);
            literal.Parameters.AddWithValue("password",password);
            var alter=(string)(await literal.ExecuteScalarAsync())!;
            await using(var cmd=new NpgsqlCommand(alter,connection,tx)) await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        }
        parsed.Username=runtimeUser;parsed.Password=password;
        var root=Repositorio();
        SaveSecret("ConnectionStrings","AfrisanRuntime",parsed.ConnectionString);
        if(string.IsNullOrWhiteSpace(config["Persistencia:JwtKey"])) SaveSecret("Persistencia","JwtKey",Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));
        if(string.IsNullOrWhiteSpace(config["Persistencia:FotosPath"])) SaveSecret("Persistencia","FotosPath",Path.Combine(root,".local","evidencias"));
        SaveSecret("Persistencia","Modo","PostgreSQL");
        Console.WriteLine("Actualización 07 aplicada sin borrar datos. Conexión restringida y firma estable guardadas en User Secrets.");
        Console.WriteLine("El siguiente paso es crear el administrador inicial con --crear-administrador.");
    }
    public static string Repositorio()
    {
        for(var d=new DirectoryInfo(AppContext.BaseDirectory);d is not null;d=d.Parent)
            if(Directory.Exists(Path.Combine(d.FullName,"AFRISAN_NUEVO"))) return d.FullName;
        return Directory.GetCurrentDirectory();
    }
    internal static void SaveSecret(string section,string name,string value)
    {
        var dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Microsoft","UserSecrets","Afrisan-Nuevo-Api-Supabase-2026");
        Directory.CreateDirectory(dir);var file=Path.Combine(dir,"secrets.json");
        var root=File.Exists(file)?JsonNode.Parse(File.ReadAllText(file))!.AsObject():new JsonObject();
        root.Remove(section+":"+name);
        var child=root[section] as JsonObject;
        if(child is null) {child=new JsonObject();root[section]=child;}
        child[name]=value;
        var temp=Path.Combine(dir,Guid.NewGuid().ToString("N")+".tmp");
        File.WriteAllText(temp,root.ToJsonString(new(){WriteIndented=true}),new UTF8Encoding(false));
        File.Move(temp,file,true);
    }
    public static async Task CrearAdministrador(PgUsuarios usuarios)
    {
        Console.Write("Nombre: ");var nombre=Console.ReadLine()??"";
        Console.Write("Apellido: ");var apellido=Console.ReadLine()??"";
        Console.Write("Correo del administrador: ");var correo=Console.ReadLine()??"";
        Console.Write("Contraseña (mínimo 12 caracteres, entrada oculta): ");var password=LeerPassword();
        Console.Write("Repetir contraseña: ");var repetir=LeerPassword();
        if(password!=repetir) throw new InvalidOperationException("Las contraseñas no coinciden.");
        var request=new Afrisan.Nuevo.Contratos.CrearUsuarioRequest{Nombre=nombre,Apellido=apellido,Correo=correo,Password=password,Rol="Administrador"};
        System.ComponentModel.DataAnnotations.Validator.ValidateObject(request,new(request),true);
        await usuarios.Crear(request,null,true);
        Console.WriteLine("Administrador creado. Ya puedes entrar a AFRISAN con tu correo y contraseña.");
    }
    internal static string LeerPassword()
    {
        if(Console.IsInputRedirected) return Console.ReadLine()??"";
        var text=new StringBuilder();
        while(true) {var key=Console.ReadKey(true);if(key.Key==ConsoleKey.Enter) break;if(key.Key==ConsoleKey.Backspace){if(text.Length>0)text.Length--;}else if(!char.IsControl(key.KeyChar))text.Append(key.KeyChar);}
        Console.WriteLine();return text.ToString();
    }
}
