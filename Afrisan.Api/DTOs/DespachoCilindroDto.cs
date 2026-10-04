namespace Afrisan.Api.DTOs
{
    public class DespachoCilindroDto
    {
        public int? OrdenTrabajoId { get; set; }

        public string Responsable { get; set; } = string.Empty;

        public decimal? PesoKg { get; set; }

        public string? Observaciones { get; set; }
    }
}