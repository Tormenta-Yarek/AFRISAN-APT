using System.ComponentModel.DataAnnotations;

namespace Afrisan.Api.DTOs
{
    public class GasRefrigeranteDto
    {
        [Required]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        public string Codigo { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;
    }
}