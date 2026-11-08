using System.ComponentModel.DataAnnotations;

namespace Afrisan.Nuevo.Contratos;

/// <summary>Pedido por tipo de gas. Se conserva la cantidad autorizada, sin inferir consumo.</summary>
public sealed record LineaOtRequest
{
    [Range(1, long.MaxValue)] public long RefrigeranteId { get; init; }
    [Range(1, long.MaxValue)] public long GrSolicitados { get; init; }
    [Range(1, long.MaxValue)] public long GrAutorizados { get; init; }
}

/// <summary>Preparación manual de una OT de revisión por el Autorizador.</summary>
public sealed record CrearOtRequest
{
    [Range(1, int.MaxValue)] public int? SucursalId { get; init; }
    public Guid IdOperacion { get; init; }
    [Required, StringLength(200)] public string Sucursal { get; init; } = "";
    [Required, StringLength(400)] public string Direccion { get; init; } = "";
    [Required, StringLength(150)] public string Descripcion { get; init; } = "";
    [Required, EmailAddress] public string TecnicoEmail { get; init; } = "";
    [MinLength(1), MaxLength(20)] public LineaOtRequest[] Detalle { get; init; } = [];
    [MaxLength(50)] public string[] CilindrosARetirar { get; init; } = [];
}
/// <summary>Detalle del pedido y cantidades por refrigerante.</summary>
public sealed record LineaOtResponse(long RefrigeranteId, string Refrigerante, long GrSolicitados, long GrAutorizados);
/// <summary>Envase entregado al técnico; recepción de retirados se confirma por bodega.</summary>
public sealed record EntregaResponse(string Codigo, string Refrigerante, long TaraGr, long CapacidadGr, long PesoBrutoGr, string Usuario, DateTimeOffset Fecha,
    string Estado = "EnProcesoDespacho", long? PesoRecepcionGr = null, string? UsuarioRecepcion = null, DateTimeOffset? FechaRecepcion = null,
    Guid Id = default, FotoResponse? FotoDespacho = null);
/// <summary>Registro operativo de la OT sin fotografías binarias.</summary>
public sealed record OtResponse(Guid Id, string Codigo, string Sucursal, string Direccion, string Descripcion,
    string TecnicoEmail, string Estado, string Autorizador, DateTimeOffset Fecha,
    LineaOtResponse[] Detalle, string[] CilindrosARetirar, EntregaResponse[] Entregas,
    OperacionResponse[] Operaciones, IncidenciaResponse[] Incidencias, EventoResponse[] Historial,
    string? ObservacionTermino, RevisionTrabajoResponse? AprobacionBodega = null, RevisionTrabajoResponse? RevisionAutorizador = null);
/// <summary>Retiro físico de bodega registrado por el técnico asignado, con peso y foto.</summary>
public sealed record EntregarCilindroRequest
{
    public Guid IdOperacion { get; init; }
    [Required, StringLength(120)] public string Codigo { get; init; } = "";
    [Range(0, long.MaxValue)] public long PesoBrutoGr { get; init; }
    [Required] public FotoPesajeRequest? Foto { get; init; }
}

public sealed record FotoPesajeRequest
{
    public Guid Id { get; init; }
    [Required, StringLength(150)] public string Nombre { get; init; } = "";
    [Required, StringLength(30)] public string ContentType { get; init; } = "";
    [Required, MaxLength(5_600_000)] public string Base64 { get; init; } = "";
}

