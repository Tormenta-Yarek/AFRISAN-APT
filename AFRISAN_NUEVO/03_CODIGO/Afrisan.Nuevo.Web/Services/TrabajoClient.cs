using System.Net.Http.Json;
using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Web.Services;

public sealed partial class InventarioClient
{
    private static string OtRuta(Guid id) => $"ordenes-trabajo/{id}";
    public Task<OtResponse[]> OrdenesAsync() => ObtenerAsync<OtResponse[]>("ordenes-trabajo");
    public Task<OtResponse> OrdenAsync(Guid id) => ObtenerAsync<OtResponse>(OtRuta(id));
    public Task<OtResponse> CrearOtAsync(CrearOtRequest dto) => EnviarAsync<OtResponse>("ordenes-trabajo", dto);
    public Task<OtResponse> EntregarAsync(Guid id, EntregarCilindroRequest dto) => EnviarAsync<OtResponse>(OtRuta(id) + "/entregas", dto);
    public Task<CilindroResponse[]> DisponiblesOtAsync(Guid id) => ObtenerAsync<CilindroResponse[]>(OtRuta(id) + "/cilindros-disponibles");
    public Task<OtResponse> AprobarBodegaAsync(Guid id, RevisarTrabajoRequest dto) => EnviarAsync<OtResponse>(OtRuta(id) + "/aprobar-bodega", dto);
    public Task<OtResponse> RevisarAutorizadorAsync(Guid id, RevisarTrabajoRequest dto) => EnviarAsync<OtResponse>(OtRuta(id) + "/revision-autorizador", dto);
    public Task<OtResponse> RecibirAsync(Guid id, RecibirCilindroRequest dto) => EnviarAsync<OtResponse>(OtRuta(id) + "/recepciones", dto);
    public Task<OtResponse> IncidenciaAsync(Guid id, CrearIncidenciaRequest dto) => EnviarAsync<OtResponse>(OtRuta(id) + "/incidencias", dto);
    public Task<OtResponse> ResolverAsync(Guid id, Guid incidencia, ResolverIncidenciaRequest dto) => EnviarAsync<OtResponse>(OtRuta(id) + $"/incidencias/{incidencia}/resolver", dto);
    public Task<OtResponse> TerminarAsync(Guid id, TerminarTrabajoRequest dto) => EnviarAsync<OtResponse>(OtRuta(id) + "/termino-tecnico", dto);
    public Task<EntornoResponse> EntornoAsync() => ObtenerAsync<EntornoResponse>("entorno");
    public Task<OperacionResponse> SubirFotoAsync(Guid id, Guid operacion, SubirFotoRequest dto) => EnviarAsync<OperacionResponse>(OtRuta(id) + $"/operaciones/{operacion}/fotos", dto);
    public async Task<OperacionResponse> GuardarOperacionAsync(Guid id, GuardarOperacionRequest dto)
    {
        using var respuesta = await http.PutAsJsonAsync(OtRuta(id) + "/operaciones", dto);
        await RevisarAsync(respuesta);
        return await respuesta.Content.ReadFromJsonAsync<OperacionResponse>() ?? throw new InvalidOperationException("No se pudo confirmar el guardado; consulta el borrador antes de repetir.");
    }
    public Task<OperacionResponse> ConfirmarOperacionAsync(Guid id, Guid operacion, int version) => PostSinCuerpo<OperacionResponse>(OtRuta(id) + $"/operaciones/{operacion}/confirmar?version={version}");
    public Task<OtResponse> InformarEntregaAsync(Guid id, Guid operacion) => PostSinCuerpo<OtResponse>(OtRuta(id) + $"/operaciones/{operacion}/informar-entrega");
    public Task<OtResponse> DescartarOperacionAsync(Guid id, Guid operacion, int version) => PostSinCuerpo<OtResponse>(OtRuta(id) + $"/operaciones/{operacion}/descartar?version={version}");
    public Task<OtResponse> CerrarOtAsync(Guid id) => PostSinCuerpo<OtResponse>(OtRuta(id) + "/cerrar");
    private async Task<T> PostSinCuerpo<T>(string ruta)
    {
        using var respuesta = await http.PostAsync(ruta, null); await RevisarAsync(respuesta);
        return await respuesta.Content.ReadFromJsonAsync<T>() ?? throw new InvalidOperationException("Respuesta no confirmada; vuelve a consultar.");
    }
    public async Task<OperacionResponse> QuitarFotoAsync(Guid id, Guid operacion, Guid foto, int version)
    {
        using var respuesta = await http.DeleteAsync(OtRuta(id) + $"/operaciones/{operacion}/fotos/{foto}?version={version}"); await RevisarAsync(respuesta);
        return await respuesta.Content.ReadFromJsonAsync<OperacionResponse>() ?? throw new InvalidOperationException("No se pudo confirmar la eliminación de la foto del borrador.");
    }
    public async Task<string> FotoAsync(Guid id, Guid operacion, FotoResponse foto)
    {
        using var respuesta = await http.GetAsync(OtRuta(id) + $"/operaciones/{operacion}/fotos/{foto.Id}"); await RevisarAsync(respuesta);
        return $"data:{foto.ContentType};base64," + Convert.ToBase64String(await respuesta.Content.ReadAsByteArrayAsync());
    }
    public async Task<string> FotoDespachoAsync(Guid id, EntregaResponse entrega)
    {
        using var respuesta = await http.GetAsync(OtRuta(id) + $"/entregas/{entrega.Id}/foto"); await RevisarAsync(respuesta);
        return $"data:{entrega.FotoDespacho!.ContentType};base64," + Convert.ToBase64String(await respuesta.Content.ReadAsByteArrayAsync());
    }
}
