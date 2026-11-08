using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Afrisan.Nuevo.Contratos;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Afrisan.Nuevo.Api;

// Cuentas exclusivamente de revisión, aisladas de los usuarios de la aplicación anterior.
public sealed class SesionesDemo
{
    public const string Issuer = "AFRISAN.Nuevo.Demo";
    public const string Audience = "AFRISAN.Nuevo.Web";
    public const string Password = "DemoAfrisan2026!";
    public string Instancia { get; } = Guid.NewGuid().ToString("N");
    private readonly byte[] firma;
    private readonly object gate = new();
    private readonly PasswordHasher<CuentaDemoResponse> hasher = new();
    private readonly Dictionary<string, Cuenta> cuentas;
    public CuentaDemoResponse[] Cuentas => cuentas.Values.Select(c => c.Info).ToArray();

    public SesionesDemo(byte[] firma)
    {
        this.firma = firma;
        cuentas = Roles.Todos.Select(rol => new CuentaDemoResponse($"{(rol == "Tecnico" ? "Técnico" : rol)} Demo", $"{rol.ToLowerInvariant()}@afrisan.example", rol))
            .ToDictionary(c => c.Email, c => new Cuenta(c, hasher.HashPassword(c, Password)), StringComparer.OrdinalIgnoreCase);
    }

    public SesionResponse? Login(LoginRequest request)
    {
        lock (gate)
        {
            if (!cuentas.TryGetValue(request.Email.Trim(), out var cuenta) || cuenta.BloqueadoHasta > DateTimeOffset.UtcNow) return null;
            if (hasher.VerifyHashedPassword(cuenta.Info, cuenta.Hash, request.Password) == PasswordVerificationResult.Failed)
            {
                if (++cuenta.Intentos >= 5) cuenta.BloqueadoHasta = DateTimeOffset.UtcNow.AddMinutes(15);
                return null;
            }
            cuenta.Intentos = 0;
            cuenta.BloqueadoHasta = null;
            var vence = DateTimeOffset.UtcNow.AddHours(2);
            Claim[] claims = [new(ClaimTypes.NameIdentifier, cuenta.Info.Email), new(ClaimTypes.Name, cuenta.Info.Nombre),
                new(ClaimTypes.Role, cuenta.Info.Rol), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())];
            var jwt = new JwtSecurityToken(Issuer, Audience, claims, expires: vence.UtcDateTime,
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(firma), SecurityAlgorithms.HmacSha256));
            return new(new JwtSecurityTokenHandler().WriteToken(jwt), vence, cuenta.Info.Nombre, cuenta.Info.Rol, cuenta.Info.Email);
        }
    }

    private sealed class Cuenta(CuentaDemoResponse info, string hash)
    {
        public CuentaDemoResponse Info { get; } = info;
        public string Hash { get; } = hash;
        public int Intentos { get; set; }
        public DateTimeOffset? BloqueadoHasta { get; set; }
    }
}
