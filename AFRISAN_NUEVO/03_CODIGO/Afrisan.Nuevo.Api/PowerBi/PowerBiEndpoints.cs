using System.Security.Claims;
using Afrisan.Nuevo.Api.Persistencia;
using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Api.PowerBi;

public static class PowerBiEndpoints
{
    public static void MapPowerBi(this WebApplication app)
    {
        var group=app.MapGroup("/api/v1/power-bi")
            .RequireAuthorization(p=>p.RequireRole(RolesBi.Consulta.Split(',')));
        group.AddEndpointFilter(async(context,next)=>{
            context.HttpContext.Response.Headers.CacheControl="no-store";
            context.HttpContext.Response.Headers.XContentTypeOptions="nosniff";
            return await next(context);
        });
        group.MapGet("/configuracion",(PowerBiOptions config)=>config.Publica());
        group.MapGet("/resumen",async(PgPowerBi repo,ClaimsPrincipal user,DateOnly? desde,DateOnly? hasta,
            int? sucursalId,long? refrigeranteId,int? tecnicoId)=>
                PgPowerBi.Resumen(await repo.Datos(PgDatabase.Actor(user)),desde,hasta,sucursalId,refrigeranteId,tecnicoId));
        group.MapGet("/paquete",async(PgPowerBi repo,ClaimsPrincipal user)=>{
            var datos=await repo.Datos(PgDatabase.Actor(user));
            return Results.File(PowerBiPaquete.Crear(datos),"application/zip",
                $"AFRISAN_PowerBI_{datos.GeneradoUtc:yyyyMMdd_HHmmss}.zip");
        });
    }
}
