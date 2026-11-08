using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Afrisan.Nuevo.Api.Persistencia;

public sealed class AfrisanDb(DbContextOptions<AfrisanDb> options) : DbContext(options);

// Un contexto y una transacción por comando. Nunca se crea ni migra el esquema al iniciar.
public sealed class PgDatabase(IDbContextFactory<AfrisanDb> factory)
{
    public async Task<PgUnit> Open(int? actor = null)
    {
        var db = await factory.CreateDbContextAsync();
        try
        {
            var unit = new PgUnit(db, await db.Database.BeginTransactionAsync());
            if (actor is not null)
                await unit.Query<string>("SELECT set_config('afrisan.id_usuario', @p0, true) AS \"Value\"", actor.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return unit;
        }
        catch { await db.DisposeAsync(); throw; }
    }

    public static int Actor(ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id > 0
            ? id : throw new ReglaInventarioException("La sesión no identifica un usuario persistido.", 401);
}

public sealed class PgUnit(AfrisanDb db, IDbContextTransaction transaction) : IAsyncDisposable
{
    private static object[] Parameters(object?[] values) => values.Select((v, i) =>
        (object)new NpgsqlParameter("p" + i, v ?? DBNull.Value)).ToArray();
    public Task<T[]> Query<T>(string sql, params object?[] values) => db.Database.SqlQueryRaw<T>(sql, Parameters(values)).ToArrayAsync();
    public Task<int> Exec(string sql, params object?[] values) => db.Database.ExecuteSqlRawAsync(sql, Parameters(values));
    public async Task<T> One<T>(string sql, params object?[] values) => (await Query<T>(sql, values)).Single();
    public Task Commit() => transaction.CommitAsync();
    public async ValueTask DisposeAsync() { await transaction.DisposeAsync(); await db.DisposeAsync(); }
}

public sealed record IdRow(long Id);
public sealed record UsuarioRow(int Id, string Nombre, string Apellido, string Correo, string Hash,
    string Rol, bool Activo, int Intentos, DateTimeOffset? BloqueadoHasta, Guid Version);
public sealed record CilindroRow(long Id, string Codigo, string? NumeroSerie, long RefrigeranteId,
    string RefrigeranteCodigo, string RefrigeranteNombre, long CapacidadGr, long TaraGr,
    string Estado, DateTimeOffset FechaRegistro, string Ubicacion, int? SucursalId,
    string? Sucursal, string? Tecnico, Guid? UltimoPesajeId);
public sealed record PesajeRow(Guid Id, long CilindroId, string Tipo, long Peso, DateTimeOffset Fecha,
    string Usuario, string? Observacion);
public sealed record OtRow(long Id, Guid Publico, string Codigo, int SucursalId, string Sucursal,
    string Direccion, string Descripcion, int TecnicoId, string Tecnico, string Estado,
    string Autorizador, DateTimeOffset Fecha, string? ObservacionTermino, string? Huella);
public sealed record OperacionRow(Guid Id, long OtId, string Tipo, int Version, string Estado,
    string Datos, string Fotos, DateTimeOffset Fecha, DateTimeOffset? Confirmacion,
    DateTimeOffset? Entrega, Guid? PesajeInstalado, Guid? PesajeRetirado);
public sealed record DespachoRow(Guid Id, long CilindroId, string Codigo, string Refrigerante,
    long TaraGr, long CapacidadGr, long Peso, string Usuario, DateTimeOffset Fecha, string Estado, Guid PesajeId);
public sealed record RecepcionRow(Guid Id, Guid? OperacionId, Guid? DespachoId, long CilindroId,
    long Peso, string Usuario, DateTimeOffset Fecha, string? Observacion);
public sealed record EvidenciaRow(Guid Id, Guid PesajeId, string Bucket, string Ruta, string Nombre,
    string ContentType, long Bytes, string Sha256, string Usuario, DateTimeOffset Fecha);
