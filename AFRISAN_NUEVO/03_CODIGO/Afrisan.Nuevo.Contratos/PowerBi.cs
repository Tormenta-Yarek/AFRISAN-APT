namespace Afrisan.Nuevo.Contratos;

public static class RolesBi
{
    public const string Consulta = "Administrador,Jefatura";
}

public sealed record PowerBiConfiguracionResponse(bool Configurado, string? EmbedUrl, string? AbrirUrl);
public sealed record BiGrupoResponse(string Nombre, decimal Valor);
public sealed record BiOpcionResponse(long Id, string Nombre);
public sealed record BiResumenResponse(DateTimeOffset GeneradoUtc, DateOnly Desde, DateOnly Hasta,
    int Cilindros, int SinPesaje, int OrdenesAbiertas, int OrdenesCerradas,
    decimal KgInstalados, int RetiradosPendientes, int IncidenciasAbiertas,
    BiGrupoResponse[] InventarioPorEstado, BiGrupoResponse[] OrdenesPorEstado,
    BiGrupoResponse[] InstaladosPorRefrigerante, BiOpcionResponse[] Sucursales,
    BiOpcionResponse[] Refrigerantes, BiOpcionResponse[] Tecnicos);

// Contrato de las seis tablas del paquete Power BI. Sin correos, fotografías ni credenciales.
public sealed record BiInventario(long CilindroId, string Codigo, long RefrigeranteId,
    string Refrigerante, string Estado, string Ubicacion, int? SucursalId, string? Sucursal,
    int? TecnicoId, string? Tecnico, decimal? KgSegunUltimoPesaje, DateTimeOffset? FechaPesaje);
public sealed record BiOrden(long OtId, string Codigo, int SucursalId, string Sucursal,
    int TecnicoId, string Tecnico, string Estado, DateTimeOffset FechaCreacion,
    DateOnly DiaCreacion, DateTimeOffset? FechaCierre);
public sealed record BiDetalle(long OtId, long RefrigeranteId, string Refrigerante,
    decimal KgSolicitados, decimal KgAutorizados);
public sealed record BiOperacion(Guid OperacionId, long OtId, string Tipo,
    long RefrigeranteId, string Refrigerante, string CilindroInstalado, string? CilindroRetirado,
    DateTimeOffset FechaConfirmacion, DateOnly DiaConfirmacion, decimal KgInstalados,
    decimal? KgRetirados, DateTimeOffset? FechaRecepcion, decimal? KgRecepcion,
    bool PendienteRecepcion);
public sealed record BiDespacho(Guid DespachoId, long OtId, string Cilindro,
    long RefrigeranteId, string Refrigerante, DateTimeOffset Fecha, DateOnly Dia,
    decimal KgDespachados, string Estado, DateTimeOffset? FechaDevolucion,
    decimal? KgDevueltos);
public sealed record BiIncidencia(Guid IncidenciaId, long OtId, string Tipo,
    DateTimeOffset Fecha, DateOnly Dia, bool Abierta, DateTimeOffset? FechaRevision);
