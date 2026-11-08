using Afrisan.Nuevo.Api.Persistencia;
using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Api.PowerBi;

public sealed record BiDatos(DateTimeOffset GeneradoUtc, BiInventario[] Inventario, BiOrden[] Ordenes,
    BiDetalle[] Detalle, BiOperacion[] Operaciones, BiDespacho[] Despachos, BiIncidencia[] Incidencias);

public sealed class PgPowerBi(PgDatabase database)
{
    // Un corte consistente; no se consulta Storage ni se amplían permisos de PostgreSQL.
    public async Task<BiDatos> Datos(int actor)
    {
        await using var u = await database.Open();
        await u.Exec("SET TRANSACTION ISOLATION LEVEL REPEATABLE READ, READ ONLY");
        await u.Query<string>("SELECT set_config('afrisan.id_usuario', @p0, true) AS \"Value\"",actor.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var generado = await u.One<DateTimeOffset>("SELECT transaction_timestamp() AS \"Value\"");
        async Task<T[]> Read<T>(string sql)
        {
            var rows = await u.Query<T>(sql + " LIMIT 50001");
            if (rows.Length > 50000)
                throw new ReglaInventarioException("El paquete supera 50.000 filas por tabla. Se necesita preparar una conexión BI de mayor volumen; no se exportaron datos parciales.", 413);
            return rows;
        }
        var inventario = await Read<BiInventario>("""
            SELECT c."Id_Cilindro" AS "CilindroId",c."CilindroQR" AS "Codigo",
              c."Id_Refrigerante" AS "RefrigeranteId",r."CodigoRefrigerante" AS "Refrigerante",
              c."Estado",c."Ubicacion",c."Id_Sucursal" AS "SucursalId",s."NombreSucursal" AS "Sucursal",
              c."Id_Tecnico" AS "TecnicoId",t."Nombre"||' '||t."Apellido" AS "Tecnico",
              (p."PesoBrutoGr"-c."TaraGr")/1000.0 AS "KgSegunUltimoPesaje",p."Fecha" AS "FechaPesaje"
            FROM afrisan."Cilindros" c JOIN afrisan."Refrigerantes" r USING("Id_Refrigerante")
            LEFT JOIN afrisan."Pesaje" p ON p."Id_Pesaje"=c."Id_UltimoPesaje"
            LEFT JOIN afrisan."Sucursales" s USING("Id_Sucursal")
            LEFT JOIN afrisan."Usuario" t ON t."Id_Usuario"=c."Id_Tecnico"
            ORDER BY c."Id_Cilindro"
            """);
        var ordenes = await Read<BiOrden>("""
            SELECT o."Id_OrdenTrabajo" AS "OtId",o."CodigoOT" AS "Codigo",o."Id_Sucursal" AS "SucursalId",
              s."NombreSucursal" AS "Sucursal",o."Id_Tecnico" AS "TecnicoId",
              t."Nombre"||' '||t."Apellido" AS "Tecnico",o."Estado",o."FechaCreacion",
              (o."FechaCreacion" AT TIME ZONE 'America/Santiago')::date AS "DiaCreacion",o."FechaCierre"
            FROM afrisan."OrdenTrabajo" o JOIN afrisan."Sucursales" s USING("Id_Sucursal")
            JOIN afrisan."Usuario" t ON t."Id_Usuario"=o."Id_Tecnico" ORDER BY o."Id_OrdenTrabajo"
            """);
        var detalle = await Read<BiDetalle>("""
            SELECT d."Id_OrdenTrabajo" AS "OtId",d."Id_Refrigerante" AS "RefrigeranteId",
              r."CodigoRefrigerante" AS "Refrigerante",d."GrSolicitados"/1000.0 AS "KgSolicitados",
              d."GrAutorizados"/1000.0 AS "KgAutorizados"
            FROM afrisan."DetalleOT" d JOIN afrisan."Refrigerantes" r USING("Id_Refrigerante")
            ORDER BY d."Id_OrdenTrabajo",d."Id_Refrigerante"
            """);
        var operaciones = await Read<BiOperacion>("""
            SELECT op."Id_Operacion" AS "OperacionId",op."Id_OrdenTrabajo" AS "OtId",op."Tipo",
              ci."Id_Refrigerante" AS "RefrigeranteId",g."CodigoRefrigerante" AS "Refrigerante",
              ci."CilindroQR" AS "CilindroInstalado",cr."CilindroQR" AS "CilindroRetirado",
              op."FechaConfirmacion",(op."FechaConfirmacion" AT TIME ZONE 'America/Santiago')::date AS "DiaConfirmacion",
              (pi."PesoBrutoGr"-ci."TaraGr")/1000.0 AS "KgInstalados",
              (pr."PesoBrutoGr"-cr."TaraGr")/1000.0 AS "KgRetirados",rb."Fecha" AS "FechaRecepcion",
              (pb."PesoBrutoGr"-cr."TaraGr")/1000.0 AS "KgRecepcion",
              (op."Tipo"='Reemplazo' AND rb."Id_Recepcion" IS NULL) AS "PendienteRecepcion"
            FROM afrisan."OperacionTerreno" op
            JOIN afrisan."Cilindros" ci ON ci."Id_Cilindro"=op."Id_Instalado"
            JOIN afrisan."Refrigerantes" g ON g."Id_Refrigerante"=ci."Id_Refrigerante"
            JOIN afrisan."Pesaje" pi ON pi."Id_Pesaje"=op."Id_PesajeInstalado"
            LEFT JOIN afrisan."Cilindros" cr ON cr."Id_Cilindro"=op."Id_Retirado"
            LEFT JOIN afrisan."Pesaje" pr ON pr."Id_Pesaje"=op."Id_PesajeRetirado"
            LEFT JOIN afrisan."RecepcionBodega" rb ON rb."Id_Operacion"=op."Id_Operacion"
            LEFT JOIN afrisan."Pesaje" pb ON pb."Id_Pesaje"=rb."Id_Pesaje"
            WHERE op."Estado"='Registrada' ORDER BY op."Id_Operacion"
            """);
        var despachos = await Read<BiDespacho>("""
            SELECT d."Id_Despacho" AS "DespachoId",d."Id_OrdenTrabajo" AS "OtId",c."CilindroQR" AS "Cilindro",
              c."Id_Refrigerante" AS "RefrigeranteId",g."CodigoRefrigerante" AS "Refrigerante",d."Fecha",
              (d."Fecha" AT TIME ZONE 'America/Santiago')::date AS "Dia",
              (p."PesoBrutoGr"-c."TaraGr")/1000.0 AS "KgDespachados",d."Estado",
              rb."Fecha" AS "FechaDevolucion",(pb."PesoBrutoGr"-c."TaraGr")/1000.0 AS "KgDevueltos"
            FROM afrisan."Despacho" d JOIN afrisan."Cilindros" c USING("Id_Cilindro")
            JOIN afrisan."Refrigerantes" g USING("Id_Refrigerante")
            JOIN afrisan."Pesaje" p ON p."Id_Pesaje"=d."Id_Pesaje"
            LEFT JOIN afrisan."RecepcionBodega" rb ON rb."Id_DespachoSinUso"=d."Id_Despacho"
            LEFT JOIN afrisan."Pesaje" pb ON pb."Id_Pesaje"=rb."Id_Pesaje" ORDER BY d."Id_Despacho"
            """);
        var incidencias = await Read<BiIncidencia>("""
            SELECT "Id_Incidencia" AS "IncidenciaId","Id_OrdenTrabajo" AS "OtId","Tipo","Fecha",
              ("Fecha" AT TIME ZONE 'America/Santiago')::date AS "Dia",
              ("FechaRevision" IS NULL) AS "Abierta","FechaRevision"
            FROM afrisan."Incidencia" ORDER BY "Id_Incidencia"
            """);
        return new(generado,inventario,ordenes,detalle,operaciones,despachos,incidencias);
    }

    public static BiResumenResponse Resumen(BiDatos d, DateOnly? desde, DateOnly? hasta,
        int? sucursalId, long? refrigeranteId, int? tecnicoId)
    {
        var hoy = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(d.GeneradoUtc,
            TimeZoneInfo.FindSystemTimeZoneById("America/Santiago")).DateTime);
        var fin = hasta ?? hoy;
        var inicio = desde ?? fin.AddDays(-29);
        if (inicio > fin || sucursalId is <= 0 || refrigeranteId is <= 0 || tecnicoId is <= 0)
            throw new ReglaInventarioException("Revisa el rango de fechas y los filtros.");
        bool Dia(DateOnly dia) => dia >= inicio && dia <= fin;
        var elegibles = d.Ordenes.Where(o => (sucursalId is null || o.SucursalId == sucursalId)
            && (tecnicoId is null || o.TecnicoId == tecnicoId)).Select(o => o.OtId).ToHashSet();
        var conGas = d.Detalle.Where(x => refrigeranteId is null || x.RefrigeranteId == refrigeranteId)
            .Select(x => x.OtId).ToHashSet();
        var ordenes = d.Ordenes.Where(o => elegibles.Contains(o.OtId) && conGas.Contains(o.OtId) && Dia(o.DiaCreacion)).ToArray();
        var inv = d.Inventario.Where(x => (sucursalId is null || x.SucursalId == sucursalId)
            && (refrigeranteId is null || x.RefrigeranteId == refrigeranteId)
            && (tecnicoId is null || x.TecnicoId == tecnicoId)).ToArray();
        var ops = d.Operaciones.Where(x => elegibles.Contains(x.OtId) && Dia(x.DiaConfirmacion)
            && (refrigeranteId is null || x.RefrigeranteId == refrigeranteId)).ToArray();
        var inc = d.Incidencias.Where(x => elegibles.Contains(x.OtId) && conGas.Contains(x.OtId) && Dia(x.Dia)).ToArray();
        static BiGrupoResponse[] Contar(IEnumerable<string> source) => source.GroupBy(x => x)
            .Select(g => new BiGrupoResponse(g.Key,g.Count())).OrderByDescending(x => x.Valor).ThenBy(x => x.Nombre).ToArray();
        return new(d.GeneradoUtc,inicio,fin,inv.Length,inv.Count(x => x.KgSegunUltimoPesaje is null),
            ordenes.Count(x => x.Estado != "Cerrada"),ordenes.Count(x => x.Estado == "Cerrada"),
            ops.Sum(x => x.KgInstalados),ops.Count(x => x.PendienteRecepcion),inc.Count(x => x.Abierta),
            Contar(inv.Select(x => x.Estado)),Contar(ordenes.Select(x => x.Estado)),
            ops.GroupBy(x => x.Refrigerante).Select(g => new BiGrupoResponse(g.Key,g.Sum(x => x.KgInstalados))).OrderBy(x => x.Nombre).ToArray(),
            d.Ordenes.Select(x => new BiOpcionResponse(x.SucursalId,x.Sucursal)).Distinct().OrderBy(x => x.Nombre).ToArray(),
            d.Detalle.Select(x => new BiOpcionResponse(x.RefrigeranteId,x.Refrigerante))
                .Concat(d.Inventario.Select(x => new BiOpcionResponse(x.RefrigeranteId,x.Refrigerante))).Distinct().OrderBy(x => x.Nombre).ToArray(),
            d.Ordenes.Select(x => new BiOpcionResponse(x.TecnicoId,x.Tecnico)).Distinct().OrderBy(x => x.Nombre).ToArray());
    }
}
