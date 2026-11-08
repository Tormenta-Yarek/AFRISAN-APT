using Afrisan.Nuevo.Contratos;
namespace Afrisan.Nuevo.Api.Persistencia;

public sealed class PgInventario(PgDatabase database)
{
    internal const string CilindrosSql = """
        SELECT c."Id_Cilindro" AS "Id",c."CilindroQR" AS "Codigo",c."NumeroSerie",c."Id_Refrigerante" AS "RefrigeranteId",
          r."CodigoRefrigerante" AS "RefrigeranteCodigo",r."NombreRefrigerante" AS "RefrigeranteNombre",
          c."Capacidadgr" AS "CapacidadGr",c."TaraGr",c."Estado",c."FechaRegistro",c."Ubicacion",c."Id_Sucursal" AS "SucursalId",
          s."NombreSucursal" AS "Sucursal",u."Correo_Usuario" AS "Tecnico",c."Id_UltimoPesaje" AS "UltimoPesajeId"
        FROM afrisan."Cilindros" c JOIN afrisan."Refrigerantes" r USING("Id_Refrigerante")
        LEFT JOIN afrisan."Sucursales" s USING("Id_Sucursal") LEFT JOIN afrisan."Usuario" u ON u."Id_Usuario"=c."Id_Tecnico"
        """;
    internal const string PesajesSql = """
        SELECT p."Id_Pesaje" AS "Id",p."Id_Cilindro" AS "CilindroId",p."Tipo",p."PesoBrutoGr" AS "Peso",
          p."Fecha",u."Correo_Usuario" AS "Usuario",p."Observacion"
        FROM afrisan."Pesaje" p JOIN afrisan."Usuario" u USING("Id_Usuario")
        """;
    internal static async Task<CilindroResponse[]> Map(PgUnit unit, CilindroRow[] rows)
    {
        if(rows.Length==0) return [];
        var pesajes=await unit.Query<PesajeRow>(PesajesSql+" WHERE p.\"Id_Cilindro\"=ANY(@p0)",rows.Select(c=>c.Id).ToArray());
        return rows.Select(c=>{
            PesajeResponse? Peso(PesajeRow? p)=>p is null?null:new(p.Peso,p.Fecha,p.Usuario,p.Observacion);
            return new CilindroResponse(c.Id,c.Codigo,c.NumeroSerie,c.RefrigeranteId,c.RefrigeranteCodigo,c.RefrigeranteNombre,
                c.CapacidadGr,c.TaraGr,c.Estado,c.FechaRegistro,Peso(pesajes.FirstOrDefault(p=>p.CilindroId==c.Id&&p.Tipo=="Inicial")),
                c.Ubicacion,c.Sucursal,c.Tecnico,Peso(pesajes.FirstOrDefault(p=>p.Id==c.UltimoPesajeId)),c.SucursalId);
        }).ToArray();
    }
    public async Task<RefrigeranteResponse[]> Refrigerantes()
    {
        await using var u=await database.Open();
        return await u.Query<RefrigeranteResponse>("SELECT \"Id_Refrigerante\" AS \"Id\",\"CodigoRefrigerante\" AS \"Codigo\",\"NombreRefrigerante\" AS \"Nombre\" FROM afrisan.\"Refrigerantes\" WHERE \"Activo\" ORDER BY \"CodigoRefrigerante\"");
    }
    public async Task<RefrigeranteResponse> CrearRefrigerante(CrearRefrigeranteRequest dto,int actor)
    {
        if(string.IsNullOrWhiteSpace(dto.Codigo)||string.IsNullOrWhiteSpace(dto.Nombre)) throw new ReglaInventarioException("Código y nombre son obligatorios.");
        await using var u=await database.Open(actor);
        var row=await u.One<RefrigeranteResponse>("""
            INSERT INTO afrisan."Refrigerantes"("CodigoRefrigerante","NombreRefrigerante") VALUES(@p0,@p1)
            RETURNING "Id_Refrigerante" AS "Id","CodigoRefrigerante" AS "Codigo","NombreRefrigerante" AS "Nombre"
            """,dto.Codigo.Trim().ToUpperInvariant(),dto.Nombre.Trim());
        await u.Commit(); return row;
    }
    public async Task<CilindroResponse[]> Cilindros()
    {
        await using var u=await database.Open(); return await Map(u,await u.Query<CilindroRow>(CilindrosSql+" ORDER BY c.\"Id_Cilindro\" DESC"));
    }
    public async Task<CilindroResponse> Cilindro(string codigo)
    {
        await using var u=await database.Open();
        return (await Map(u,await u.Query<CilindroRow>(CilindrosSql+" WHERE upper(c.\"CilindroQR\")=@p0",codigo.Trim().ToUpperInvariant()))).SingleOrDefault()
            ?? throw new ReglaInventarioException("No se encontró el cilindro.",404);
    }
    public async Task<CilindroResponse> CrearCilindro(CrearCilindroRequest dto,int actor)
    {
        if(dto.UbicacionInicial is not("Bodega" or "Sucursal")) throw new ReglaInventarioException("Ubicación inicial inválida.");
        if(dto.UbicacionInicial=="Sucursal"&&dto.SucursalId is null) throw new ReglaInventarioException("Selecciona una sucursal registrada.");
        await using var u=await database.Open(actor);
        var codigo="AFR-"+Guid.NewGuid().ToString("N").ToUpperInvariant();
        var id=await u.One<long>("""
            INSERT INTO afrisan."Cilindros"("CilindroQR","NumeroSerie","Id_Refrigerante","Capacidadgr","TaraGr","Ubicacion","Id_Sucursal","Id_Registrador")
            VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7) RETURNING "Id_Cilindro" AS "Value"
            """,codigo,Null(dto.NumeroSerie),dto.RefrigeranteId,dto.CapacidadGr,dto.TaraGr,dto.UbicacionInicial,dto.UbicacionInicial=="Sucursal"?dto.SucursalId:null,actor);
        if(dto.PesoInicialGr is { } peso) await InsertarPesaje(u,id,null,"Inicial",peso,actor,Null(dto.Observacion));
        var result=(await Map(u,await u.Query<CilindroRow>(CilindrosSql+" WHERE c.\"Id_Cilindro\"=@p0",id))).Single();
        await u.Commit(); return result;
    }
    public async Task<CilindroResponse> Pesar(string codigo,RegistrarPesajeInicialRequest dto,int actor)
    {
        await using var u=await database.Open(actor);
        var c=(await u.Query<CilindroRow>(CilindrosSql+" WHERE upper(c.\"CilindroQR\")=@p0 FOR UPDATE OF c",codigo.Trim().ToUpperInvariant())).SingleOrDefault()
            ?? throw new ReglaInventarioException("No se encontró el cilindro.",404);
        if(c.Ubicacion!="Bodega"||c.UltimoPesajeId is not null) throw new ReglaInventarioException("El pesaje inicial no reemplaza pesajes anteriores ni se registra fuera de Bodega.",409);
        await InsertarPesaje(u,c.Id,null,"Inicial",dto.PesoBrutoGr,actor,Null(dto.Observacion));
        var result=(await Map(u,await u.Query<CilindroRow>(CilindrosSql+" WHERE c.\"Id_Cilindro\"=@p0",c.Id))).Single();
        await u.Commit();return result;
    }
    public async Task<EventoResponse[]> Eventos(string codigo)
    {
        await using var u=await database.Open();
        var c=(await u.Query<CilindroRow>(CilindrosSql+" WHERE upper(c.\"CilindroQR\")=@p0",codigo.Trim().ToUpperInvariant())).SingleOrDefault()
            ?? throw new ReglaInventarioException("No se encontró el cilindro.",404);
        return await u.Query<EventoResponse>("""
            SELECT h."Fecha",'Ubicación / estado'::text AS "Accion",u."Correo_Usuario" AS "Usuario",
              h."Ubicacion"||' / '||h."Estado" AS "Descripcion"
            FROM afrisan."HistorialCilindro" h JOIN afrisan."Usuario" u USING("Id_Usuario")
            WHERE h."Id_Cilindro"=@p0 ORDER BY h."Id_Historial"
            """,c.Id);
    }
    internal static Task<Guid> InsertarPesaje(PgUnit u,long cilindro,long? ot,string tipo,long peso,int actor,string? observacion=null)=>u.One<Guid>("""
        INSERT INTO afrisan."Pesaje"("Id_Cilindro","Id_OrdenTrabajo","Tipo","PesoBrutoGr","Id_Usuario","Observacion")
        VALUES(@p0,@p1,@p2,@p3,@p4,@p5) RETURNING "Id_Pesaje" AS "Value"
        """,cilindro,ot,tipo,peso,actor,observacion);
    internal static string? Null(string? text)=>string.IsNullOrWhiteSpace(text)?null:text.Trim();
}
