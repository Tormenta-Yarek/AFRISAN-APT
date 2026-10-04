namespace Afrisan.Api.DTOs
{
    public class RetornoCilindroDto
    {
        public string Responsable { get; set; } = string.Empty;

        public decimal? PesoKg { get; set; }

        public string? Observaciones { get; set; }
    }
}