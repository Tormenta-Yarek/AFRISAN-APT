using System.Globalization;
using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Web.Services;

public sealed partial class InventarioClient
{
    public Task<PowerBiConfiguracionResponse> PowerBiConfiguracionAsync() =>
        ObtenerAsync<PowerBiConfiguracionResponse>("power-bi/configuracion");

    public Task<BiResumenResponse> PowerBiResumenAsync(DateTime? desde,DateTime? hasta,
        int? sucursal,long? refrigerante,int? tecnico)
    {
        var query=new List<string>();
        void Agregar(string clave,string? valor) {if(valor is not null)query.Add(clave+"="+Uri.EscapeDataString(valor));}
        Agregar("desde",desde?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture));
        Agregar("hasta",hasta?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture));
        Agregar("sucursalId",sucursal?.ToString(CultureInfo.InvariantCulture));
        Agregar("refrigeranteId",refrigerante?.ToString(CultureInfo.InvariantCulture));
        Agregar("tecnicoId",tecnico?.ToString(CultureInfo.InvariantCulture));
        return ObtenerAsync<BiResumenResponse>("power-bi/resumen?"+string.Join('&',query));
    }

    public async Task<byte[]> PowerBiPaqueteAsync()
    {
        using var respuesta=await http.GetAsync("power-bi/paquete");
        await RevisarAsync(respuesta);
        return await respuesta.Content.ReadAsByteArrayAsync();
    }
}
