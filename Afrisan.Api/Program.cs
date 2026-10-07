using Afrisan.Api.Data;
using Afrisan.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);

// Permitir que la API escuche desde el computador
// y desde otros dispositivos de la red local.
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(5141);
    });
}

// =====================================================
// BASE DE DATOS
// =====================================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "No se encontró la cadena de conexión 'DefaultConnection'. " +
        "Revisa la configuración de Afrisan.Api."
    );
}

builder.Services.AddDbContext<AfrisanDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// =====================================================
// ASP.NET CORE IDENTITY
// =====================================================

builder.Services
    .AddIdentity<Usuario, IdentityRole>(options =>
    {
        // Cada usuario deberá tener un correo único.
        options.User.RequireUniqueEmail = true;

        // Reglas de contraseña.
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;

        // Bloqueo después de intentos fallidos.
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AfrisanDbContext>()
    .AddDefaultTokenProviders();

// =====================================================
// CORS
// =====================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AfrisanWeb", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (!Uri.TryCreate(
                origin,
                UriKind.Absolute,
                out var uri))
            {
                return false;
            }

            bool puertoPermitido =
                (uri.Scheme == "http" && uri.Port == 5226) ||
                (uri.Scheme == "https" && uri.Port == 7083);

            if (!puertoPermitido)
            {
                return false;
            }

            // Acceso desde localhost
            if (uri.Host.Equals(
                "localhost",
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Acceso desde IP
            if (!IPAddress.TryParse(uri.Host, out var ip))
            {
                return false;
            }

            // Loopback
            if (IPAddress.IsLoopback(ip))
            {
                return true;
            }

            // Solo IPv4
            if (ip.AddressFamily != AddressFamily.InterNetwork)
            {
                return false;
            }

            byte[] bytes = ip.GetAddressBytes();

            // Redes privadas:
            // 10.0.0.0/8
            // 172.16.0.0/12
            // 192.168.0.0/16
            return
                bytes[0] == 10 ||
                (bytes[0] == 172 &&
                 bytes[1] >= 16 &&
                 bytes[1] <= 31) ||
                (bytes[0] == 192 &&
                 bytes[1] == 168);
        })
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

// =====================================================
// CONTROLADORES
// =====================================================

builder.Services.AddControllers();

// =====================================================
// SWAGGER
// =====================================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =====================================================
// CONSTRUIR APLICACIÓN
// =====================================================

var app = builder.Build();

// =====================================================
// PIPELINE HTTP
// =====================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseRouting();

// Permitir comunicación con Afrisan.Web
app.UseCors("AfrisanWeb");

// Primero autenticación, después autorización.
app.UseAuthentication();
app.UseAuthorization();

// Controladores de la API
app.MapControllers();

// =====================================================
// CREAR ROLES Y ADMINISTRADOR INICIAL
// =====================================================

using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.InicializarAsync(
        scope.ServiceProvider,
        app.Configuration);
}

// =====================================================
// INICIAR API
// =====================================================

app.Run();