using System.ComponentModel.DataAnnotations;

namespace Afrisan.Api.DTOs
{
    public class CilindroDto
    {
        [Required]
        public string CodigoQr { get; set; } = string.Empty;

        [Required]
        public string TipoRefrigerante { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue)]
        public decimal CapacidadKg { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PesoActualKg { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? TaraKg { get; set; }

        [Required]
        public string Estado { get; set; } = string.Empty;

        public int? GasRefrigeranteId { get; set; }
    }
}