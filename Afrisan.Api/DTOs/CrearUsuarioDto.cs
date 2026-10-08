using System.ComponentModel.DataAnnotations;

namespace Afrisan.Api.DTOs
{
    public class CrearUsuarioDto
    {
        [Required]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Rol { get; set; } = string.Empty;
    }
}