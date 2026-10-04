using System.Text.Json.Serialization;

namespace Afrisan.Api.Models
{
    public class GasRefrigerante
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Codigo { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        [JsonIgnore]
        public ICollection<Cilindro> Cilindros { get; set; }
            = new List<Cilindro>();
    }
}