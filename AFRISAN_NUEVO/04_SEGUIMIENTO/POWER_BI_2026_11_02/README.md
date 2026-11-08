# Preparación y validación del módulo Power BI

Fecha de trabajo: **02/11/2026** (America/Santiago).

Resultado: **100 comprobaciones correctas**:

- 78 sobre API real .NET 8 y PostgreSQL PGlite aislado. Incluyen el recorrido
  previo de persistencia y nuevos controles de roles, filtros, estados, pesos,
  corte de datos, ZIP, reinicio y configuración segura del informe.
- 13 sobre fechas Chile/UTC, OT creadas en otro mes, cantidades, nulos, CSV,
  comas/comillas/saltos de línea y neutralización de fórmulas.
- 9 sobre Blazor real en Brave headless con API y sesión sintéticas: carga,
  gráficos, filtros, descarga, permisos de pantalla y vista móvil de 390 px.

Compilación completa: cero errores y advertencias. `git diff --check` correcto.

Los datos de las pruebas son sintéticos; las contraseñas y perfiles de prueba
se generan localmente y no acceden a User Secrets reales. No se insertaron
usuarios ni operaciones de ejemplo en Supabase. El navegador usa un perfil
independiente e intercepta las peticiones antes de enviarlas al backend real.
Su ZIP simulado verifica el puente de descarga; el contenido del ZIP real se
valida en las pruebas HTTP y .NET.

## Evidencia

- `resultados_powerbi.json`: 78 comprobaciones de API, RLS y persistencia.
- `resultados_formulas.json`: 13 comprobaciones de semántica y exportación.
- `resultados_pantalla.json`: 9 comprobaciones del navegador.
- `paquete_muestra.json`: lista de archivos y conteos de un paquete sintético.
- `verificacion_real.json`: consultas reales de las seis fuentes completadas;
  base todavía sin registros operativos, sin informe configurado.
- `pantalla_escritorio.png`, `pantalla_movil.png`: capturas con datos sintéticos.

## Repetir

Usar .NET SDK instalado y las dependencias ya restauradas del proyecto:

```powershell
$env:DOTNET_CLI_HOME='D:\Afrisan\.local\dotnet-cli'
$env:NUGET_PACKAGES='D:\Afrisan\.local\nuget-packages'
dotnet build AFRISAN_NUEVO/04_SEGUIMIENTO/POWER_BI_2026_11_02/PowerBi.Pruebas/PowerBi.Pruebas.csproj -m:1 -p:UseSharedCompilation=false -o D:/Afrisan/.local/powerbi-tests
dotnet D:/Afrisan/.local/powerbi-tests/PowerBi.Pruebas.dll D:/Afrisan/AFRISAN_NUEVO/04_SEGUIMIENTO/POWER_BI_2026_11_02/resultados_formulas.json
$env:AFRISAN_API_DLL='D:\Afrisan\.local\powerbi-tests\Afrisan.Nuevo.Api.dll'
$env:AFRISAN_TEST_POWERBI='1'
$env:AFRISAN_TEST_STORAGE='0'
node AFRISAN_NUEVO/04_SEGUIMIENTO/PERSISTENCIA_2026_11_02/validar_api.mjs
```

La prueba de navegador necesita la web actualizada en localhost:5286 y Brave
instalado en la ruta indicada por el guion. Usa un puerto DevTools aleatorio
solo en loopback. En un entorno restringido puede necesitar permiso para
controlar el navegador y conectar WebSocket local:

```powershell
node AFRISAN_NUEVO/04_SEGUIMIENTO/POWER_BI_2026_11_02/validar_pantalla.mjs
```

El socket de pruebas PGlite admite una sola conexión y necesita liberar el
cierre TCP entre solicitudes; en esta ampliación se deja un intervalo de 30 ms.
No se reintentan escrituras ni se ocultan errores del servidor.

## Verificación de instalación real

Después de instalar la compilación se verificaron `/health/ready` (PostgreSQL
y SupabaseStorage), `/power-bi` HTTP 200 y `/power-bi/paquete` anónimo HTTP 401.
El diagnóstico `--verificar-powerbi` consulta las seis tablas del paquete en
transacción de solo lectura y muestra únicamente conteos y estado de informe.
Se ejecutó correctamente contra Supabase real, con cero registros operativos
y sin informe configurado. No se cambiaron tablas, políticas ni permisos.

No se validaron Power Query/DAX dentro de Power BI Desktop, un PBIX publicado,
la autenticación Microsoft ni actualización programada: el usuario todavía no
tiene informe publicado. Los 100 controles no sustituyen esa aceptación.
