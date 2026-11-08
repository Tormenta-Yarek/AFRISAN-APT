using System.Security.Claims;
using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Api;

public static class TrabajoEndpoints
{
    public static void MapTrabajo(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/v1/ordenes-trabajo").RequireAuthorization()
            .AddEndpointFilter<ValidarInventarioFilter>().WithTags("Trabajo técnico");
        grupo.MapGet("", (TrabajoStore store, ClaimsPrincipal user) => TypedResults.Ok(store.Listar(user)));
        grupo.MapPost("", (CrearOtRequest dto, TrabajoStore store, ClaimsPrincipal user) =>
        {
            var ot = store.Crear(dto, user);
            return TypedResults.Created($"/api/v1/ordenes-trabajo/{ot.Id}", ot);
        }).RequireAuthorization(p => p.RequireRole("Autorizador"));
        grupo.MapGet("/{id:guid}", (Guid id, TrabajoStore store, ClaimsPrincipal user) => TypedResults.Ok(store.Obtener(id, user)));
        grupo.MapGet("/{id:guid}/cilindros-disponibles", (Guid id, TrabajoStore store, ClaimsPrincipal user) => TypedResults.Ok(store.Disponibles(id, user)))
            .RequireAuthorization(p => p.RequireRole("Tecnico"));
        grupo.MapPost("/{id:guid}/entregas", (Guid id, EntregarCilindroRequest dto, TrabajoStore store, ClaimsPrincipal user) => TypedResults.Ok(store.Entregar(id, dto, user)))
            .RequireAuthorization(p => p.RequireRole("Tecnico"));
        grupo.MapGet("/{id:guid}/entregas/{entrega:guid}/foto", (Guid id, Guid entrega, TrabajoStore store, ClaimsPrincipal user, HttpContext ctx) =>
        {
            var imagen = store.LeerFotoDespacho(id, entrega, user);
            ctx.Response.Headers.CacheControl = "no-store";
            ctx.Response.Headers.XContentTypeOptions = "nosniff";
            return TypedResults.File(imagen.Bytes, imagen.Mime);
        });
        grupo.MapPost("/{id:guid}/recepciones", (Guid id, RecibirCilindroRequest dto, TrabajoStore store, ClaimsPrincipal user) => TypedResults.Ok(store.Recibir(id, dto, user)))
            .RequireAuthorization(p => p.RequireRole("Bodega"));
        grupo.MapPut("/{id:guid}/operaciones", (Guid id, GuardarOperacionRequest dto, TrabajoStore store, ClaimsPrincipal user) => TypedResults.Ok(store.Guardar(id, dto, user)))
            .RequireAuthorization(p => p.RequireRole("Tecnico"));
        grupo.MapPost("/{id:guid}/operaciones/{operacion:guid}/fotos", (Guid id, Guid operacion, SubirFotoRequest dto, TrabajoStore store, ClaimsPrincipal user) => TypedResults.Ok(store.Foto(id, operacion, dto, user)))
            .RequireAuthorization(p => p.RequireRole("Tecnico"));
        grupo.MapDelete("/{id:guid}/operaciones/{operacion:guid}/fotos/{foto:guid}", (Guid id, Guid operacion, Guid foto, int version, TrabajoStore store, ClaimsPrincipal user) => TypedResults.Ok(store.QuitarFoto(id, operacion, foto, user, version)))
            .RequireAuthorization(p => p.RequireRole("Tecnico"));
        grupo.MapGet("/{id:guid}/operaciones/{operacion:guid}/fotos/{foto:guid}", (Guid id, Guid operacion, Guid foto, TrabajoStore store, ClaimsPrincipal user, HttpContext ctx) =>
        {
            var imagen = store.LeerFoto(id, operacion, foto, user);
            ctx.Response.Headers.CacheControl = "no-store";
            ctx.Response.Headers.XContentTypeOptions = "nosniff";
            return TypedResults.File(imagen.Bytes, imagen.Mime);
        });
        grupo.MapPost("/{id:guid}/operaciones/{operacion:guid}/confirmar", (Guid id, Guid operacion, int version, TrabajoStore store, ClaimsPrincipal user) => TypedResults.Ok(store.Confirmar(id, operacion, user, version)))
            .RequireAuthorization(p => p.RequireRole("Tecnico"));
        grupo.MapPost("/{id:guid}/operaciones/{operacion:guid}/informar-entrega", (Guid id, Guid operacion, TrabajoStore store, ClaimsPrincipal user) => TypedResults.Ok(store.InformarEntrega(id, operacion, user)))
            .RequireAuthorization(p => p.RequireRole("Tecnico"));
        grupo.MapPost("/{id:guid}/incidencias", (Guid id, CrearIncidenciaRequest dto, TrabajoStore store, ClaimsPrincipal user) => TypedResults.Ok(store.Incidencia(id, dto, user)))
            .RequireAuthorization(p => p.RequireRole("Tecnico", "Bodega"));
        grupo.MapPost("/{id:guid}/incidencias/{incidencia:guid}/resolver", (Guid id, Guid incidencia, ResolverIncidenciaRequest dto, TrabajoStore store, ClaimsPrincipal user) => TypedResults.Ok(store.Resolver(id, incidencia, dto, user)))
            .RequireAuthorization(p => p.RequireRole("Autorizador"));
        grupo.MapPost("/{id:guid}/operaciones/{operacion:guid}/descartar", (Guid id, Guid operacion, int version, TrabajoStore store, ClaimsPrincipal user) => TypedResults.Ok(store.Descartar(id, operacion, version, user)))
            .RequireAuthorization(p => p.RequireRole("Tecnico"));
        grupo.MapPost("/{id:guid}/termino-tecnico", (Guid id, TerminarTrabajoRequest dto, TrabajoStore store, ClaimsPrincipal user) => TypedResults.Ok(store.Terminar(id, dto, user)))
            .RequireAuthorization(p => p.RequireRole("Tecnico"));
        grupo.MapPost("/{id:guid}/aprobar-bodega", (Guid id, RevisarTrabajoRequest dto, TrabajoStore store, ClaimsPrincipal user) => TypedResults.Ok(store.AprobarBodega(id, dto, user)))
            .RequireAuthorization(p => p.RequireRole("Bodega"));
        grupo.MapPost("/{id:guid}/revision-autorizador", (Guid id, RevisarTrabajoRequest dto, TrabajoStore store, ClaimsPrincipal user) => TypedResults.Ok(store.RevisarAutorizador(id, dto, user)))
            .RequireAuthorization(p => p.RequireRole("Autorizador"));
        grupo.MapPost("/{id:guid}/cerrar", (Guid id, TrabajoStore store, ClaimsPrincipal user) => TypedResults.Ok(store.Cerrar(id, user)))
            .RequireAuthorization(p => p.RequireRole("Autorizador"));
    }
}
