using Afrisan.Nuevo.Contratos;
using Microsoft.JSInterop;

namespace Afrisan.Nuevo.Web.Services;

public sealed class TrabajoLocalService(InventarioClient api, SesionService sesion, IJSRuntime js) : IAsyncDisposable
{
    private readonly SemaphoreSlim sincronizacion = new(1, 1);
    private readonly string id = Guid.NewGuid().ToString();
    private DotNetObjectReference<TrabajoLocalService>? referencia;
    private readonly HashSet<Guid> enEdicion = [];
    public void Editar(Guid operacion) => enEdicion.Add(operacion);
    public void DejarEditar(Guid operacion) => enEdicion.Remove(operacion);
    public bool ConsultaOffline { get; private set; }
    public bool Sincronizando { get; private set; }
    public string? Mensaje { get; private set; }
    private string UsuarioCache => $"{Usuario}:{sesion.Entorno?.Instancia}";
    private string Usuario => sesion.Sesion?.Usuario ?? "";

    public async Task IniciarAsync()
    {
        if (referencia is not null) return;
        referencia = DotNetObjectReference.Create(this);
        await js.InvokeVoidAsync("afrisanLocal.suscribir", id, referencia);
    }
    public async Task<OtResponse[]> OrdenesAsync()
    {
        await IniciarAsync();
        try
        {
            var lista = await api.OrdenesAsync();
            foreach (var ot in lista) await CacheAsync(ot);
            ConsultaOffline = false;
            return lista;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            ConsultaOffline = true;
            return await js.InvokeAsync<OtResponse[]>("afrisanLocal.listar", "ot", UsuarioCache);
        }
    }
    public async Task<OtResponse> OrdenAsync(Guid idOt)
    {
        await IniciarAsync();
        try { var ot = await api.OrdenAsync(idOt); await CacheAsync(ot); ConsultaOffline = false; return ot; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            ConsultaOffline = true;
            return (await js.InvokeAsync<OtResponse[]>("afrisanLocal.listar", "ot", UsuarioCache)).FirstOrDefault(o => o.Id == idOt)
                ?? throw new InvalidOperationException("Esta OT no está guardada para consulta sin conexión. Ábrela primero con conexión.");
        }
    }
    public ValueTask CacheAsync(OtResponse ot) => js.InvokeVoidAsync("afrisanLocal.guardar", "ot", UsuarioCache, ot.Id.ToString(), ot);
    public ValueTask GuardarLocalAsync(BorradorLocal borrador)
    {
        if (borrador.Usuario != Usuario || string.IsNullOrWhiteSpace(Usuario)) throw new InvalidOperationException("No se puede guardar el trabajo sin una sesión identificada.");
        return js.InvokeVoidAsync("afrisanLocal.guardar", "borrador", Usuario, borrador.Datos.Id.ToString(), borrador);
    }
    public async ValueTask<BorradorLocal[]> BorradoresAsync() => (await js.InvokeAsync<BorradorLocal[]>("afrisanLocal.listar", "borrador", Usuario)).Where(b => !b.Descartado).ToArray();
    public async Task DescartarAsync(Guid otId, Guid operacionId)
    {
        if (!await sincronizacion.WaitAsync(0)) throw new InvalidOperationException("Espera a que termine la sincronización antes de descartar.");
        try
        {
            var local = (await BorradoresAsync()).FirstOrDefault(b => b.OtId == otId && b.Datos.Id == operacionId);
            var ot = await api.OrdenAsync(otId); // Requiere conexión: no ocultar un borrador remoto por error de red.
            var op = ot.Operaciones.FirstOrDefault(o => o.Id == operacionId);
            if (op is not null) ot = await api.DescartarOperacionAsync(otId, operacionId, local?.Datos.Version ?? op.Version);
            if (local is not null) await GuardarLocalAsync(local with { Descartado = true, Pendiente = false, ConfirmarAlSincronizar = false });
            await CacheAsync(ot);
        }
        finally { sincronizacion.Release(); }
    }
    public ValueTask<IncidenciaLocal[]> IncidenciasAsync() => js.InvokeAsync<IncidenciaLocal[]>("afrisanLocal.listar", "incidencia", Usuario);
    private ValueTask ConservarIncidencia(IncidenciaLocal incidencia) => js.InvokeVoidAsync("afrisanLocal.guardar", "incidencia", Usuario, incidencia.Datos.Id.ToString(), incidencia);
    public ValueTask GuardarIncidenciaAsync(Guid ot, CrearIncidenciaRequest datos)
    {
        if (string.IsNullOrWhiteSpace(Usuario) || sesion.Entorno is null) throw new InvalidOperationException("Falta la identidad/instancia local del trabajo.");
        return ConservarIncidencia(new(Usuario, sesion.Entorno.Instancia, ot, datos));
    }

