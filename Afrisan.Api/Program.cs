using Afrisan.Api.Data;
using Afrisan.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Net.Sockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// KESTREL
// =====================================================

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
        "No se encontró la cadena de conexión 'DefaultConnection'."
    );
}

builder.Services.AddDbContext<AfrisanDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// =====================================================
// IDENTITY
// =====================================================

builder.Services
    .AddIdentity<Usuario, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AfrisanDbContext>()
    .AddDefaultTokenProviders();

// =====================================================
// JWT
// =====================================================

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience) ||
    string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Falta configurar Jwt:Issuer, Jwt:Audience o Jwt:Key."
    );
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)
                    ),

                ClockSkew = TimeSpan.Zero
            };
    });

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

            if (uri.Host.Equals(
                "localhost",
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!IPAddress.TryParse(uri.Host, out var ip))
            {
                return false;
            }

            if (IPAddress.IsLoopback(ip))
            {
                return true;
            }

            if (ip.AddressFamily != AddressFamily.InterNetwork)
            {
                return false;
            }

            byte[] bytes = ip.GetAddressBytes();

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
// SWAGGER + JWT
// =====================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Ingresa el token JWT."
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
});

// =====================================================
// APP
// =====================================================

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

app.UseCors("AfrisanWeb");

// Siempre autenticación antes de autorización.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// =====================================================
// SEED DE ROLES Y ADMINISTRADOR
// =====================================================

using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.InicializarAsync(
        scope.ServiceProvider,
        app.Configuration);
}

app.Run();