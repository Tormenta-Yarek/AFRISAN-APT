using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Afrisan.Nuevo.Api.PowerBi;

public static class PowerBiPaquete
{
    public static byte[] Crear(BiDatos d)
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output,ZipArchiveMode.Create,true))
        {
            void Texto(string nombre,string contenido)
            {
                using var stream = zip.CreateEntry(nombre).Open();
                using var writer = new StreamWriter(stream,new UTF8Encoding(true));
                writer.Write(contenido);
            }
            void Csv<T>(string nombre,T[] filas)
            {
                var props = typeof(T).GetProperties(BindingFlags.Public|BindingFlags.Instance);
                using var stream = zip.CreateEntry(nombre+".csv").Open();
                using var writer = new StreamWriter(stream,new UTF8Encoding(true));
                writer.WriteLine(string.Join(',',props.Select(p=>Celda(p.Name))));
                foreach(var fila in filas)
                    writer.WriteLine(string.Join(',',props.Select(p=>Celda(p.GetValue(fila)))));
            }
            Csv("Inventario",d.Inventario); Csv("Ordenes",d.Ordenes); Csv("DetalleOT",d.Detalle);
            Csv("Operaciones",d.Operaciones); Csv("Despachos",d.Despachos); Csv("Incidencias",d.Incidencias);
            Texto("Manifiesto.json",JsonSerializer.Serialize(new {
                version=1,generadoUtc=d.GeneradoUtc,zonaFechas="America/Santiago",
                alcance="Corte completo de datos operativos. Los filtros de la vista previa no limitan este paquete.",
                filas=new {Inventario=d.Inventario.Length,Ordenes=d.Ordenes.Length,DetalleOT=d.Detalle.Length,
                    Operaciones=d.Operaciones.Length,Despachos=d.Despachos.Length,Incidencias=d.Incidencias.Length}
            },new JsonSerializerOptions{WriteIndented=true}));
            var assembly = typeof(PowerBiPaquete).Assembly;
            foreach(var recurso in assembly.GetManifestResourceNames().Where(n=>n.StartsWith("AFRISAN.PowerBI.",StringComparison.Ordinal)))
            {
                using var reader = new StreamReader(assembly.GetManifestResourceStream(recurso)!);
                Texto(recurso["AFRISAN.PowerBI.".Length..],reader.ReadToEnd());
            }
        }
        return output.ToArray();
    }

    private static string Celda(object? valor)
    {
        var texto=valor switch {
            null=>"", DateTimeOffset fecha=>fecha.ToUniversalTime().ToString("O",CultureInfo.InvariantCulture),
            DateOnly dia=>dia.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),
            bool b=>b?"true":"false", IFormattable numero=>numero.ToString(null,CultureInfo.InvariantCulture),
            _=>valor.ToString()??""
        };
        // El CSV también puede abrirse en Excel. Evitar fórmulas en nombres/códigos libres.
        if(valor is string && (texto.TrimStart().StartsWith('=') || texto.TrimStart().StartsWith('+')
            || texto.TrimStart().StartsWith('-') || texto.TrimStart().StartsWith('@')
            || texto.StartsWith('\t') || texto.StartsWith('\r') || texto.StartsWith('\n')))
            texto="'"+texto;
        return "\""+texto.Replace("\"","\"\"")+"\"";
    }
}
