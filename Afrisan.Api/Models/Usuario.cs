using Microsoft.AspNetCore.Identity;

namespace Afrisan.Api.Models
{
    public class Usuario : IdentityUser
    {
        public string NombreCompleto { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}