using Afrisan.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);

// Permitir que la API escuche desde el computador
// y desde otros dispositivos de la red local.
// Esta configuración se aplica durante el desarrollo.
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(5141);
    });
}

// Conexión con PostgreSQL / Supabase
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

// Permitir que Afrisan.Web consulte la API
// desde localhost o desde una IP de red privada.
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

            // Puertos utilizados por Afrisan.Web
            bool puertoPermitido =
                (uri.Scheme == "http" && uri.Port == 5226) ||
                (uri.Scheme == "https" && uri.Port == 7083);

            if (!puertoPermitido)
            {
                return false;
            }

            // Acceso desde el propio computador
            if (uri.Host.Equals(
                "localhost",
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Acceso desde una dirección IP
            if (!IPAddress.TryParse(uri.Host, out var ip))
            {
                return false;
            }

            // Permitir direcciones de loopback
            if (IPAddress.IsLoopback(ip))
            {
                return true;
            }

            // Comprobar que sea una dirección IPv4
            if (ip.AddressFamily != AddressFamily.InterNetwork)
            {
                return false;
            }

            byte[] bytes = ip.GetAddressBytes();

            // Permitir direcciones IPv4 privadas:
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

// Controladores
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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

// Habilitar comunicación con Afrisan.Web
app.UseCors("AfrisanWeb");

app.UseAuthorization();

app.MapControllers();

app.Run();  