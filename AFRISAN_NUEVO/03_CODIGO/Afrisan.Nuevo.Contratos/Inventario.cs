using System.ComponentModel.DataAnnotations;

namespace Afrisan.Nuevo.Contratos;

public static class Roles
{
    public const string Lectura = "Administrador,Bodega,Autorizador,Jefatura";
    // Distribución provisional de PN-01, pendiente de confirmar catálogo vs operación.
    public const string Catalogo = "Administrador,Bodega,Jefatura";
    public const string AltaCilindro = "Administrador,Bodega";
    public static readonly string[] Todos = ["Bodega", "Autorizador", "Jefatura", "Tecnico", "Administrador"];
}

/// <summary>Datos para registrar un tipo de refrigerante, sin cantidades de stock.</summary>
public sealed record CrearRefrigeranteRequest
{
    [Required, StringLength(40)] public string Codigo { get; init; } = "";
    [Required, StringLength(150)] public string Nombre { get; init; } = "";
}

/// <summary>Tipo único de gas disponible para asociar a cilindros.</summary>
public sealed record RefrigeranteResponse(long Id, string Codigo, string Nombre);

/// <summary>Alta del envase. Peso inicial es lectura bruta opcional, nunca capacidad inferida.</summary>
public sealed record CrearCilindroRequest
{
    [Range(1, int.MaxValue)] public int? SucursalId { get; init; }
    [Required] public string UbicacionInicial { get; init; } = "Bodega";
    [StringLength(200)] public string? SucursalInicial { get; init; }
    [Range(1, long.MaxValue)] public long RefrigeranteId { get; init; }
    [StringLength(120)] public string? NumeroSerie { get; init; }
    [Range(1, long.MaxValue)] public long CapacidadGr { get; init; }
    [Range(1, long.MaxValue)] public long TaraGr { get; init; }
    [Range(0, long.MaxValue)] public long? PesoInicialGr { get; init; }
    [StringLength(1000)] public string? Observacion { get; init; }
}

/// <summary>Primer pesaje físico de un cilindro, separado de un despacho o retorno.</summary>
public sealed record RegistrarPesajeInicialRequest
{
    [Range(0, long.MaxValue)] public long PesoBrutoGr { get; init; }
    [StringLength(1000)] public string? Observacion { get; init; }
}

/// <summary>Evidencia de un registro de balanza inicial: fecha, actor y lectura.</summary>
public sealed record PesajeResponse(long PesoBrutoGr, DateTimeOffset Fecha, string Usuario, string? Observacion);

/// <summary>Consulta del cilindro con un único refrigerante y peso medido diferenciados.</summary>
public sealed record CilindroResponse(long Id, string Codigo, string? NumeroSerie,
    long RefrigeranteId, string RefrigeranteCodigo, string RefrigeranteNombre,
    long CapacidadGr, long TaraGr, string Estado, DateTimeOffset FechaRegistro,
    PesajeResponse? PesajeInicial, string Ubicacion = "Bodega", string? Sucursal = null,
    string? Tecnico = null, PesajeResponse? UltimoPesaje = null, int? SucursalId = null)
{
    public PesajeResponse? PesajeRegistrado => UltimoPesaje ?? PesajeInicial;
    public long? ContenidoSegunPesajeGr => PesajeRegistrado is null ? null : PesajeRegistrado.PesoBrutoGr - TaraGr;
}

/// <summary>Traza interna del prototipo para el detalle del registro.</summary>
public sealed record EventoResponse(DateTimeOffset Fecha, string Accion, string Usuario, string Descripcion);

/// <summary>Credenciales de una cuenta exclusivamente de demostración.</summary>
public sealed record LoginRequest
{
    [Required, EmailAddress] public string Email { get; init; } = "";
    [Required] public string Password { get; init; } = "";
}

/// <summary>Sesión firmada con vencimiento y rol comprobados por la API.</summary>
public sealed record SesionResponse(string Token, DateTimeOffset Vence, string Nombre, string Rol, string Usuario = "");

/// <summary>Cuenta pública de ejemplo; no corresponde a una cuenta de Supabase.</summary>
public sealed record CuentaDemoResponse(string Nombre, string Email, string Rol);

/// <summary>Identifica la instancia demo y explica que la persistencia es temporal.</summary>
public sealed record EntornoResponse(string Instancia, string Modo, string PasswordDemo, CuentaDemoResponse[] Cuentas);
