using System.Security.Claims;
using Afrisan.Nuevo.Contratos;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using QRCoder;

namespace Afrisan.Nuevo.Api.Persistencia;

public static class PgEndpoints
{
    public static void MapPersistencia(this WebApplication app)
    {
        app.MapGet("/health/ready",async(PgDatabase db,StorageOptions storage)=>{await using var u=await db.Open();await u.One<int>("SELECT 1 AS \"Value\"");return TypedResults.Ok(new{modo="Persistente",almacenamiento="PostgreSQL",supabase=true,fotografias=storage.Nube?"SupabaseStorage":"DiscoPrivado"});});
        var api=app.MapGroup("/api/v1").AddEndpointFilter<ValidarInventarioFilter>();
        api.MapGet("/entorno",(PersistenciaOptions config)=>TypedResults.Ok(new EntornoResponse(config.Instancia,"PostgreSQL","",[])));
        api.MapPost("/sesiones",async(LoginRequest request,PgUsuarios usuarios)=>{
            var sesion=await usuarios.Login(request);
            return sesion is null?Results.Problem(statusCode:401,title:"No se pudo iniciar sesión",detail:"Credenciales incorrectas, cuenta inactiva o temporalmente bloqueada."):Results.Ok(sesion);
        });
        var datos=api.MapGroup("").RequireAuthorization();
        datos.MapGet("/refrigerantes",async(PgInventario repo)=>await repo.Refrigerantes()).RequireAuthorization(p=>p.RequireRole(Roles.Lectura.Split(',')));
        datos.MapGet("/refrigerantes/{id:long}",async(long id,PgInventario repo)=>(await repo.Refrigerantes()).FirstOrDefault(r=>r.Id==id)??throw new ReglaInventarioException("Refrigerante no encontrado.",404)).RequireAuthorization(p=>p.RequireRole(Roles.Lectura.Split(',')));
        datos.MapPost("/refrigerantes",async(CrearRefrigeranteRequest dto,PgInventario repo,ClaimsPrincipal user)=>{var row=await repo.CrearRefrigerante(dto,PgDatabase.Actor(user));return TypedResults.Created($"/api/v1/refrigerantes/{row.Id}",row);}).RequireAuthorization(p=>p.RequireRole(Roles.Catalogo.Split(',')));
        datos.MapGet("/cilindros",async(PgInventario repo)=>await repo.Cilindros()).RequireAuthorization(p=>p.RequireRole(Roles.Lectura.Split(',')));
        datos.MapGet("/cilindros/{codigo}",async(string codigo,PgInventario repo)=>await repo.Cilindro(codigo));
        datos.MapPost("/cilindros",async(CrearCilindroRequest dto,PgInventario repo,ClaimsPrincipal user)=>{var row=await repo.CrearCilindro(dto,PgDatabase.Actor(user));return TypedResults.Created($"/api/v1/cilindros/{row.Codigo}",row);}).RequireAuthorization(p=>p.RequireRole(Roles.AltaCilindro.Split(',')));
        datos.MapGet("/cilindros/{codigo}/historial",async(string codigo,PgInventario repo)=>await repo.Eventos(codigo));
        datos.MapGet("/cilindros/{codigo}/pesaje-inicial",async(string codigo,PgInventario repo)=>(await repo.Cilindro(codigo)).PesajeInicial??throw new ReglaInventarioException("Sin pesaje inicial.",404));
        datos.MapPost("/cilindros/{codigo}/pesaje-inicial",async(string codigo,RegistrarPesajeInicialRequest dto,PgInventario repo,ClaimsPrincipal user)=>await repo.Pesar(codigo,dto,PgDatabase.Actor(user))).RequireAuthorization(p=>p.RequireRole(Roles.AltaCilindro.Split(',')));
        datos.MapGet("/cilindros/{codigo}/qr",async(string codigo,PgInventario repo)=>{var row=await repo.Cilindro(codigo);using var data=QRCodeGenerator.GenerateQrCode(row.Codigo,QRCodeGenerator.ECCLevel.Q);using var qr=new PngByteQRCode(data);return TypedResults.File(qr.GetGraphic(10),"image/png",row.Codigo+".png");});
        var ot=datos.MapGroup("/ordenes-trabajo");
        ot.MapGet("",async(PgTrabajo repo,ClaimsPrincipal user)=>await repo.Listar(user));
        ot.MapGet("/{id:guid}",async(Guid id,PgTrabajo repo,ClaimsPrincipal user)=>await repo.Obtener(id,user));
        ot.MapPost("",async(CrearOtRequest dto,PgTrabajo repo,ClaimsPrincipal user)=>{var row=await repo.Crear(dto,user);return TypedResults.Created($"/api/v1/ordenes-trabajo/{row.Id}",row);}).RequireAuthorization(p=>p.RequireRole("Autorizador"));
        ot.MapGet("/{id:guid}/cilindros-disponibles",async(Guid id,PgTrabajo repo,ClaimsPrincipal user)=>await repo.Disponibles(id,user)).RequireAuthorization(p=>p.RequireRole("Tecnico"));
        ot.MapPost("/{id:guid}/entregas",async(Guid id,EntregarCilindroRequest dto,PgTrabajo repo,ClaimsPrincipal user)=>await repo.Entregar(id,dto,user)).RequireAuthorization(p=>p.RequireRole("Tecnico"));
        ot.MapPut("/{id:guid}/operaciones",async(Guid id,GuardarOperacionRequest dto,PgTrabajo repo,ClaimsPrincipal user)=>await repo.Guardar(id,dto,user)).RequireAuthorization(p=>p.RequireRole("Tecnico"));
        ot.MapPost("/{id:guid}/operaciones/{operacion:guid}/fotos",async(Guid id,Guid operacion,SubirFotoRequest dto,PgTrabajo repo,ClaimsPrincipal user)=>await repo.Foto(id,operacion,dto,user)).RequireAuthorization(p=>p.RequireRole("Tecnico"));
        ot.MapDelete("/{id:guid}/operaciones/{operacion:guid}/fotos/{foto:guid}",async(Guid id,Guid operacion,Guid foto,int version,PgTrabajo repo,ClaimsPrincipal user)=>await repo.QuitarFoto(id,operacion,foto,version,user)).RequireAuthorization(p=>p.RequireRole("Tecnico"));
        ot.MapPost("/{id:guid}/operaciones/{operacion:guid}/confirmar",async(Guid id,Guid operacion,int version,PgTrabajo repo,ClaimsPrincipal user)=>await repo.Confirmar(id,operacion,version,user)).RequireAuthorization(p=>p.RequireRole("Tecnico"));
        ot.MapPost("/{id:guid}/operaciones/{operacion:guid}/informar-entrega",async(Guid id,Guid operacion,PgTrabajo repo,ClaimsPrincipal user)=>await repo.InformarEntrega(id,operacion,user)).RequireAuthorization(p=>p.RequireRole("Tecnico"));
        ot.MapPost("/{id:guid}/operaciones/{operacion:guid}/descartar",async(Guid id,Guid operacion,int version,PgTrabajo repo,ClaimsPrincipal user)=>await repo.Descartar(id,operacion,version,user)).RequireAuthorization(p=>p.RequireRole("Tecnico"));
        ot.MapPost("/{id:guid}/recepciones",async(Guid id,RecibirCilindroRequest dto,PgTrabajo repo,ClaimsPrincipal user)=>await repo.Recibir(id,dto,user)).RequireAuthorization(p=>p.RequireRole("Bodega"));
        ot.MapPost("/{id:guid}/incidencias",async(Guid id,CrearIncidenciaRequest dto,PgTrabajo repo,ClaimsPrincipal user)=>await repo.Incidencia(id,dto,user)).RequireAuthorization(p=>p.RequireRole("Tecnico","Bodega"));
        ot.MapPost("/{id:guid}/incidencias/{incidencia:guid}/resolver",async(Guid id,Guid incidencia,ResolverIncidenciaRequest dto,PgTrabajo repo,ClaimsPrincipal user)=>await repo.Resolver(id,incidencia,dto,user)).RequireAuthorization(p=>p.RequireRole("Autorizador"));
        ot.MapPost("/{id:guid}/termino-tecnico",async(Guid id,TerminarTrabajoRequest dto,PgTrabajo repo,ClaimsPrincipal user)=>await repo.Transicion(id,"termino",dto.Observacion,true,user)).RequireAuthorization(p=>p.RequireRole("Tecnico"));
        ot.MapPost("/{id:guid}/aprobar-bodega",async(Guid id,RevisarTrabajoRequest dto,PgTrabajo repo,ClaimsPrincipal user)=>await repo.Transicion(id,"bodega",dto.Observacion,dto.ConfirmoRevision,user)).RequireAuthorization(p=>p.RequireRole("Bodega"));
        ot.MapPost("/{id:guid}/revision-autorizador",async(Guid id,RevisarTrabajoRequest dto,PgTrabajo repo,ClaimsPrincipal user)=>await repo.Transicion(id,"autorizador",dto.Observacion,dto.ConfirmoRevision,user)).RequireAuthorization(p=>p.RequireRole("Autorizador"));
        ot.MapPost("/{id:guid}/cerrar",async(Guid id,PgTrabajo repo,ClaimsPrincipal user)=>await repo.Transicion(id,"cerrar",null,true,user)).RequireAuthorization(p=>p.RequireRole("Autorizador"));
        static async Task<IResult> Photo(Guid id,Guid? operation,Guid photo,PgTrabajo repo,ClaimsPrincipal user,HttpContext context) {var f=await repo.LeerFoto(id,operation,photo,user);context.Response.Headers.CacheControl="no-store";context.Response.Headers.XContentTypeOptions="nosniff";return Results.File(f.Bytes,f.Mime);}
        ot.MapGet("/{id:guid}/entregas/{entrega:guid}/foto",async(Guid id,Guid entrega,PgTrabajo repo,ClaimsPrincipal user,HttpContext ctx)=>await Photo(id,null,entrega,repo,user,ctx));
        ot.MapGet("/{id:guid}/operaciones/{operacion:guid}/fotos/{foto:guid}",async(Guid id,Guid operacion,Guid foto,PgTrabajo repo,ClaimsPrincipal user,HttpContext ctx)=>await Photo(id,operacion,foto,repo,user,ctx));
        datos.MapGet("/usuarios",async(PgUsuarios repo)=>await repo.Listar()).RequireAuthorization(p=>p.RequireRole("Administrador"));
        datos.MapPost("/usuarios",async(CrearUsuarioRequest dto,PgUsuarios repo,ClaimsPrincipal user)=>await repo.Crear(dto,PgDatabase.Actor(user))).RequireAuthorization(p=>p.RequireRole("Administrador"));
        datos.MapGet("/tecnicos",async(PgUsuarios repo)=>(await repo.Listar()).Where(u=>u.Activo&&u.Rol=="Tecnico").Select(u=>new CuentaDemoResponse($"{u.Nombre} {u.Apellido}",u.Correo,u.Rol)).ToArray()).RequireAuthorization(p=>p.RequireRole("Administrador","Autorizador"));
        datos.MapGet("/clientes",async(PgDatabase db)=>{await using var u=await db.Open();return await u.Query<ClienteResponse>("SELECT \"Id_Cliente\" AS \"Id\",\"RutCliente\" AS \"Rut\",\"NombreEmpresa\" AS \"Nombre\" FROM afrisan.\"Cliente\" WHERE \"Activo\" ORDER BY \"NombreEmpresa\"");}).RequireAuthorization(p=>p.RequireRole(Roles.Lectura.Split(',')));
        datos.MapPost("/clientes",async(CrearClienteRequest dto,PgDatabase db,ClaimsPrincipal user)=>{await using var u=await db.Open(PgDatabase.Actor(user));var c=await u.One<ClienteResponse>("INSERT INTO afrisan.\"Cliente\"(\"RutCliente\",\"NombreEmpresa\") VALUES(@p0,@p1) RETURNING \"Id_Cliente\" AS \"Id\",\"RutCliente\" AS \"Rut\",\"NombreEmpresa\" AS \"Nombre\"",dto.Rut.Trim(),dto.Nombre.Trim());await u.Commit();return c;}).RequireAuthorization(p=>p.RequireRole("Administrador"));
        datos.MapGet("/sucursales",async(PgDatabase db)=>{await using var u=await db.Open();return await u.Query<SucursalResponse>(SucursalesSql+" WHERE s.\"Activo\" AND c.\"Activo\" ORDER BY c.\"NombreEmpresa\",s.\"NombreSucursal\"");}).RequireAuthorization(p=>p.RequireRole(Roles.Lectura.Split(',')));
        datos.MapPost("/sucursales",async(CrearSucursalRequest dto,PgDatabase db,ClaimsPrincipal user)=>{await using var u=await db.Open(PgDatabase.Actor(user));var id=await u.One<int>("INSERT INTO afrisan.\"Sucursales\"(\"Id_Cliente\",\"NombreSucursal\",\"Direccion\") VALUES(@p0,@p1,@p2) RETURNING \"Id_Sucursal\" AS \"Value\"",dto.ClienteId,dto.Nombre.Trim(),dto.Direccion.Trim());var s=await u.One<SucursalResponse>(SucursalesSql+" WHERE s.\"Id_Sucursal\"=@p0",id);await u.Commit();return s;}).RequireAuthorization(p=>p.RequireRole("Administrador"));
    }
    private const string SucursalesSql="SELECT s.\"Id_Sucursal\" AS \"Id\",s.\"Id_Cliente\" AS \"ClienteId\",c.\"NombreEmpresa\" AS \"Cliente\",s.\"NombreSucursal\" AS \"Nombre\",s.\"Direccion\" FROM afrisan.\"Sucursales\" s JOIN afrisan.\"Cliente\" c USING(\"Id_Cliente\")";
}

