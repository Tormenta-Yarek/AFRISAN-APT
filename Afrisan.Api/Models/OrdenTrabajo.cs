namespace Afrisan.Api.Models
{
    public class OrdenTrabajo
    {
        public int Id { get; set; }

        // Código oficial de la Orden de Trabajo.
        // No será inventado desde el formulario de despacho:
        // el usuario deberá seleccionar una OT existente.
        public string Codigo { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public bool Activa { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public ICollection<MovimientoInventario> Movimientos
        {
            get;
            set;
        } = new List<MovimientoInventario>();
    }
}