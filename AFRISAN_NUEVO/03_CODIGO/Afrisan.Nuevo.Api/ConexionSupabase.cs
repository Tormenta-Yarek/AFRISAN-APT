using Npgsql;
using System.Net.Sockets;
using System.Security.Authentication;

namespace Afrisan.Nuevo.Api;

// Verifica transporte y objetos del esquema. No sustituye los stores en memoria.
public static class ConexionSupabase
{
    private static readonly string[] Tablas =
    [
        "Rol", "Usuario", "Cliente", "Sucursales", "Refrigerantes", "Cilindros",
        "OrdenTrabajo", "DetalleOT", "RetiroPrevisto", "Pesaje", "EvidenciaPesaje",
        "Despacho", "OperacionTerreno", "CustodiaActual", "RecepcionBodega", "RevisionOT",
        "Incidencia", "HistorialCilindro", "EventoOT", "LimiteSucursal",
        "LimiteSucursalHistorial", "MedicionLimite", "MedicionLimiteOperaciones",
        "RevisionExceso", "PreciosRefrigerantes", "Costos",
        "CertificadosDisposicion", "CertificadosCilindros", "Auditoria", "BajaCilindro"
    ];
    private static readonly string[] Vistas =
    [
        "Inventario", "ResumenOT", "PesajesRecepcion", "LimitesVigentes", "ExcesosRegistrados",
        "CostosOperaciones", "DisposicionesCertificadas", "PedidoYEjecucion"
    ];

    public static async Task<int> VerificarAsync(IConfiguration configuration)
    {
        var cadena = configuration.GetConnectionString("Supabase");
        if (string.IsNullOrWhiteSpace(cadena))
        {
            Console.Error.WriteLine("Falta ConnectionStrings:Supabase. Ejecuta Configurar-Supabase.ps1 para configurarla localmente.");
            return 1;
        }

        try
        {
            var opciones = new NpgsqlConnectionStringBuilder(cadena);
            if (string.IsNullOrWhiteSpace(opciones.Host) || string.IsNullOrWhiteSpace(opciones.Username)
                || string.IsNullOrWhiteSpace(opciones.Database))
                throw new ArgumentException();

            // Se exige cifrado y validación del certificado y nombre del servidor.
            opciones.SslMode = SslMode.VerifyFull;
            opciones.Timeout = 10;
            opciones.CommandTimeout = 15;
            opciones.IncludeErrorDetail = false;
            opciones.PersistSecurityInfo = false;
            opciones.Pooling = false;
            opciones.ApplicationName = "AFRISAN-Verificar-Supabase";

            using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await using var fuente = NpgsqlDataSource.Create(opciones.ConnectionString);
            await using var conexion = await fuente.OpenConnectionAsync(limite.Token);
            await using var transaccion = await conexion.BeginTransactionAsync(limite.Token);
            await using (var soloLectura = new NpgsqlCommand("SET TRANSACTION READ ONLY", conexion, transaccion))
                await soloLectura.ExecuteNonQueryAsync(limite.Token);

            const string sql = """
                SELECT n.nspname, c.relname, c.relkind::text, c.relrowsecurity
                FROM pg_catalog.pg_class c
                JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
                WHERE n.nspname = 'afrisan'
                  AND c.relkind IN ('r', 'p', 'v')
                """;
            var tablas = new HashSet<string>(StringComparer.Ordinal);
            var vistas = new HashSet<string>(StringComparer.Ordinal);
            var sinRls = new List<string>();
            await using (var comando = new NpgsqlCommand(sql, conexion, transaccion))
            await using (var lector = await comando.ExecuteReaderAsync(limite.Token))
            {
                while (await lector.ReadAsync(limite.Token))
                {
                    var esquema = lector.GetString(0);
                    var nombre = lector.GetString(1);
                    var tipo = lector.GetString(2);
                    if (esquema == "afrisan" && tipo is "r" or "p")
                    {
                        tablas.Add(nombre);
                        if (!lector.GetBoolean(3)) sinRls.Add(nombre);
                    }
                    if (esquema == "afrisan" && tipo == "v") vistas.Add(nombre);
                }
            }
            await transaccion.RollbackAsync(limite.Token);
            Console.WriteLine("Conexión PostgreSQL correcta, con TLS y certificado verificado.");
            var faltantes = Tablas.Except(tablas).Select(t => $"afrisan.{t}")
                .Concat(Vistas.Except(vistas).Select(v => $"afrisan.{v}")).ToArray();
            if (faltantes.Length > 0)
                Console.Error.WriteLine($"Objetos esperados ausentes: {string.Join(", ", faltantes)}.");
            if (sinRls.Count > 0)
                Console.Error.WriteLine($"Tablas sin RLS habilitado: {string.Join(", ", sinRls)}.");
            if (faltantes.Length > 0 || sinRls.Count > 0) return 2;

            Console.WriteLine($"Presentes las {Tablas.Length} tablas y {Vistas.Length} vistas esperadas en afrisan; RLS habilitado en las tablas.");
            Console.WriteLine("Diagnóstico de solo lectura. La aplicación sigue en memoria; faltan repositorios, autenticación persistente y Storage.");
            return 0;
        }
        catch (PostgresException ex)
        {
            Console.Error.WriteLine(ex.SqlState == "28P01"
                ? "PostgreSQL rechazó la autenticación. Revisa usuario y contraseña de la base de datos."
                : $"PostgreSQL rechazó la comprobación (SQLSTATE {ex.SqlState}). No se modificaron datos.");
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException or OperationCanceledException)
        {
            var causas = new List<Exception>();
            for (Exception? causa = ex; causa is not null; causa = causa.InnerException) causas.Add(causa);
            if (causas.OfType<AuthenticationException>().Any())
                Console.Error.WriteLine("Falló la validación TLS del servidor. Revisa el nombre del host y el certificado raíz de Supabase; se mantiene SSL Mode=VerifyFull.");
            else if (causas.OfType<SocketException>().FirstOrDefault() is { } red)
                Console.Error.WriteLine($"No se pudo alcanzar el servidor PostgreSQL (red: {red.SocketErrorCode}). Revisa host, puerto y acceso de red; para IPv4 usa Session pooler.");
            else if (causas.Any(c => c is TimeoutException or OperationCanceledException))
                Console.Error.WriteLine("Se agotó el tiempo de conexión o consulta a PostgreSQL. Revisa servidor, puerto, restricciones de red y estado del proyecto Supabase.");
            else
                Console.Error.WriteLine("La conexión PostgreSQL se interrumpió antes de completar el diagnóstico. Revisa el endpoint y el estado del proyecto Supabase.");
            // Solo tipos de excepción: nunca mensajes libres ni cadenas de conexión.
            Console.Error.WriteLine($"Tipos de error: {string.Join(" -> ", causas.Select(c => c.GetType().Name))}.");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.IO.IOException)
        {
            Console.Error.WriteLine("Configuración inválida. Revisa ConnectionStrings:Supabase y, si se indicó, la ruta del certificado raíz.");
        }
        // No mostrar excepciones completas: pueden incluir datos de la conexión.
        return 1;
    }
}
