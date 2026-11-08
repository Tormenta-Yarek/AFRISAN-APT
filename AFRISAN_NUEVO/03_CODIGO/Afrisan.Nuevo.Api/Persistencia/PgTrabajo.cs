using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Afrisan.Nuevo.Contratos;
namespace Afrisan.Nuevo.Api.Persistencia;

public sealed class PgTrabajo(PgDatabase database,FotosPrivadas fotos,PersistenciaOptions settings)
{
    private const string OtSql="""
        SELECT o."Id_OrdenTrabajo" AS "Id",o."Id_Publico" AS "Publico",o."CodigoOT" AS "Codigo",
          s."Id_Sucursal" AS "SucursalId",s."NombreSucursal" AS "Sucursal",s."Direccion",o."Descripcion",
          o."Id_Tecnico" AS "TecnicoId",t."Correo_Usuario" AS "Tecnico",o."Estado",a."Correo_Usuario" AS "Autorizador",
          o."FechaCreacion" AS "Fecha",o."ObservacionTermino",o."HuellaCreacion" AS "Huella"
        FROM afrisan."OrdenTrabajo" o JOIN afrisan."Sucursales" s USING("Id_Sucursal")
        JOIN afrisan."Usuario" t ON t."Id_Usuario"=o."Id_Tecnico" JOIN afrisan."Usuario" a ON a."Id_Usuario"=o."Id_Autorizador"
        """;
    private const string OpSql="""
        SELECT "Id_Operacion" AS "Id","Id_OrdenTrabajo" AS "OtId","Tipo","Version","Estado",
          "DatosBorrador"::text AS "Datos","FotosBorrador"::text AS "Fotos","FechaRecepcion" AS "Fecha",
          "FechaConfirmacion" AS "Confirmacion","FechaEntregaInformada" AS "Entrega",
          "Id_PesajeInstalado" AS "PesajeInstalado","Id_PesajeRetirado" AS "PesajeRetirado"
        FROM afrisan."OperacionTerreno"
        """;
    private static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);
    private static T Read<T>(string json)=>JsonSerializer.Deserialize<T>(json,Json)??throw new ReglaInventarioException("El registro persistido no se pudo interpretar.",503);
    private static string Write<T>(T value)=>JsonSerializer.Serialize(value,Json);
    private static void Rol(ClaimsPrincipal user,params string[] roles) {if(!roles.Any(user.IsInRole)) throw new ReglaInventarioException("Tu rol no permite esta acción.",403);}
    private static void Editable(OtRow o) {if(o.Estado is not("Asignada" or "EnProcesoDespacho" or "EnTerreno")) throw new ReglaInventarioException("La OT ya fue enviada a revisión.",409);}
    private static void Version(OperacionRow op,int version) {if(op.Version!=version) throw new ReglaInventarioException("Otra sesión modificó el borrador. Consulta la versión actual.",409);}
    private async Task<OtRow> Acceder(PgUnit u,Guid id,ClaimsPrincipal user,bool bloquear=true)
    {
        Rol(user,"Administrador","Jefatura","Autorizador","Bodega","Tecnico");
        var o=(await u.Query<OtRow>(OtSql+" WHERE o.\"Id_Publico\"=@p0"+(bloquear?" FOR UPDATE OF o":""),id)).SingleOrDefault()
            ??throw new ReglaInventarioException("OT no encontrada.",404);
        if(user.IsInRole("Tecnico")&&o.TecnicoId!=PgDatabase.Actor(user)) throw new ReglaInventarioException("La OT pertenece a otro técnico.",403);
        return o;
    }
    private static async Task<OperacionRow> Operacion(PgUnit u,long ot,Guid id)=>
        (await u.Query<OperacionRow>(OpSql+" WHERE \"Id_Operacion\"=@p0 AND \"Id_OrdenTrabajo\"=@p1 FOR UPDATE",id,ot)).SingleOrDefault()
        ??throw new ReglaInventarioException("Operación no encontrada.",404);
    private static async Task<CilindroRow> Cilindro(PgUnit u,string? codigo)=>
        (await u.Query<CilindroRow>(PgInventario.CilindrosSql+" WHERE upper(c.\"CilindroQR\")=@p0",codigo?.Trim().ToUpperInvariant()??"")).SingleOrDefault()
        ??throw new ReglaInventarioException("Cilindro no encontrado.",404);
    public async Task<OtResponse[]> Listar(ClaimsPrincipal user)
    {
        await using var u=await database.Open(PgDatabase.Actor(user));
        var list=await u.Query<OtRow>(OtSql+(user.IsInRole("Tecnico")?" WHERE o.\"Id_Tecnico\"=@p0":" WHERE @p0>0")+" ORDER BY o.\"Id_OrdenTrabajo\" DESC",PgDatabase.Actor(user));
        var result=new List<OtResponse>();foreach(var o in list) result.Add(await Ver(u,o));return result.ToArray();
    }
    public async Task<OtResponse> Obtener(Guid id,ClaimsPrincipal user)
    {await using var u=await database.Open(PgDatabase.Actor(user));return await Ver(u,await Acceder(u,id,user,false));}
    public async Task<CilindroResponse[]> Disponibles(Guid id,ClaimsPrincipal user)
    {
        Rol(user,"Tecnico");await using var u=await database.Open(PgDatabase.Actor(user));var o=await Acceder(u,id,user);
        if(o.Estado is not("Asignada" or "EnProcesoDespacho" or "EnTerreno")) return [];
        return await PgInventario.Map(u,await u.Query<CilindroRow>(PgInventario.CilindrosSql+"""
             WHERE c."Ubicacion"='Bodega' AND c."Estado"='Disponible' AND c."Id_UltimoPesaje" IS NOT NULL
             AND EXISTS(SELECT 1 FROM afrisan."DetalleOT" d WHERE d."Id_OrdenTrabajo"=@p0 AND d."Id_Refrigerante"=c."Id_Refrigerante")
            """,o.Id));
    }
    public async Task<OtResponse> Crear(CrearOtRequest dto,ClaimsPrincipal user)
    {
        Rol(user,"Autorizador");var actor=PgDatabase.Actor(user);
        if(dto.IdOperacion==Guid.Empty||dto.SucursalId is null) throw new ReglaInventarioException("Selecciona una sucursal registrada e identifica la solicitud.");
        if(dto.Detalle.Length is <1 or >20||dto.Detalle.Select(x=>x.RefrigeranteId).Distinct().Count()!=dto.Detalle.Length) throw new ReglaInventarioException("El detalle debe contener refrigerantes diferentes.");
        var codigos=dto.CilindrosARetirar.Select(c=>c.Trim().ToUpperInvariant()).Order().ToArray();
        if(codigos.Any(string.IsNullOrWhiteSpace)||codigos.Distinct().Count()!=codigos.Length) throw new ReglaInventarioException("Hay cilindros vacíos o repetidos.");
        var huella=Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new{Actor=actor,dto.SucursalId,Descripcion=dto.Descripcion.Trim(),Tecnico=dto.TecnicoEmail.Trim().ToLowerInvariant(),Detalle=dto.Detalle.OrderBy(x=>x.RefrigeranteId),Cilindros=codigos})));
        await using var u=await database.Open(actor);
        await u.Query<long>("SELECT 1::bigint AS \"Value\" FROM pg_advisory_xact_lock(hashtextextended(@p0,0))",dto.IdOperacion.ToString());
        var previous=(await u.Query<OtRow>(OtSql+" WHERE o.\"Id_Publico\"=@p0",dto.IdOperacion)).SingleOrDefault();
        if(previous is not null) {if(previous.Huella!=huella) throw new ReglaInventarioException("El UUID de creación ya corresponde a otro pedido.",409);return await Ver(u,previous);}
        var id=await u.One<long>("""
            INSERT INTO afrisan."OrdenTrabajo"("Id_Publico","CodigoOT","Id_Sucursal","Id_Tecnico","Id_Autorizador","Descripcion","HuellaCreacion")
            VALUES(@p0,@p1,@p2,(SELECT u."Id_Usuario" FROM afrisan."Usuario" u JOIN afrisan."Rol" r USING("Id_Rol")
              WHERE lower(u."Correo_Usuario")=@p3 AND u."Activo" AND r."Nombre_Rol"='Tecnico'),@p4,@p5,@p6)
            RETURNING "Id_OrdenTrabajo" AS "Value"
            """,dto.IdOperacion,"OT-"+dto.IdOperacion.ToString("N").ToUpperInvariant(),dto.SucursalId,dto.TecnicoEmail.Trim().ToLowerInvariant(),actor,dto.Descripcion.Trim(),huella);
        foreach(var d in dto.Detalle) await u.Exec("INSERT INTO afrisan.\"DetalleOT\"(\"Id_OrdenTrabajo\",\"Id_Refrigerante\",\"GrSolicitados\",\"GrAutorizados\") VALUES(@p0,@p1,@p2,@p3)",id,d.RefrigeranteId,d.GrSolicitados,d.GrAutorizados);
        foreach(var codigo in codigos) {var c=await Cilindro(u,codigo);await u.Exec("INSERT INTO afrisan.\"RetiroPrevisto\"(\"Id_OrdenTrabajo\",\"Id_Cilindro\") VALUES(@p0,@p1)",id,c.Id);}
        await Evento(u,id,actor,"OT autorizada","Pedido manual persistido; no corresponde a importación Defontana.");
        var result=await Ver(u,await Acceder(u,dto.IdOperacion,user));await u.Commit();return result;
    }
    public async Task<OtResponse> Entregar(Guid id,EntregarCilindroRequest dto,ClaimsPrincipal user)
    {
        Rol(user,"Tecnico");await using var u=await database.Open(PgDatabase.Actor(user));var o=await Acceder(u,id,user);
        if(dto.IdOperacion==Guid.Empty||dto.Foto is null) throw new ReglaInventarioException("Falta el UUID del retiro o su foto.");
        var c=await Cilindro(u,dto.Codigo);
        var anterior=(await u.Query<EvidenciaRow>(EvidenciasSql+" JOIN afrisan.\"Despacho\" d ON d.\"Id_Pesaje\"=e.\"Id_Pesaje\" WHERE d.\"Id_Despacho\"=@p0 AND d.\"Id_OrdenTrabajo\"=@p1",dto.IdOperacion,o.Id)).SingleOrDefault();
        var previa=anterior is null?null:new FotoArchivo(anterior.Id,"Despacho",c.Codigo,anterior.Nombre,anterior.ContentType,anterior.Bytes,anterior.Ruta,anterior.Sha256,anterior.Fecha,anterior.Usuario,anterior.Bucket);
        var f=await fotos.Guardar(id,dto.Foto.Id,"Despacho",c.Codigo,dto.Foto.Nombre,dto.Foto.ContentType,dto.Foto.Base64,Correo(user),previa);
        var metadata=Write(new[]{new{id=f.Id,bucket=f.Bucket,ruta=f.Ruta,nombre=f.Nombre,mime=f.ContentType,bytes=f.Bytes,sha256=f.Sha256}});
        await u.Query<Guid>("SELECT \"Id_Despacho\" AS \"Value\" FROM afrisan.retirar_cilindro(@p0,@p1,@p2,@p3,@p4::jsonb)",dto.IdOperacion,o.Id,c.Id,dto.PesoBrutoGr,metadata);
        var result=await Ver(u,await Acceder(u,id,user));await u.Commit();return result;
    }
    public async Task<OperacionResponse> Guardar(Guid id,GuardarOperacionRequest dto,ClaimsPrincipal user)
    {
        Rol(user,"Tecnico");await using var u=await database.Open(PgDatabase.Actor(user));var o=await Acceder(u,id,user);Editable(o);
        if(dto.Instancia!=settings.Instancia||dto.Id==Guid.Empty||dto.Tipo is not("Agregado" or "Reemplazo")) throw new ReglaInventarioException("El borrador no corresponde a esta instalación o no tiene un identificador válido.",409);
        if(dto.Tipo=="Agregado"&&(dto.CodigoRetirado is not null||dto.PesoRetiradoGr is not null)) throw new ReglaInventarioException("Un agregado no lleva cilindro retirado.");
        var clean=dto with{Version=0,CodigoInstalado=PgInventario.Null(dto.CodigoInstalado)?.ToUpperInvariant(),CodigoRetirado=PgInventario.Null(dto.CodigoRetirado)?.ToUpperInvariant()};
        if(clean.CodigoInstalado is not null&&!await u.One<bool>("SELECT EXISTS(SELECT 1 FROM afrisan.\"Despacho\" d JOIN afrisan.\"Cilindros\" c USING(\"Id_Cilindro\") WHERE d.\"Id_OrdenTrabajo\"=@p0 AND c.\"CilindroQR\"=@p1) AS \"Value\"",o.Id,clean.CodigoInstalado)) throw new ReglaInventarioException("El instalado no fue retirado para esta OT.");
        if(clean.CodigoRetirado is not null&&!await u.One<bool>("SELECT EXISTS(SELECT 1 FROM afrisan.\"RetiroPrevisto\" d JOIN afrisan.\"Cilindros\" c USING(\"Id_Cilindro\") WHERE d.\"Id_OrdenTrabajo\"=@p0 AND c.\"CilindroQR\"=@p1) AS \"Value\"",o.Id,clean.CodigoRetirado)) throw new ReglaInventarioException("El retirado no está en el pedido.");
        var op=(await u.Query<OperacionRow>(OpSql+" WHERE \"Id_Operacion\"=@p0",dto.Id)).SingleOrDefault();
        if(op is null) {
            if(dto.Version!=0) throw new ReglaInventarioException("La versión de alta debe ser cero.",409);
            await u.Exec("INSERT INTO afrisan.\"OperacionTerreno\"(\"Id_Operacion\",\"Id_OrdenTrabajo\",\"Tipo\",\"Id_Tecnico\",\"DatosBorrador\") VALUES(@p0,@p1,@p2,@p3,@p4::jsonb)",dto.Id,o.Id,dto.Tipo,PgDatabase.Actor(user),Write(clean));
        } else {
            if(op.OtId!=o.Id) throw new ReglaInventarioException("Ese UUID pertenece a otra OT.",409);
            if(op.Estado=="Descartada") throw new ReglaInventarioException("Borrador descartado.",409);
            if(Read<GuardarOperacionRequest>(op.Datos)==clean) return await VerOperacion(u,op);
            if(op.Estado!="Borrador") throw new ReglaInventarioException("El trabajo confirmado no se sobrescribe.",409);
            Version(op,dto.Version);var previous=Read<GuardarOperacionRequest>(op.Datos);var files=Read<FotoArchivo[]>(op.Fotos);
            if(files.Any(f=>f.Grupo=="Retirado")&&clean.CodigoRetirado!=previous.CodigoRetirado||files.Any(f=>f.Grupo=="Instalado")&&clean.CodigoInstalado!=previous.CodigoInstalado) throw new ReglaInventarioException("Quita las fotos del borrador antes de cambiar el cilindro.",409);
            await u.Exec("UPDATE afrisan.\"OperacionTerreno\" SET \"DatosBorrador\"=@p1::jsonb,\"Tipo\"=@p2,\"Version\"=\"Version\"+1 WHERE \"Id_Operacion\"=@p0",dto.Id,Write(clean),dto.Tipo);
        }
        var result=await VerOperacion(u,await Operacion(u,o.Id,dto.Id));await u.Commit();return result;
    }
    public async Task<OperacionResponse> Foto(Guid id,Guid operacion,SubirFotoRequest dto,ClaimsPrincipal user)
    {
        Rol(user,"Tecnico");await using var u=await database.Open(PgDatabase.Actor(user));var o=await Acceder(u,id,user);
        var op=await Operacion(u,o.Id,operacion);var datos=Read<GuardarOperacionRequest>(op.Datos);var files=Read<FotoArchivo[]>(op.Fotos);
        if(dto.Grupo is not("Instalado" or "Retirado")) throw new ReglaInventarioException("Grupo de foto inválido.");
        var codigo=dto.Grupo=="Instalado"?datos.CodigoInstalado:datos.CodigoRetirado;
        if(codigo is null) throw new ReglaInventarioException("Selecciona el cilindro antes de adjuntar la foto.");
        var existing=files.FirstOrDefault(f=>f.Id==dto.Id);
        if(existing is null) {Editable(o);Version(op,dto.Version);if(op.Estado!="Borrador") throw new ReglaInventarioException("Solo se agregan fotos a borradores.",409);}
        if(existing is null&&files.Count(f=>f.Grupo==dto.Grupo)>=8) throw new ReglaInventarioException("Máximo ocho fotos por pesaje.");
        var f=await fotos.Guardar(id,dto.Id,dto.Grupo,codigo,dto.Nombre,dto.ContentType,dto.Base64,Correo(user),existing);
        if(existing is not null) {
            if(existing.Grupo!=f.Grupo||existing.Codigo!=f.Codigo||existing.Sha256!=f.Sha256||existing.Nombre!=f.Nombre||existing.ContentType!=f.ContentType) throw new ReglaInventarioException("UUID de foto usado con otro contenido.",409);
            return await VerOperacion(u,op);
        }
        if(files.Any(x=>x.Sha256==f.Sha256)) throw new ReglaInventarioException("Esa fotografía ya está adjunta. Cada pesaje debe tener su propia evidencia.",409);
        await u.Exec("UPDATE afrisan.\"OperacionTerreno\" SET \"FotosBorrador\"=@p1::jsonb,\"Version\"=\"Version\"+1 WHERE \"Id_Operacion\"=@p0",operacion,Write(files.Append(f).ToArray()));
        var result=await VerOperacion(u,await Operacion(u,o.Id,operacion));await u.Commit();return result;
    }
    public async Task<OperacionResponse> QuitarFoto(Guid id,Guid operacion,Guid foto,int version,ClaimsPrincipal user)
    {
        Rol(user,"Tecnico");await using var u=await database.Open(PgDatabase.Actor(user));var o=await Acceder(u,id,user);Editable(o);
        var op=await Operacion(u,o.Id,operacion);Version(op,version);
        if(op.Estado!="Borrador") throw new ReglaInventarioException("Las evidencias confirmadas no se eliminan.",409);
        var files=Read<FotoArchivo[]>(op.Fotos);
        if(!files.Any(f=>f.Id==foto)) return await VerOperacion(u,op);
        await u.Exec("UPDATE afrisan.\"OperacionTerreno\" SET \"FotosBorrador\"=@p1::jsonb,\"Version\"=\"Version\"+1 WHERE \"Id_Operacion\"=@p0",operacion,Write(files.Where(f=>f.Id!=foto).ToArray()));
        var result=await VerOperacion(u,await Operacion(u,o.Id,operacion));await u.Commit();return result;
    }
    public async Task<OperacionResponse> Confirmar(Guid id,Guid operacion,int version,ClaimsPrincipal user)
    {
        Rol(user,"Tecnico");var actor=PgDatabase.Actor(user);await using var u=await database.Open(actor);var o=await Acceder(u,id,user);
        var op=await Operacion(u,o.Id,operacion);if(op.Estado=="Registrada") return await VerOperacion(u,op);
        Editable(o);Version(op,version);if(op.Estado!="Borrador") throw new ReglaInventarioException("La operación está descartada.",409);
        var datos=Read<GuardarOperacionRequest>(op.Datos);var files=Read<FotoArchivo[]>(op.Fotos);
        if(datos.CodigoInstalado is null||datos.PesoInstaladoGr is null||!files.Any(f=>f.Grupo=="Instalado")) throw new ReglaInventarioException("Faltan el cilindro instalado, su peso y sus fotos.");
        if(datos.Tipo=="Reemplazo"&&(datos.CodigoRetirado is null||datos.PesoRetiradoGr is null||!files.Any(f=>f.Grupo=="Retirado"))) throw new ReglaInventarioException("Faltan el cilindro retirado, su peso y sus fotos.");
        foreach(var f in files) await fotos.Leer(f.Ruta,f.ContentType,f.Sha256,f.Bucket);
        var instalado=await Cilindro(u,datos.CodigoInstalado);
        var despacho=(await u.Query<Guid>("SELECT \"Id_Despacho\" AS \"Value\" FROM afrisan.\"Despacho\" WHERE \"Id_OrdenTrabajo\"=@p0 AND \"Id_Cilindro\"=@p1 AND \"Estado\"='EnProcesoDespacho'",o.Id,instalado.Id)).SingleOrDefault();
        if(despacho==Guid.Empty) throw new ReglaInventarioException("El cilindro no está en custodia de esta OT.",409);
        var pi=await PgInventario.InsertarPesaje(u,instalado.Id,o.Id,"Instalado",datos.PesoInstaladoGr.Value,actor,datos.Observacion);
        Guid? pr=null;long? retirado=null;
        if(datos.Tipo=="Reemplazo") {var c=await Cilindro(u,datos.CodigoRetirado);retirado=c.Id;pr=await PgInventario.InsertarPesaje(u,c.Id,o.Id,"Retirado",datos.PesoRetiradoGr!.Value,actor,datos.Observacion);}
        foreach(var f in files) await Evidencia(u,f,f.Grupo=="Instalado"?pi:pr!.Value,actor);
        await u.Exec("""
            UPDATE afrisan."OperacionTerreno" SET "Id_Instalado"=@p1,"Id_Retirado"=@p2,"Id_Despacho"=@p3,
              "Id_PesajeInstalado"=@p4,"Id_PesajeRetirado"=@p5,"Observacion"=@p6,"Estado"='Registrada',"Version"="Version"+1
            WHERE "Id_Operacion"=@p0
            """,operacion,instalado.Id,retirado,despacho,pi,pr,datos.Observacion);
        var result=await VerOperacion(u,await Operacion(u,o.Id,operacion));await u.Commit();return result;
    }
    public async Task<OtResponse> InformarEntrega(Guid id,Guid operacion,ClaimsPrincipal user)
    {
        Rol(user,"Tecnico");await using var u=await database.Open(PgDatabase.Actor(user));var o=await Acceder(u,id,user);
        var op=await Operacion(u,o.Id,operacion);
        if(op.Entrega is not null) return await Ver(u,o);
        await u.Exec("UPDATE afrisan.\"OperacionTerreno\" SET \"FechaEntregaInformada\"=clock_timestamp(),\"Id_EntregaInformadaPor\"=@p1,\"Version\"=\"Version\"+1 WHERE \"Id_Operacion\"=@p0",operacion,PgDatabase.Actor(user));
        var result=await Ver(u,o);await u.Commit();return result;
    }
    public async Task<OtResponse> Recibir(Guid id,RecibirCilindroRequest dto,ClaimsPrincipal user)
    {
        Rol(user,"Bodega");var actor=PgDatabase.Actor(user);await using var u=await database.Open(actor);var o=await Acceder(u,id,user);var c=await Cilindro(u,dto.Codigo);
        var previous=(await Recepciones(u,o.Id)).FirstOrDefault(r=>r.CilindroId==c.Id);
        if(previous is not null) {
            if(previous.Peso!=dto.PesoBrutoGr||previous.Observacion!=PgInventario.Null(dto.Observacion)) throw new ReglaInventarioException("El cilindro ya fue recibido con otros datos.",409);
            return await Ver(u,o);
        }
        var operation=(await u.Query<Guid>("SELECT \"Id_Operacion\" AS \"Value\" FROM afrisan.\"OperacionTerreno\" WHERE \"Id_OrdenTrabajo\"=@p0 AND \"Id_Retirado\"=@p1 AND \"Estado\"='Registrada'",o.Id,c.Id)).SingleOrDefault();
        var dispatch=operation==Guid.Empty?(await u.Query<Guid>("SELECT \"Id_Despacho\" AS \"Value\" FROM afrisan.\"Despacho\" WHERE \"Id_OrdenTrabajo\"=@p0 AND \"Id_Cilindro\"=@p1 AND \"Estado\"='EnProcesoDespacho'",o.Id,c.Id)).SingleOrDefault():Guid.Empty;
        if(operation==Guid.Empty&&dispatch==Guid.Empty) throw new ReglaInventarioException("No existe un retiro o despacho pendiente de esta OT.",409);
        var peso=await PgInventario.InsertarPesaje(u,c.Id,o.Id,"Recepcion",dto.PesoBrutoGr,actor,dto.Observacion);
        await u.Exec("""
            INSERT INTO afrisan."RecepcionBodega"("Id_OrdenTrabajo","Id_Cilindro","Id_Operacion","Id_DespachoSinUso","Id_Pesaje","Id_Bodeguero","Observacion")
            VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6)
            """,o.Id,c.Id,operation==Guid.Empty?null:operation,dispatch==Guid.Empty?null:dispatch,peso,actor,PgInventario.Null(dto.Observacion));
        var result=await Ver(u,o);await u.Commit();return result;
    }
    public async Task<OtResponse> Descartar(Guid id,Guid operacion,int version,ClaimsPrincipal user)
    {
        Rol(user,"Tecnico");await using var u=await database.Open(PgDatabase.Actor(user));var o=await Acceder(u,id,user);Editable(o);var op=await Operacion(u,o.Id,operacion);
        if(op.Estado=="Descartada") return await Ver(u,o);
        Version(op,version);if(op.Estado!="Borrador") throw new ReglaInventarioException("Solo se descartan borradores.",409);
        await u.Exec("UPDATE afrisan.\"OperacionTerreno\" SET \"Estado\"='Descartada',\"Version\"=\"Version\"+1 WHERE \"Id_Operacion\"=@p0",operacion);
        var result=await Ver(u,o);await u.Commit();return result;
    }
    public async Task<OtResponse> Incidencia(Guid id,CrearIncidenciaRequest dto,ClaimsPrincipal user)
    {
        Rol(user,"Tecnico","Bodega");var actor=PgDatabase.Actor(user);await using var u=await database.Open(actor);var o=await Acceder(u,id,user);
        if(dto.Id==Guid.Empty) throw new ReglaInventarioException("Falta el UUID de incidencia.");
        var exists=(await Incidencias(u,o.Id)).FirstOrDefault(i=>i.Id==dto.Id);
        if(exists is not null) {
            if(exists.OperacionId!=dto.OperacionId||exists.Tipo!=dto.Tipo||exists.Motivo!=dto.Motivo.Trim()||exists.Usuario!=Correo(user)) throw new ReglaInventarioException("UUID de incidencia usado con otros datos.",409);
            return await Ver(u,o);
        }
        await u.Exec("INSERT INTO afrisan.\"Incidencia\"(\"Id_Incidencia\",\"Id_OrdenTrabajo\",\"Id_Operacion\",\"Tipo\",\"Motivo\",\"Id_Usuario\") VALUES(@p0,@p1,@p2,@p3,@p4,@p5)",dto.Id,o.Id,dto.OperacionId,dto.Tipo,dto.Motivo.Trim(),actor);
        var result=await Ver(u,o);await u.Commit();return result;
    }
    public async Task<OtResponse> Resolver(Guid id,Guid incidencia,ResolverIncidenciaRequest dto,ClaimsPrincipal user)
    {
        Rol(user,"Autorizador");await using var u=await database.Open(PgDatabase.Actor(user));var o=await Acceder(u,id,user);
        var previous=(await Incidencias(u,o.Id)).FirstOrDefault(i=>i.Id==incidencia)??throw new ReglaInventarioException("Incidencia no encontrada.",404);
        if(previous.Respuesta is not null) {if(previous.Respuesta!=dto.Respuesta.Trim()) throw new ReglaInventarioException("La incidencia ya fue resuelta.",409);return await Ver(u,o);}
        await u.Exec("UPDATE afrisan.\"Incidencia\" SET \"Respuesta\"=@p1 WHERE \"Id_Incidencia\"=@p0",incidencia,dto.Respuesta.Trim());
        var result=await Ver(u,o);await u.Commit();return result;
    }
    public async Task<OtResponse> Transicion(Guid id,string accion,string? observacion,bool confirma,ClaimsPrincipal user)
    {
        Rol(user,accion=="termino"?"Tecnico":accion=="bodega"?"Bodega":"Autorizador");
        var actor=PgDatabase.Actor(user);await using var u=await database.Open(actor);var o=await Acceder(u,id,user);
        if(accion=="termino") {
            if(o.Estado is "PendienteBodega" or "PendienteRevision" or "Cerrada") {if(PgInventario.Null(observacion)!=o.ObservacionTermino) throw new ReglaInventarioException("El informe técnico ya fue enviado con otros datos.",409);return await Ver(u,o);}
            await u.Exec("UPDATE afrisan.\"OrdenTrabajo\" SET \"Estado\"='PendienteBodega',\"ObservacionTermino\"=@p1 WHERE \"Id_OrdenTrabajo\"=@p0",o.Id,PgInventario.Null(observacion));
        } else if(accion=="cerrar") {
            if(o.Estado=="Cerrada") return await Ver(u,o);
            await u.Exec("UPDATE afrisan.\"OrdenTrabajo\" SET \"Estado\"='Cerrada' WHERE \"Id_OrdenTrabajo\"=@p0",o.Id);
        } else {
            if(!confirma) throw new ReglaInventarioException("Confirma expresamente la revisión.");
            var etapa=accion=="bodega"?"Bodega":"Autorizador";
            if(!await u.One<bool>("SELECT EXISTS(SELECT 1 FROM afrisan.\"RevisionOT\" WHERE \"Id_OrdenTrabajo\"=@p0 AND \"Etapa\"=@p1) AS \"Value\"",o.Id,etapa))
                await u.Exec("INSERT INTO afrisan.\"RevisionOT\"(\"Id_OrdenTrabajo\",\"Etapa\",\"Id_Usuario\",\"ConfirmoRevision\",\"Observacion\") VALUES(@p0,@p1,@p2,true,@p3)",o.Id,etapa,actor,PgInventario.Null(observacion));
        }
        var result=await Ver(u,await Acceder(u,id,user));await u.Commit();return result;
    }
    public async Task<(byte[] Bytes,string Mime)> LeerFoto(Guid id,Guid? operacion,Guid foto,ClaimsPrincipal user)
    {
        await using var u=await database.Open(PgDatabase.Actor(user));var o=await Acceder(u,id,user,false);
        if(user.IsInRole("Autorizador")&&o.Estado is not("PendienteRevision" or "Cerrada")) throw new ReglaInventarioException("Bodega debe aprobar antes de revisar fotografías.",409);
        if(operacion is { } opId) {
            var op=await Operacion(u,o.Id,opId);var f=Read<FotoArchivo[]>(op.Fotos).FirstOrDefault(f=>f.Id==foto)??throw new ReglaInventarioException("Foto no encontrada.",404);
            return await fotos.Leer(f.Ruta,f.ContentType,f.Sha256,f.Bucket);
        }
        var evidence=(await u.Query<EvidenciaRow>(EvidenciasSql+" JOIN afrisan.\"Despacho\" d ON d.\"Id_Pesaje\"=e.\"Id_Pesaje\" WHERE d.\"Id_Despacho\"=@p0 AND d.\"Id_OrdenTrabajo\"=@p1",foto,o.Id)).SingleOrDefault()
            ??throw new ReglaInventarioException("Foto de despacho no encontrada.",404);
        return await fotos.Leer(evidence.Ruta,evidence.ContentType,evidence.Sha256,evidence.Bucket);
    }
    private static string Correo(ClaimsPrincipal user)=>user.FindFirstValue("correo")??throw new ReglaInventarioException("Sesión inválida.",401);
    private static Task<int> Evento(PgUnit u,long ot,int actor,string accion,string detalle)=>u.Exec("INSERT INTO afrisan.\"EventoOT\"(\"Id_OrdenTrabajo\",\"Accion\",\"Id_Usuario\",\"Detalle\") VALUES(@p0,@p1,@p2,@p3)",ot,accion,actor,detalle);
    private const string EvidenciasSql="""
        SELECT e."Id_Evidencia" AS "Id",e."Id_Pesaje" AS "PesajeId",e."BucketPrivado" AS "Bucket",e."RutaObjeto" AS "Ruta",
          e."NombreArchivo" AS "Nombre",e."ContentType",e."Bytes",e."SHA256" AS "Sha256",u."Correo_Usuario" AS "Usuario",e."Fecha"
        FROM afrisan."EvidenciaPesaje" e JOIN afrisan."Usuario" u USING("Id_Usuario")
        """;
    private static Task<int> Evidencia(PgUnit u,FotoArchivo f,Guid pesaje,int actor)=>u.Exec("""
        INSERT INTO afrisan."EvidenciaPesaje"("Id_Evidencia","Id_Pesaje","BucketPrivado","RutaObjeto","NombreArchivo","ContentType","Bytes","SHA256","Id_Usuario")
        VALUES(@p0,@p1,@p8,@p2,@p3,@p4,@p5,@p6,@p7)
        """,f.Id,pesaje,f.Ruta,f.Nombre,f.ContentType,f.Bytes,f.Sha256,actor,f.Bucket);
    private static Task<RecepcionRow[]> Recepciones(PgUnit u,long ot)=>u.Query<RecepcionRow>("""
        SELECT r."Id_Recepcion" AS "Id",r."Id_Operacion" AS "OperacionId",r."Id_DespachoSinUso" AS "DespachoId",
          r."Id_Cilindro" AS "CilindroId",p."PesoBrutoGr" AS "Peso",u."Correo_Usuario" AS "Usuario",r."Fecha",r."Observacion"
        FROM afrisan."RecepcionBodega" r JOIN afrisan."Pesaje" p USING("Id_Pesaje") JOIN afrisan."Usuario" u ON u."Id_Usuario"=r."Id_Bodeguero"
        WHERE r."Id_OrdenTrabajo"=@p0
        """,ot);
    private static Task<IncidenciaResponse[]> Incidencias(PgUnit u,long ot)=>u.Query<IncidenciaResponse>("""
        SELECT i."Id_Incidencia" AS "Id",i."Id_Operacion" AS "OperacionId",i."Tipo",i."Motivo",u."Correo_Usuario" AS "Usuario",
          i."Fecha",i."Respuesta",r."Correo_Usuario" AS "Revisor",i."FechaRevision"
        FROM afrisan."Incidencia" i JOIN afrisan."Usuario" u ON u."Id_Usuario"=i."Id_Usuario"
        LEFT JOIN afrisan."Usuario" r ON r."Id_Usuario"=i."Id_Revisor" WHERE i."Id_OrdenTrabajo"=@p0 ORDER BY i."Fecha"
        """,ot);
    private static async Task<OperacionResponse> VerOperacion(PgUnit u,OperacionRow op,RecepcionRow[]? recepciones=null)
    {
        var datos=Read<GuardarOperacionRequest>(op.Datos);var files=Read<FotoArchivo[]>(op.Fotos);
        var recepcion=(recepciones??await Recepciones(u,op.OtId)).FirstOrDefault(r=>r.OperacionId==op.Id);
        return new(op.Id,op.Version,op.Tipo,datos.CodigoRetirado,datos.CodigoInstalado,datos.PesoRetiradoGr,datos.PesoInstaladoGr,
            datos.Observacion,op.Estado,files.Select(f=>f.Response()).ToArray(),op.Entrega??op.Confirmacion??op.Fecha,
            op.Entrega is not null,recepcion is not null,recepcion?.Usuario,recepcion?.Fecha,recepcion?.Peso,recepcion?.Observacion);
    }
    private static async Task<OtResponse> Ver(PgUnit u,OtRow o)
    {
        var detalle=await u.Query<LineaOtResponse>("SELECT d.\"Id_Refrigerante\" AS \"RefrigeranteId\",r.\"CodigoRefrigerante\" AS \"Refrigerante\",d.\"GrSolicitados\",d.\"GrAutorizados\" FROM afrisan.\"DetalleOT\" d JOIN afrisan.\"Refrigerantes\" r USING(\"Id_Refrigerante\") WHERE d.\"Id_OrdenTrabajo\"=@p0",o.Id);
        var retirar=await u.Query<string>("SELECT c.\"CilindroQR\" AS \"Value\" FROM afrisan.\"RetiroPrevisto\" r JOIN afrisan.\"Cilindros\" c USING(\"Id_Cilindro\") WHERE r.\"Id_OrdenTrabajo\"=@p0",o.Id);
        var despachos=await u.Query<DespachoRow>("""
            SELECT d."Id_Despacho" AS "Id",c."Id_Cilindro" AS "CilindroId",c."CilindroQR" AS "Codigo",r."CodigoRefrigerante" AS "Refrigerante",
              c."TaraGr",c."Capacidadgr" AS "CapacidadGr",p."PesoBrutoGr" AS "Peso",u."Correo_Usuario" AS "Usuario",d."Fecha",d."Estado",d."Id_Pesaje" AS "PesajeId"
            FROM afrisan."Despacho" d JOIN afrisan."Cilindros" c USING("Id_Cilindro") JOIN afrisan."Refrigerantes" r USING("Id_Refrigerante")
            JOIN afrisan."Pesaje" p ON p."Id_Pesaje"=d."Id_Pesaje" JOIN afrisan."Usuario" u ON u."Id_Usuario"=d."Id_Tecnico" WHERE d."Id_OrdenTrabajo"=@p0
            """,o.Id);
        var recepciones=await Recepciones(u,o.Id);
        var evidencias=await u.Query<EvidenciaRow>(EvidenciasSql+" WHERE e.\"Id_Pesaje\"=ANY(@p0)",despachos.Select(d=>d.PesajeId).ToArray());
        var entregas=despachos.Select(d=>{
            var r=recepciones.FirstOrDefault(r=>r.DespachoId==d.Id);var e=evidencias.FirstOrDefault(e=>e.PesajeId==d.PesajeId);
            return new EntregaResponse(d.Codigo,d.Refrigerante,d.TaraGr,d.CapacidadGr,d.Peso,d.Usuario,d.Fecha,d.Estado,r?.Peso,r?.Usuario,r?.Fecha,d.Id,
                e is null?null:new(e.Id,"Despacho",d.Codigo,e.Nombre,e.ContentType,e.Bytes,e.Fecha,e.Usuario));}).ToArray();
        var operaciones=new List<OperacionResponse>();foreach(var op in await u.Query<OperacionRow>(OpSql+" WHERE \"Id_OrdenTrabajo\"=@p0 ORDER BY \"FechaRecepcion\"",o.Id)) operaciones.Add(await VerOperacion(u,op,recepciones));
        var historial=await u.Query<EventoResponse>("SELECT e.\"Fecha\",e.\"Accion\",u.\"Correo_Usuario\" AS \"Usuario\",coalesce(e.\"Detalle\",'') AS \"Descripcion\" FROM afrisan.\"EventoOT\" e JOIN afrisan.\"Usuario\" u USING(\"Id_Usuario\") WHERE e.\"Id_OrdenTrabajo\"=@p0 ORDER BY e.\"Id_Evento\"",o.Id);
        var revisiones=await u.Query<RevisionRow>("SELECT r.\"Etapa\",u.\"Correo_Usuario\" AS \"Usuario\",r.\"Fecha\",r.\"Observacion\" FROM afrisan.\"RevisionOT\" r JOIN afrisan.\"Usuario\" u USING(\"Id_Usuario\") WHERE r.\"Id_OrdenTrabajo\"=@p0",o.Id);
        RevisionTrabajoResponse? Revision(string etapa){var r=revisiones.FirstOrDefault(r=>r.Etapa==etapa);return r is null?null:new(r.Usuario,r.Fecha,r.Observacion);}
        return new(o.Publico,o.Codigo,o.Sucursal,o.Direccion,o.Descripcion,o.Tecnico,o.Estado,o.Autorizador,o.Fecha,
            detalle,retirar,entregas,operaciones.ToArray(),await Incidencias(u,o.Id),historial,o.ObservacionTermino,Revision("Bodega"),Revision("Autorizador"));
    }
}
public sealed record RevisionRow(string Etapa,string Usuario,DateTimeOffset Fecha,string? Observacion);
