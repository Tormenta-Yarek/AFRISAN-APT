using System.Security.Cryptography;
using Afrisan.Nuevo.Api;
using Afrisan.Nuevo.Api.Persistencia;
using Afrisan.Nuevo.Api.PowerBi;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
// Diagnóstico independiente: no inicia HTTP ni modifica datos o permisos.
if (args.Contains("--verificar-supabase", StringComparer.OrdinalIgnoreCase))
{
    Environment.ExitCode = await ConexionSupabase.VerificarAsync(builder.Configuration);
    return;
}
if (args.Contains("--preparar-persistencia"))
{
    try { await PrepararPersistencia.Ejecutar(builder.Configuration); }
    catch (Exception ex) { Console.Error.WriteLine(ex is Npgsql.PostgresException pg ? $"No se pudo preparar PostgreSQL (SQLSTATE {pg.SqlState})." : "No se pudo preparar la persistencia. Revisa conexión, certificado y permisos del propietario."); Environment.ExitCode=1; }
    return;
}
var persistente = !string.Equals(builder.Configuration["Persistencia:Modo"], "Memoria", StringComparison.OrdinalIgnoreCase);
if (!persistente && !builder.Environment.IsDevelopment())
    throw new InvalidOperationException("El modo Memoria solo está disponible en Development.");

