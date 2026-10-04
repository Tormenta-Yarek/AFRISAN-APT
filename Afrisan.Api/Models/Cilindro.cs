namespace Afrisan.Api.Models
{
    public class Cilindro
    {
        public int Id { get; set; }

        public string CodigoQr { get; set; } = string.Empty;

        // Se mantiene temporalmente para no romper los datos existentes
        public string TipoRefrigerante { get; set; } = string.Empty;

        // Capacidad máxima de refrigerante del cilindro
        public decimal CapacidadKg { get; set; }

        // Tara física del cilindro vacío
        public decimal? TaraKg { get; set; }

        // Peso bruto actual: cilindro + refrigerante
        public decimal PesoActualKg { get; set; }

        public string Estado { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // Relación con GasRefrigerante
        public int? GasRefrigeranteId { get; set; }

        public GasRefrigerante? GasRefrigerante { get; set; }
    }
}