/// <summary>Confirmación humana de la revisión; no equivale a verificar automáticamente una fotografía.</summary>
public sealed record RevisarTrabajoRequest
{
    public bool ConfirmoRevision { get; init; }
    [StringLength(2000)] public string? Observacion { get; init; }
}
public sealed record RevisionTrabajoResponse(string Usuario, DateTimeOffset Fecha, string? Observacion);
/// <summary>Recepción oficial en bodega de un envase retirado de la sucursal.</summary>
public sealed record RecibirCilindroRequest
{
    [Required, StringLength(120)] public string Codigo { get; init; } = "";
    [Range(0, long.MaxValue)] public long PesoBrutoGr { get; init; }
    [StringLength(1000)] public string? Observacion { get; init; }
}
/// <summary>Borrador del reemplazo físico o agregado. La versión detecta conflictos.</summary>
public sealed record GuardarOperacionRequest
{
    public Guid Id { get; init; }
    [Required] public string Instancia { get; init; } = "";
    [Range(0, int.MaxValue)] public int Version { get; init; }
    [Required] public string Tipo { get; init; } = "Reemplazo";
    [StringLength(120)] public string? CodigoRetirado { get; init; }
    [StringLength(120)] public string? CodigoInstalado { get; init; }
    [Range(0, long.MaxValue)] public long? PesoRetiradoGr { get; init; }
    [Range(0, long.MaxValue)] public long? PesoInstaladoGr { get; init; }
    [StringLength(2000)] public string? Observacion { get; init; }
}
/// <summary>Foto con identificador estable: el reintento no crea otra imagen.</summary>
public sealed record SubirFotoRequest
{
    public int Version { get; init; }
    public Guid Id { get; init; }
    [Required] public string Grupo { get; init; } = "";
    [Required, StringLength(150)] public string Nombre { get; init; } = "";
    [Required, StringLength(30)] public string ContentType { get; init; } = "";
    [Required, MaxLength(5_600_000)] public string Base64 { get; init; } = "";
}
/// <summary>Referencia privada de fotografía, ligada al cilindro y a su condición.</summary>
public sealed record FotoResponse(Guid Id, string Grupo, string CodigoCilindro, string Nombre, string ContentType,
    long Bytes, DateTimeOffset Fecha, string Usuario);
/// <summary>Trabajo físico, pesajes y colección de evidencias por reemplazo o agregado.</summary>
public sealed record OperacionResponse(Guid Id, int Version, string Tipo, string? CodigoRetirado,
    string? CodigoInstalado, long? PesoRetiradoGr, long? PesoInstaladoGr, string? Observacion,
    string Estado, FotoResponse[] Fotos, DateTimeOffset Actualizada, bool EntregaInformada,
    bool RecibidoBodega, string? UsuarioRecepcion, DateTimeOffset? FechaRecepcion,
    long? PesoRecepcionGr = null, string? ObservacionRecepcion = null);
/// <summary>Incidencia o solicitud de rectificación, sin alterar el registro original.</summary>
public sealed record CrearIncidenciaRequest
{
    public Guid Id { get; init; }
    public Guid? OperacionId { get; init; }
    [Required] public string Tipo { get; init; } = "Incidencia";
    [Required, StringLength(2000, MinimumLength = 5)] public string Motivo { get; init; } = "";
}
/// <summary>Resultado de revisión de una incidencia por Autorizador.</summary>
public sealed record ResolverIncidenciaRequest
{
    [Required, StringLength(2000, MinimumLength = 5)] public string Respuesta { get; init; } = "";
}
/// <summary>Historial de revisión: motivo y conclusión, sin atribución automática de culpa.</summary>
public sealed record IncidenciaResponse(Guid Id, Guid? OperacionId, string Tipo, string Motivo, string Usuario,
    DateTimeOffset Fecha, string? Respuesta, string? Revisor, DateTimeOffset? FechaRevision);
/// <summary>Informe técnico de término o cierre administrativo, según el endpoint autorizado.</summary>
public sealed record TerminarTrabajoRequest
{
    [StringLength(2000)] public string? Observacion { get; init; }
}
/// <summary>Datos locales de un borrador y sus fotos. No significan confirmación del servidor.</summary>
public sealed record BorradorLocal
{
    public bool Descartado { get; init; }
    public string Usuario { get; init; } = "";
    public Guid OtId { get; init; }
    public GuardarOperacionRequest Datos { get; init; } = new();
    public SubirFotoRequest[] Fotos { get; init; } = [];
    public Guid[] FotosEliminar { get; init; } = [];
    public bool Pendiente { get; init; } = true;
    public bool ConfirmarAlSincronizar { get; init; }
    public string? Error { get; init; }
}

/// <summary>Incidencia conservada en el dispositivo hasta confirmación de la API.</summary>
public sealed record IncidenciaLocal(string Usuario, string Instancia, Guid OtId,
    CrearIncidenciaRequest Datos, bool Pendiente = true, string? Error = null);
