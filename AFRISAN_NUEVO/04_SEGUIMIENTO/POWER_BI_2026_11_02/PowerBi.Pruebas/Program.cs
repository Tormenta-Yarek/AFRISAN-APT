using System.IO.Compression;
using System.Text.Json;
using Afrisan.Nuevo.Api;
using Afrisan.Nuevo.Api.PowerBi;
using Afrisan.Nuevo.Contratos;
using Microsoft.Extensions.Configuration;

var checks=new List<string>();
void Check(bool ok,string texto){if(!ok)throw new Exception(texto);checks.Add(texto);Console.WriteLine("OK: "+texto);}
var corte=DateTimeOffset.Parse("2026-11-02T01:30:00Z"); // 01/11 a las 22:30 en Chile.
var creado=DateTimeOffset.Parse("2026-10-01T12:00:00Z");
var d=new BiDatos(corte,
    [new(1,"=HYPERLINK(\"no\")",1,"GAS","PendientePesaje","Bodega",null,null,null,null,null,null),
     new(2,"AFR-2",1,"GAS","Instalado","Sucursal",1,"@Sucursal, \"Sur\"\nDos",2,"Técnico",4.6m,corte)],
    [new(1,"OT-1",1,"@Sucursal, \"Sur\"\nDos",2,"Técnico","EnTerreno",creado,new(2026,10,1),null)],
    [new(1,1,"GAS",10m,9m),new(1,2,"GAS-2",3m,2m)],
    [new(Guid.NewGuid(),1,"Reemplazo",1,"GAS","AFR-2","AFR-1",corte,new(2026,11,1),4.6m,0.8m,null,null,true)],
    [],[]);
var actual=PgPowerBi.Resumen(d,null,null,null,null,null);
Check(actual.Hasta==new DateOnly(2026,11,1),"El día predeterminado corresponde a Chile y no al día UTC");
Check(actual.OrdenesAbiertas==0&&actual.KgInstalados==4.6m,"La operación del período se cuenta aunque la OT se creó en otro mes");
Check(actual.RetiradosPendientes==1&&actual.SinPesaje==1,"Pendientes y pesos ausentes tienen significado independiente");
var historico=PgPowerBi.Resumen(d,new(2026,10,1),new(2026,10,31),null,null,null);
Check(historico.OrdenesAbiertas==1&&historico.KgInstalados==0&&historico.Cilindros==2,"Cada indicador aplica su fecha sin convertir inventario actual en histórico");
Check(PgPowerBi.Resumen(d,null,null,null,2,null).KgInstalados==0,"Filtro por gas no hereda kilos de otro detalle de la misma OT");
Check(PgPowerBi.Resumen(d,null,null,1,null,null).Cilindros==1,"Filtro de sucursal en inventario usa la ubicación actual");
try{PgPowerBi.Resumen(d,new(2026,11,2),new(2026,11,1),null,null,null);throw new Exception("Rango inválido aceptado");}
catch(ReglaInventarioException ex){Check(ex.Estado==400,"Se rechaza rango invertido");}
using var buffer=new MemoryStream(PowerBiPaquete.Crear(d));
using var zip=new ZipArchive(buffer,ZipArchiveMode.Read);
string Leer(string nombre){using var reader=new StreamReader(zip.GetEntry(nombre)!.Open());return reader.ReadToEnd();}
Check(Leer("Inventario.csv").Contains("\"'=HYPERLINK(\"\"no\"\")\""),"Nombres que empiezan como fórmula se neutralizan en CSV");
Check(Leer("Ordenes.csv").Contains("\"'@Sucursal, \"\"Sur\"\"\nDos\""),"CSV conserva comas, comillas, Unicode y saltos de línea");
Check(Leer("Inventario.csv").Contains("\"\",\"\""),"Ausencia de medición se exporta vacía y no como cero");
Check(JsonDocument.Parse(Leer("Manifiesto.json")).RootElement.GetProperty("filas").GetProperty("DetalleOT").GetInt32()==2,"Manifiesto conserva la cantidad de filas sin aplanar hechos");
var options=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["PowerBi:ReportId"]="https://app.powerbi.com/view?r=publico"}).Build();
try{PowerBiOptions.Load(options);throw new Exception("URL pública aceptada");}
catch(InvalidOperationException){Check(true,"Configuración impide URL pública como ReportId");}
Check(!new PowerBiOptions(null,null).Publica().Configurado,"Sin ReportId no se presenta un informe ficticio");
var result=new {estado="CORRECTO",cantidad=checks.Count,comprobaciones=checks};
if(args.Length==1)File.WriteAllText(args[0],JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"Resultado: CORRECTO. {checks.Count} comprobaciones de fechas, fórmulas y CSV.");
