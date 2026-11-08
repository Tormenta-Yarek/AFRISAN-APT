// Power BI Desktop: crear parámetro de texto CarpetaAfrisan y consulta en blanco Afrisan.
// Pegar este contenido en su Editor avanzado. La carpeta contiene el ZIP descomprimido.
let
    Carpeta = Text.TrimEnd(CarpetaAfrisan, {"\", "/"}) & "\",
    Leer = (Nombre as text, Tipos as list) as table =>
        let
            Cabecera = Table.PromoteHeaders(
                Csv.Document(Binary.Buffer(File.Contents(Carpeta & Nombre & ".csv")),
                    [Delimiter=",", Encoding=65001, QuoteStyle=QuoteStyle.Csv]),
                [PromoteAllScalars=true]),
            Nulos = Table.ReplaceValue(Cabecera,"",null,Replacer.ReplaceValue,Table.ColumnNames(Cabecera))
        in Table.TransformColumnTypes(Nulos, Tipos, "en-US"),
    Inventario = Leer("Inventario", {
        {"CilindroId",type text},{"Codigo",type text},{"RefrigeranteId",type text},
        {"Refrigerante",type text},{"Estado",type text},{"Ubicacion",type text},
        {"SucursalId",type text},{"Sucursal",type text},{"TecnicoId",type text},{"Tecnico",type text},
        {"KgSegunUltimoPesaje",type number},{"FechaPesaje",type datetimezone}}),
    Ordenes = Leer("Ordenes", {
        {"OtId",type text},{"Codigo",type text},{"SucursalId",type text},{"Sucursal",type text},
        {"TecnicoId",type text},{"Tecnico",type text},{"Estado",type text},
        {"FechaCreacion",type datetimezone},{"DiaCreacion",type date},{"FechaCierre",type datetimezone}}),
    DetalleOT = Leer("DetalleOT", {
        {"OtId",type text},{"RefrigeranteId",type text},{"Refrigerante",type text},
        {"KgSolicitados",type number},{"KgAutorizados",type number}}),
    Operaciones = Leer("Operaciones", {
        {"OperacionId",type text},{"OtId",type text},{"Tipo",type text},
        {"RefrigeranteId",type text},{"Refrigerante",type text},
        {"CilindroInstalado",type text},{"CilindroRetirado",type text},
        {"FechaConfirmacion",type datetimezone},{"DiaConfirmacion",type date},
        {"KgInstalados",type number},{"KgRetirados",type number},
        {"FechaRecepcion",type datetimezone},{"KgRecepcion",type number},{"PendienteRecepcion",type logical}}),
    Despachos = Leer("Despachos", {
        {"DespachoId",type text},{"OtId",type text},{"Cilindro",type text},
        {"RefrigeranteId",type text},{"Refrigerante",type text},
        {"Fecha",type datetimezone},{"Dia",type date},{"KgDespachados",type number},
        {"Estado",type text},{"FechaDevolucion",type datetimezone},{"KgDevueltos",type number}}),
    Incidencias = Leer("Incidencias", {
        {"IncidenciaId",type text},{"OtId",type text},{"Tipo",type text},
        {"Fecha",type datetimezone},{"Dia",type date},{"Abierta",type logical},{"FechaRevision",type datetimezone}})
in
    [Inventario=Inventario, Ordenes=Ordenes, DetalleOT=DetalleOT,
     Operaciones=Operaciones, Despachos=Despachos, Incidencias=Incidencias]
