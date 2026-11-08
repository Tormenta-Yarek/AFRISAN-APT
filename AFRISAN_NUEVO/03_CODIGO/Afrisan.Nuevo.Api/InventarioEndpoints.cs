using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Afrisan.Nuevo.Contratos;
using QRCoder;

namespace Afrisan.Nuevo.Api;

public static class InventarioEndpoints
{
    public static void MapInventario(this WebApplication app)
    {
        app.MapGet("/health/ready", () => TypedResults.Ok(new { modo = "Revision", almacenamiento = "Memoria", supabase = false }));
        var api = app.MapGroup("/api/v1").AddEndpointFilter<ValidarInventarioFilter>();
        api.MapGet("/entorno", (SesionesDemo sesiones) => TypedResults.Ok(new EntornoResponse(sesiones.Instancia, "Memoria", SesionesDemo.Password, sesiones.Cuentas)));
        api.MapPost("/sesiones", Login).Produces<SesionResponse>().ProducesProblem(401);

        var datos = api.MapGroup("").RequireAuthorization();
        datos.MapGet("/refrigerantes", (InventarioStore store) => TypedResults.Ok(store.Refrigerantes()))
            .RequireAuthorization(p => p.RequireRole(Roles.Lectura.Split(',')));
        datos.MapPost("/refrigerantes", (CrearRefrigeranteRequest dto, InventarioStore store) =>
        {
            var nuevo = store.CrearRefrigerante(dto);
            return TypedResults.Created($"/api/v1/refrigerantes/{nuevo.Id}", nuevo);
        }).RequireAuthorization(p => p.RequireRole(Roles.Catalogo.Split(',')))
          .ProducesValidationProblem().ProducesProblem(409);
        datos.MapGet("/refrigerantes/{id:long}", (long id, InventarioStore store) =>
        {
            var registro = store.Refrigerantes().FirstOrDefault(r => r.Id == id)
                ?? throw new ReglaInventarioException("No se encontró el refrigerante.", 404);
            return TypedResults.Ok(registro);
        }).RequireAuthorization(p => p.RequireRole(Roles.Lectura.Split(',')));

        datos.MapGet("/cilindros", (InventarioStore store) => TypedResults.Ok(store.Cilindros()))
            .RequireAuthorization(p => p.RequireRole(Roles.Lectura.Split(',')));
        datos.MapPost("/cilindros", (CrearCilindroRequest dto, InventarioStore store, ClaimsPrincipal user) =>
        {
            var nuevo = store.CrearCilindro(dto, user.Identity!.Name!);
            return TypedResults.Created($"/api/v1/cilindros/{nuevo.Codigo}", nuevo);
        }).RequireAuthorization(p => p.RequireRole(Roles.AltaCilindro.Split(',')))
          .ProducesValidationProblem().ProducesProblem(409);
        datos.MapGet("/cilindros/{codigo}", (string codigo, InventarioStore store) => TypedResults.Ok(Obtener(store, codigo)))
            .ProducesProblem(404);
        datos.MapGet("/cilindros/{codigo}/historial", (string codigo, InventarioStore store) =>
            TypedResults.Ok(store.Eventos(Obtener(store, codigo).Id))).ProducesProblem(404);
        datos.MapPost("/cilindros/{codigo}/pesaje-inicial", (string codigo, RegistrarPesajeInicialRequest dto, InventarioStore store, ClaimsPrincipal user) =>
            TypedResults.Created($"/api/v1/cilindros/{codigo}/pesaje-inicial", store.Pesar(codigo, dto, user.Identity!.Name!)))
            .RequireAuthorization(p => p.RequireRole(Roles.AltaCilindro.Split(','))).ProducesValidationProblem().ProducesProblem(409);
        datos.MapGet("/cilindros/{codigo}/pesaje-inicial", (string codigo, InventarioStore store) =>
            TypedResults.Ok(Obtener(store, codigo).PesajeInicial ?? throw new ReglaInventarioException("Sin pesaje inicial registrado.", 404)));
        datos.MapGet("/cilindros/{codigo}/qr", (string codigo, InventarioStore store) =>
        {
            var registro = Obtener(store, codigo);
            using var data = QRCodeGenerator.GenerateQrCode(registro.Codigo, QRCodeGenerator.ECCLevel.Q);
            using var qr = new PngByteQRCode(data);
            return TypedResults.File(qr.GetGraphic(10), "image/png", $"{registro.Codigo}.png");
        }).ProducesProblem(404);
    }

    private static CilindroResponse Obtener(InventarioStore store, string codigo) =>
        store.Cilindro(codigo) ?? throw new ReglaInventarioException("No se encontró el código de cilindro.", 404);

    private static IResult Login(LoginRequest dto, SesionesDemo sesiones)
    {
        var resultado = sesiones.Login(dto);
        return resultado is null
            ? TypedResults.Problem(statusCode: 401, title: "No se pudo iniciar sesión", detail: "Credenciales incorrectas o cuenta temporalmente bloqueada. Cinco intentos fallidos bloquean la cuenta 15 minutos.")
            : TypedResults.Ok(resultado);
    }
}

// .NET 8 no valida automáticamente DataAnnotations en Minimal APIs.
public sealed class ValidarInventarioFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var dto in context.Arguments.Where(a => a?.GetType().Namespace == typeof(CrearCilindroRequest).Namespace))
        {
            var resultados = new List<ValidationResult>();
            if (!Validator.TryValidateObject(dto!, new ValidationContext(dto!), resultados, true))
                return TypedResults.ValidationProblem(resultados.SelectMany(v => v.MemberNames.DefaultIfEmpty("datos").Select(m => (m, v.ErrorMessage)))
                    .GroupBy(v => v.m).ToDictionary(g => g.Key, g => g.Select(v => v.ErrorMessage ?? "Dato inválido.").ToArray()));
        }
        try { return await next(context); }
        catch (ReglaInventarioException ex)
        {
            return TypedResults.Problem(statusCode: ex.Estado, title: "Revisa los datos de inventario", detail: ex.Message);
        }
    }
}
