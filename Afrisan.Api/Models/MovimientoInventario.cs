namespace Afrisan.Api.Models
{
    public class MovimientoInventario
    {
        public int Id { get; set; }

        // Cilindro al que pertenece el movimiento
        public int CilindroId { get; set; }

        public Cilindro Cilindro { get; set; } = null!;

        // "Despacho" o "Retorno"
        public string TipoMovimiento { get; set; } = string.Empty;

        // Peso registrado al momento del movimiento
        public decimal PesoKg { get; set; }

        // Persona que retira o devuelve el cilindro
        public string Responsable { get; set; } = string.Empty;

        public DateTime FechaMovimiento { get; set; } = DateTime.UtcNow;

        public string? Observaciones { get; set; }
    }
}