using Afrisan.Api.DTOs;
using Afrisan.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Afrisan.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        private static readonly string[] RolesPermitidos =
        {
            "Administrador",
            "Jefatura",
            "Bodega",
            "Tecnico"
        };

        public UsuariosController(
            UserManager<Usuario> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // =====================================================
        // CREAR USUARIO
        // =====================================================

        [HttpPost]
        public async Task<IActionResult> CrearUsuario(
            CrearUsuarioDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var email =
                dto.Email
                    .Trim()
                    .ToLowerInvariant();

            var nombreCompleto =
                dto.NombreCompleto.Trim();

            var rol =
                dto.Rol.Trim();

            if (string.IsNullOrWhiteSpace(nombreCompleto))
            {
                return BadRequest(new
                {
                    mensaje =
                        "El nombre completo es obligatorio."
                });
            }

            var rolValido =
                RolesPermitidos.FirstOrDefault(
                    r => r.Equals(
                        rol,
                        StringComparison.OrdinalIgnoreCase));

            if (rolValido == null)
            {
                return BadRequest(new
                {
                    mensaje =
                        "El rol indicado no es válido.",
                    rolesPermitidos =
                        RolesPermitidos
                });
            }

            if (!await _roleManager.RoleExistsAsync(
                rolValido))
            {
                return BadRequest(new
                {
                    mensaje =
                        "El rol seleccionado no existe en AFRISAN."
                });
            }

            var usuarioExistente =
                await _userManager.FindByEmailAsync(
                    email);

            if (usuarioExistente != null)
            {
                return Conflict(new
                {
                    mensaje =
                        "Ya existe un usuario registrado con ese correo."
                });
            }

            var usuario = new Usuario
            {
                UserName = email,
                Email = email,
                NombreCompleto = nombreCompleto,
                Activo = true,
                EmailConfirmed = true
            };

            var resultado =
                await _userManager.CreateAsync(
                    usuario,
                    dto.Password);

            if (!resultado.Succeeded)
            {
                return BadRequest(new
                {
                    mensaje =
                        "No se pudo crear el usuario.",
                    errores =
                        resultado.Errors
                            .Select(e => e.Description)
                            .ToList()
                });
            }

            var resultadoRol =
                await _userManager.AddToRoleAsync(
                    usuario,
                    rolValido);

            if (!resultadoRol.Succeeded)
            {
                await _userManager.DeleteAsync(usuario);

                return BadRequest(new
                {
                    mensaje =
                        "No se pudo asignar el rol al usuario."
                });
            }

            return Created(
                $"/api/Usuarios/{usuario.Id}",
                new
                {
                    id = usuario.Id,
                    nombreCompleto =
                        usuario.NombreCompleto,
                    email =
                        usuario.Email,
                    activo =
                        usuario.Activo,
                    rol =
                        rolValido
                });
        }
    }
}