public sealed class PgExceptionHandler(ILogger<PgExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context,Exception exception,CancellationToken cancellationToken)
    {
        var error=exception;
        while(error.InnerException is not null&&error is not (NpgsqlException or ReglaInventarioException or BadHttpRequestException or TimeoutException)) error=error.InnerException;
        var (code,message)=error switch {
            ReglaInventarioException r=>(r.Estado,r.Message),
            BadHttpRequestException b=>(b.StatusCode,"Revisa el formato de los parámetros y el contenido de la solicitud."),
            PostgresException {SqlState:"P0001"} p=>(409,p.MessageText),
            PostgresException {SqlState:"23505"}=>(409,"Ya existe un registro con ese identificador, código o referencia."),
            PostgresException {SqlState:"23503" or "23502" or "23514"}=>(400,"Falta una referencia válida o los datos incumplen una regla del registro."),
            PostgresException {SqlState:"42501"}=>(403,"La identidad no tiene permiso para esta operación."),
            PostgresException {SqlState:"40001" or "40P01"}=>(409,"Otro proceso modificó estos datos. Actualiza y vuelve a intentar."),
            NpgsqlException or TimeoutException=>(503,"La base de datos no está disponible. El guardado no fue confirmado; conserva el borrador y vuelve a consultar."),
            _=>(500,"No se pudo completar la operación.")};
        if (code >= 500)
            logger.LogError(exception, "Error controlado en solicitud {Method} {Path} (HTTP {StatusCode})", context.Request.Method, context.Request.Path, code);
        else
            logger.LogWarning("Rechazo de regla de dominio o datos en {Method} {Path} (HTTP {StatusCode}): {Detalle}", context.Request.Method, context.Request.Path, code, message);
        context.Response.StatusCode=code;
        await context.Response.WriteAsJsonAsync(new ProblemDetails{Status=code,Title="No se pudo completar la operación",Detail=message},cancellationToken);
        return true;
    }
}
