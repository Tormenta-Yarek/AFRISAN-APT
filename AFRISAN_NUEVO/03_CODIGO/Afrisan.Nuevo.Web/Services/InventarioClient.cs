using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Web.Services;

public sealed partial class InventarioClient(HttpClient http, SesionService sesion)
{
    public Task<ClienteResponse[]> ClientesAsync()=>ObtenerAsync<ClienteResponse[]>("clientes");
    public Task<SucursalResponse[]> SucursalesAsync()=>ObtenerAsync<SucursalResponse[]>("sucursales");
    public Task<CuentaDemoResponse[]> TecnicosAsync()=>ObtenerAsync<CuentaDemoResponse[]>("tecnicos");
    public Task<UsuarioResponse[]> UsuariosAsync()=>ObtenerAsync<UsuarioResponse[]>("usuarios");
    public Task<ClienteResponse> CrearClienteAsync(CrearClienteRequest dto)=>EnviarAsync<ClienteResponse>("clientes",dto);
    public Task<SucursalResponse> CrearSucursalAsync(CrearSucursalRequest dto)=>EnviarAsync<SucursalResponse>("sucursales",dto);
    public Task<UsuarioResponse> CrearUsuarioAsync(CrearUsuarioRequest dto)=>EnviarAsync<UsuarioResponse>("usuarios",dto);
    public Task<RefrigeranteResponse[]> RefrigerantesAsync() => ObtenerAsync<RefrigeranteResponse[]>("refrigerantes");
    public Task<CilindroResponse[]> CilindrosAsync() => ObtenerAsync<CilindroResponse[]>("cilindros");
    public Task<CilindroResponse> CilindroAsync(string codigo) => ObtenerAsync<CilindroResponse>($"cilindros/{Uri.EscapeDataString(codigo)}");
    public Task<EventoResponse[]> HistorialAsync(string codigo) => ObtenerAsync<EventoResponse[]>($"cilindros/{Uri.EscapeDataString(codigo)}/historial");
    public Task<RefrigeranteResponse> CrearRefrigeranteAsync(CrearRefrigeranteRequest dto) => EnviarAsync<RefrigeranteResponse>("refrigerantes", dto);
    public Task<CilindroResponse> CrearCilindroAsync(CrearCilindroRequest dto) => EnviarAsync<CilindroResponse>("cilindros", dto);
    public Task<CilindroResponse> PesarAsync(string codigo, RegistrarPesajeInicialRequest dto) => EnviarAsync<CilindroResponse>($"cilindros/{Uri.EscapeDataString(codigo)}/pesaje-inicial", dto);
    public Task<SesionResponse> LoginAsync(LoginRequest dto) => EnviarAsync<SesionResponse>("sesiones", dto);

    public async Task<string> QrAsync(string codigo)
    {
        using var respuesta = await http.GetAsync($"cilindros/{Uri.EscapeDataString(codigo)}/qr");
        await RevisarAsync(respuesta);
        return "data:image/png;base64," + Convert.ToBase64String(await respuesta.Content.ReadAsByteArrayAsync());
    }

    private async Task<T> ObtenerAsync<T>(string ruta)
    {
        using var respuesta = await http.GetAsync(ruta);
        await RevisarAsync(respuesta);
        return await respuesta.Content.ReadFromJsonAsync<T>() ?? throw new InvalidOperationException("La API no devolvió datos válidos.");
    }
    private async Task<T> EnviarAsync<T>(string ruta, object dto)
    {
        using var respuesta = await http.PostAsJsonAsync(ruta, dto);
        await RevisarAsync(respuesta);
        return await respuesta.Content.ReadFromJsonAsync<T>() ?? throw new InvalidOperationException("No se pudo interpretar la respuesta. Actualiza antes de repetir la operación.");
    }
    private async Task RevisarAsync(HttpResponseMessage respuesta)
    {
        if (respuesta.IsSuccessStatusCode) return;
        if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Un login fallido tiene su propio detalle; una sesión vencida se elimina.
            if (sesion.Sesion is not null) await sesion.SalirAsync();
        }
        var mensaje = respuesta.StatusCode == HttpStatusCode.Forbidden
            ? "Tu rol no puede realizar esta acción." : "No se pudo completar la operación.";
        try
        {
            using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
            if (json.RootElement.TryGetProperty("detail", out var detalle)) mensaje = detalle.GetString() ?? mensaje;
            else if (json.RootElement.TryGetProperty("errors", out var errores))
                mensaje = string.Join(" ", errores.EnumerateObject().SelectMany(e => e.Value.EnumerateArray()).Select(e => e.GetString()));
        }
        catch (JsonException) { }
        throw new InvalidOperationException(mensaje);
    }
}
