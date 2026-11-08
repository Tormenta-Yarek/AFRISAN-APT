using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Afrisan.Nuevo.Web.Models;

// Modelos editables exclusivos de los formularios. La API recibe DTOs separados.
public sealed class CredencialesForm
{
    [Required(ErrorMessage = "Ingresa el correo."), EmailAddress] public string Email { get; set; } = "";
    [Required(ErrorMessage = "Ingresa la contraseña.")] public string Password { get; set; } = "";
}
public sealed class RefrigeranteForm
{
    [Required(ErrorMessage = "Ingresa el código."), StringLength(40)] public string Codigo { get; set; } = "";
    [Required(ErrorMessage = "Ingresa el nombre."), StringLength(150)] public string Nombre { get; set; } = "";
}
public sealed class CilindroForm
{
    public int? SucursalId { get; set; }
    public string UbicacionInicial { get; set; } = "Bodega";
    [StringLength(200)] public string? SucursalInicial { get; set; }
    [Range(1, long.MaxValue, ErrorMessage = "Selecciona un refrigerante.")] public long RefrigeranteId { get; set; }
    [StringLength(120)] public string? NumeroSerie { get; set; }
    [Range(1, long.MaxValue, ErrorMessage = "La capacidad debe ser mayor a cero.")] public long CapacidadGr { get; set; }
    [Range(1, long.MaxValue, ErrorMessage = "La tara debe ser mayor a cero.")] public long TaraGr { get; set; }
    [Range(0, long.MaxValue)] public long? PesoInicialGr { get; set; }
    [StringLength(1000)] public string? Observacion { get; set; }
}
public sealed class PesajeForm
{
    [Required(ErrorMessage = "Ingresa el peso bruto."), Range(0, long.MaxValue)] public long? PesoBrutoGr { get; set; }
    [StringLength(1000)] public string? Observacion { get; set; }
}
public static class Formato
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CL");
    public static string Gramos(long? gramos) => gramos is { } g ? g.ToString("N0", Cultura) + " g" : "Sin pesaje registrado";
    public static string Kg(long? gramos) => gramos is { } g ? (g / 1000m).ToString("N3", Cultura) + " kg" : "—";
    public static string Estado(string valor) => valor switch { "PendientePesaje" => "Pendiente de pesaje", "Vacio" => "Vacío", "EnProcesoDespacho" => "En proceso de despacho", "EnTerreno" => "En terreno", "PendienteBodega" => "Pendiente de aprobación de Bodega", "PendienteRevision" => "Pendiente de revisión del Autorizador", "RetiradoPendienteRecepcion" => "Retirado / pendiente de bodega", _ => valor };
    public static string Estilo(string valor) => valor switch { "Disponible" => "success", "Vacio" => "neutral", _ => "pending" };
    public static string Error(Exception ex) => ex is InvalidOperationException ? ex.Message : "No se pudo consultar la API. Comprueba que Afrisan.Nuevo.Api esté encendida y vuelve a intentarlo.";
}
