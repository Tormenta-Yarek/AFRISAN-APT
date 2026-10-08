using Afrisan.Api.DTOs;
using Afrisan.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Afrisan.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<Usuario> userManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        // =====================================================
        // LOGIN
        // =====================================================

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new
                {
                    mensaje = "Debes ingresar correo y contraseña."
                });
            }

            var email = dto.Email.Trim();

            var usuario =
                await _userManager.FindByEmailAsync(email);

            if (usuario == null)
            {
                return Unauthorized(new
                {
                    mensaje = "Correo o contraseña incorrectos."
                });
            }

            if (!usuario.Activo)
            {
                return Unauthorized(new
                {
                    mensaje = "La cuenta se encuentra desactivada."
                });
            }

            if (await _userManager.IsLockedOutAsync(usuario))
            {
                return Unauthorized(new
                {
                    mensaje =
                        "La cuenta está temporalmente bloqueada por múltiples intentos fallidos."
                });
            }

            var passwordCorrecta =
                await _userManager.CheckPasswordAsync(
                    usuario,
                    dto.Password);

            if (!passwordCorrecta)
            {
                await _userManager.AccessFailedAsync(usuario);

                return Unauthorized(new
                {
                    mensaje = "Correo o contraseña incorrectos."
                });
            }

            await _userManager.ResetAccessFailedCountAsync(usuario);

            var roles =
                await _userManager.GetRolesAsync(usuario);

            var token = GenerarToken(
                usuario,
                roles);

            return Ok(new
            {
                mensaje = "Inicio de sesión correcto.",
                token = token.Token,
                expiracion = token.Expiracion,
                usuario = new
                {
                    id = usuario.Id,
                    nombreCompleto = usuario.NombreCompleto,
                    email = usuario.Email,
                    roles
                }
            });
        }

        // =====================================================
        // PERFIL DEL USUARIO AUTENTICADO
        // =====================================================

        [Authorize]
        [HttpGet("perfil")]
        public IActionResult Perfil()
        {
            var usuarioId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var nombre =
                User.FindFirstValue(ClaimTypes.Name);

            var email =
                User.FindFirstValue(ClaimTypes.Email);

            var roles =
                User.FindAll(ClaimTypes.Role)
                    .Select(r => r.Value)
                    .ToList();

            return Ok(new
            {
                id = usuarioId,
                nombreCompleto = nombre,
                email,
                roles
            });
        }

        // =====================================================
        // GENERAR JWT
        // =====================================================

        private (
            string Token,
            DateTime Expiracion
        ) GenerarToken(
            Usuario usuario,
            IList<string> roles)
        {
            var jwtIssuer =
                _configuration["Jwt:Issuer"];

            var jwtAudience =
                _configuration["Jwt:Audience"];

            var jwtKey =
                _configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtIssuer) ||
                string.IsNullOrWhiteSpace(jwtAudience) ||
                string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException(
                    "La configuración JWT está incompleta."
                );
            }

            var claims = new List<Claim>
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    usuario.Id),

                new Claim(
                    ClaimTypes.NameIdentifier,
                    usuario.Id),

                new Claim(
                    ClaimTypes.Name,
                    usuario.NombreCompleto),

                new Claim(
                    ClaimTypes.Email,
                    usuario.Email ?? string.Empty),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString())
            };

            foreach (var rol in roles)
            {
                claims.Add(
                    new Claim(
                        ClaimTypes.Role,
                        rol));
            }

            var key =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey));

            var credentials =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256);

            var expiracion =
                DateTime.UtcNow.AddHours(8);

            var jwt = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiracion,
                signingCredentials: credentials);

            var token =
                new JwtSecurityTokenHandler()
                    .WriteToken(jwt);

            return (
                token,
                expiracion
            );
        }
    }
}