byte[] firma;
if (persistente)
{
    var settings=PersistenciaOptions.Load(builder.Configuration,builder.Environment);
    firma=settings.JwtKey;
    builder.Services.AddSingleton(settings);
    builder.Services.AddDbContextFactory<AfrisanDb>(o=>o.UseNpgsql(settings.ConnectionString));
    builder.Services.AddSingleton<PgDatabase>();
    builder.Services.AddSingleton<PgUsuarios>();
    builder.Services.AddSingleton<PgInventario>();
    builder.Services.AddSingleton<PgTrabajo>();
    builder.Services.AddSingleton<PgPowerBi>();
    builder.Services.AddSingleton(PowerBiOptions.Load(builder.Configuration));
    builder.Services.AddSingleton<FotosPrivadas>();
    builder.Services.AddSingleton(StorageOptions.Load(builder.Configuration,builder.Environment));
    builder.Services.AddHttpClient<SupabaseFotos>(client=>client.Timeout=TimeSpan.FromSeconds(30))
        .ConfigurePrimaryHttpMessageHandler(()=>new HttpClientHandler{AllowAutoRedirect=false});
    builder.Logging.AddFilter("System.Net.Http.HttpClient.SupabaseFotos",LogLevel.Warning);
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command",LogLevel.Warning);
}
else
{
    firma=RandomNumberGenerator.GetBytes(64);
    builder.Services.AddSingleton(new SesionesDemo(firma));
    builder.Services.AddSingleton<InventarioStore>();
    builder.Services.AddSingleton<TrabajoStore>();
}
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 6 * 1024 * 1024);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<PgExceptionHandler>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = persistente ? PgUsuarios.Issuer : SesionesDemo.Issuer,
        ValidateAudience = true, ValidAudience = persistente ? PgUsuarios.Audience : SesionesDemo.Audience,
        ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(firma)
    };
    if(persistente) o.Events=new JwtBearerEvents
    {
        OnTokenValidated=async context=>{
            if(!int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier),out var id)) {context.Fail("Sesión inválida.");return;}
            var user=await context.HttpContext.RequestServices.GetRequiredService<PgUsuarios>().User(id);
            if(user is null||!user.Activo||context.Principal?.FindFirstValue("version")!=user.Version.ToString()||context.Principal.FindFirstValue(ClaimTypes.Role)!=user.Rol)
                context.Fail("La cuenta o sesión ya no está vigente.");
        }
    };
});
builder.Services.AddAuthorization();
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() is { Length: > 0 } cfgOrigins ? cfgOrigins : ["http://localhost:5286"];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = persistente ? "AFRISAN · PostgreSQL" : "AFRISAN · Memoria", Version = "v1" });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
    });
});
var app = builder.Build();
if(persistente)
{
    await app.Services.GetRequiredService<PersistenciaOptions>().Inicializar(app.Services.GetRequiredService<PgDatabase>());
    if(args.Contains("--verificar-powerbi"))
    {
        try
        {
            int actor;
            await using(var u=await app.Services.GetRequiredService<PgDatabase>().Open())
            {
                await u.Exec("SET TRANSACTION READ ONLY");
                actor=await u.One<int>("SELECT u.\"Id_Usuario\" AS \"Value\" FROM afrisan.\"Usuario\" u JOIN afrisan.\"Rol\" r USING(\"Id_Rol\") WHERE u.\"Activo\" AND r.\"Nombre_Rol\"='Administrador' ORDER BY u.\"Id_Usuario\" LIMIT 1");
            }
            var datos=await app.Services.GetRequiredService<PgPowerBi>().Datos(actor);
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new {estado="CORRECTO",soloLectura=true,
                datos.GeneradoUtc,cilindros=datos.Inventario.Length,ordenes=datos.Ordenes.Length,
                detalles=datos.Detalle.Length,operaciones=datos.Operaciones.Length,
                despachos=datos.Despachos.Length,incidencias=datos.Incidencias.Length,
                informeConfigurado=app.Services.GetRequiredService<PowerBiOptions>().Publica().Configurado}));
        }
        catch{Console.Error.WriteLine("No se pudieron verificar las consultas Power BI. Revisa conexión, permisos y administrador activo; no se modificaron registros.");Environment.ExitCode=1;}
        return;
    }
    if(args.Contains("--configurar-storage"))
    {
        try{await ConfigurarStorage.Ejecutar(builder.Configuration,builder.Environment,app.Services.GetRequiredService<PgDatabase>(),app.Services.GetRequiredService<FotosPrivadas>());}
        catch(Exception ex){Console.Error.WriteLine(ex is ReglaInventarioException or InvalidOperationException?ex.Message:"No se pudo configurar Storage. Revisa conexión y clave privada; no se activó el cambio de proveedor.");Environment.ExitCode=1;}
        return;
    }
    if(args.Contains("--verificar-storage"))
    {
        try{
            var storage=app.Services.GetRequiredService<StorageOptions>();
            if(!storage.Nube)throw new InvalidOperationException("Las fotografías siguen en disco privado; falta configurar Storage.");
            await app.Services.GetRequiredService<SupabaseFotos>().VerificarBucket();
            Console.WriteLine($"Storage Supabase activo; bucket privado {storage.Bucket}, 4 MiB, JPEG/PNG/WebP. La API conserva la autorización por OT y rol.");
        }catch(Exception ex){Console.Error.WriteLine(ex is ReglaInventarioException or InvalidOperationException?ex.Message:"No se pudo verificar Storage.");Environment.ExitCode=1;}
        return;
    }
    if(args.Contains("--verificar-persistencia"))
    {
        await using var unit=await app.Services.GetRequiredService<PgDatabase>().Open();
        await unit.Exec("SET TRANSACTION READ ONLY");
        var total=await unit.One<long>("SELECT count(*) AS \"Value\" FROM afrisan.\"Usuario\"");
        var admins=await unit.One<long>("SELECT count(*) AS \"Value\" FROM afrisan.\"Usuario\" u JOIN afrisan.\"Rol\" r USING(\"Id_Rol\") WHERE u.\"Activo\" AND r.\"Nombre_Rol\"='Administrador'");
        Console.WriteLine($"Persistencia PostgreSQL preparada con identidad afrisan_api. Usuarios: {total}; administradores activos: {admins}.");
        Console.WriteLine("Transacción de diagnóstico de solo lectura; no se crean registros.");
        return;
    }
    if(args.Contains("--crear-administrador"))
    {
        try {await PrepararPersistencia.CrearAdministrador(app.Services.GetRequiredService<PgUsuarios>());}
        catch(Exception ex) {Console.Error.WriteLine(ex is ReglaInventarioException or System.ComponentModel.DataAnnotations.ValidationException or InvalidOperationException?ex.Message:"No se pudo crear el administrador. Revisa los datos; no se confirmó el alta.");Environment.ExitCode=1;}
        return;
    }
}
app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
if(persistente) { app.MapPersistencia(); app.MapPowerBi(); }
else {app.MapInventario();app.MapTrabajo();}
app.Logger.LogInformation(persistente?"AFRISAN: PostgreSQL, usuarios persistidos y evidencias privadas.":"AFRISAN: modo explícito de revisión en memoria.");
app.Run();