    [JSInvokable] public async Task ConexionRecuperada() => await SincronizarAsync();

    public async Task SincronizarAsync(Guid? incluirEnEdicion = null)
    {
        if (sesion.Sesion?.Rol != "Tecnico" || !sesion.Autenticado || !await sincronizacion.WaitAsync(0)) return;
        Sincronizando = true; Mensaje = null;
        try
        {
            var entorno = await api.EntornoAsync();
            foreach (var borrador in (await BorradoresAsync()).Where(b => b.Pendiente && (!enEdicion.Contains(b.Datos.Id) || b.Datos.Id == incluirEnEdicion)))
            {
                if (borrador.Datos.Instancia != entorno.Instancia)
                {
                    await GuardarLocalAsync(borrador with { Error = "El borrador corresponde a otra instalación. Se conserva sin enviarlo a una OT de otra base." });
                    continue;
                }
                var actual = borrador;
                try
                {
                    foreach (var eliminada in actual.FotosEliminar)
                    {
                        var eliminacion = await api.QuitarFotoAsync(actual.OtId, actual.Datos.Id, eliminada, actual.Datos.Version);
                        actual = actual with { Datos = actual.Datos with { Version = eliminacion.Version } };
                        await GuardarLocalAsync(actual);
                    }
                    var remoto = await api.GuardarOperacionAsync(actual.OtId, actual.Datos);
                    actual = actual with { Datos = actual.Datos with { Version = remoto.Version } };
                    await GuardarLocalAsync(actual);
                    foreach (var foto in actual.Fotos)
                    {
                        remoto = await api.SubirFotoAsync(actual.OtId, actual.Datos.Id, foto with { Version = actual.Datos.Version });
                        actual = actual with { Datos = actual.Datos with { Version = remoto.Version } };
                        await GuardarLocalAsync(actual);
                    }
                    if (actual.ConfirmarAlSincronizar) remoto = await api.ConfirmarOperacionAsync(actual.OtId, actual.Datos.Id, actual.Datos.Version);
                    await GuardarLocalAsync(actual with { Datos = actual.Datos with { Version = remoto.Version }, FotosEliminar = [], Pendiente = false, Error = null });
                    await CacheAsync(await api.OrdenAsync(actual.OtId));
                }
                catch (InvalidOperationException ex) { await GuardarLocalAsync(actual with { Error = ex.Message }); }
            }
            foreach (var incidencia in (await IncidenciasAsync()).Where(i => i.Pendiente))
            {
                if (incidencia.Instancia != entorno.Instancia)
                {
                    await ConservarIncidencia(incidencia with { Error = "Corresponde a otra ejecución de la API; se conserva sin reenviarla." });
                    continue;
                }
                try { var ot = await api.IncidenciaAsync(incidencia.OtId, incidencia.Datos); await ConservarIncidencia(incidencia with { Pendiente = false, Error = null }); await CacheAsync(ot); }
                catch (InvalidOperationException ex) { await ConservarIncidencia(incidencia with { Error = ex.Message }); }
            }
            Mensaje = "Sincronización terminada. Revisa los borradores e incidencias con conflicto o pendientes.";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { Mensaje = "Sin conexión con la API. Los borradores y fotos siguen guardados en este dispositivo."; }
        finally { Sincronizando = false; sincronizacion.Release(); }
    }
    public async ValueTask DisposeAsync()
    {
        if (referencia is not null)
        {
            try { await js.InvokeVoidAsync("afrisanLocal.cancelar", id); } catch (JSDisconnectedException) { }
            referencia.Dispose();
        }
        // No se eliminan borradores al cambiar de página o cerrar sesión.
    }
}
