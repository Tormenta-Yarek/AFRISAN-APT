# Conexión de diagnóstico a Supabase

La aplicación ya tiene un proveedor persistente implementado y su conexión
restringida `afrisan_api` está preparada y verificada. Consulta la
[guía de persistencia](PERSISTENCIA.md). Este archivo describe la conexión de
diagnóstico del propietario, que se conserva separada de la conexión de ejecución.
Las menciones históricas al modo en memoria se conservan como antecedente.

**Actualización 02/11/2026:** el diagnóstico espera ahora **30 tablas y ocho
vistas en el único esquema `afrisan`**, según el
[nuevo instalador sin emisiones](../02_BASE_DATOS/README.md). El
[diagnóstico autenticado ya pasó](../04_SEGUIMIENTO/SUPABASE_2026_11_02.md), con CA
oficial configurada. No volver a instalar el SQL 04 ni repetir el reemplazo 06.

Proyecto: `sugsgvrqxtzeaddkvzii`. El usuario confirmó el 29 de octubre de 2026 que
ejecutó el SQL adjunto, cuyo contenido coincide con el instalador
`04_Crear_Afrisan_Contexto_Actual.sql`. Ese antecedente fue sustituido por el esquema
único del 02/11/2026, cuyos objetos esperados ya se verificaron mediante diagnóstico.

## Configurar y comprobar

En Supabase, abrir **Connect → Direct · Connection string**, buscar **Method** y
seleccionar **Session pooler**. Copiar los valores exactos: el host depende del
clúster, y el usuario del pooler lleva el identificador del proyecto. El host
directo suministrado (`db.sugsgvrqxtzeaddkvzii.supabase.co`) solo resolvió a IPv6
y no fue accesible en la comprobación inicial desde este equipo.

Desde PowerShell, en `D:\Afrisan\AFRISAN_NUEVO\03_CODIGO`:

```powershell
.\Configurar-Supabase.ps1
```

Si Windows PowerShell informa que la ejecución de scripts está deshabilitada,
usar este comando, que aplica RemoteSigned solo al proceso que ejecuta el script:

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File "D:\Afrisan\AFRISAN_NUEVO\03_CODIGO\Configurar-Supabase.ps1"
```

No cambia la política permanente del usuario o del equipo. La contraseña se
introduce únicamente en la entrada oculta de esa terminal.

El script solicita la contraseña mediante entrada oculta y guarda la cadena en
**User Secrets**, fuera del repositorio y sin enviarla como argumento de proceso.
Este almacén de desarrollo no cifra el archivo del perfil local. No compartir
su contenido ni ejecutar `dotnet user-secrets list` para enviar resultados al chat.
No se necesitan claves `anon` o `service_role` para este diagnóstico PostgreSQL.

Valores predeterminados confirmados por el usuario: servidor
`aws-0-us-east-1.pooler.supabase.com`, puerto `5432`, base `postgres` y usuario
`postgres.sugsgvrqxtzeaddkvzii`. Se pueden cambiar mediante los parámetros
`-Servidor`, `-Puerto`, `-BaseDatos` y `-Usuario`.

Se exige TLS con validación de certificado y nombre del servidor (`VerifyFull`).
Si el certificado no está respaldado por una CA confiable en el equipo, descargar
el certificado raíz desde Supabase y añadir `-CertificadoRaiz 'RUTA_DEL_CERTIFICADO'`.

Si aparece `RemoteCertificateChainErrors` / `UntrustedRoot`, descargar la CA desde
**Database → Settings → SSL Configuration → Download Certificate** del proyecto.
Guardar el archivo público, por ejemplo, en
`D:\Afrisan\.local\certificates\supabase.crt`. No usar como CA un certificado
obtenido únicamente de una conexión TLS que no ha sido verificada.

Para añadirlo a la configuración ya guardada sin volver a introducir la contraseña:

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File "D:\Afrisan\AFRISAN_NUEVO\03_CODIGO\Configurar-Supabase.ps1" -UsarConfiguracionGuardada -CertificadoRaiz "D:\Afrisan\.local\certificates\supabase.crt"
```

El script conserva host, puerto, base, usuario y contraseña guardados, configura
el certificado y mantiene `VerifyFull`. No instala la CA en el almacén global
de Windows. El archivo PS1 usa UTF-8 con BOM para los acentos en Windows PowerShell.
El diagnóstico distingue errores TLS, DNS/red y tiempos de espera, sin imprimir
la cadena de conexión ni mensajes libres de excepciones.

Para repetir la comprobación sin introducir nuevamente la contraseña:

```powershell
dotnet run --project .\Afrisan.Nuevo.Api --launch-profile http -- --verificar-supabase
```

También puede usarse la variable de entorno `ConnectionStrings__Supabase` con
una cadena Npgsql, evitando guardarla en archivos versionados. Esta variable tiene
prioridad sobre User Secrets. Las credenciales pertenecen únicamente a la API.

## Alcance del resultado

La comprobación abre una transacción **READ ONLY**, consulta el catálogo de
PostgreSQL y revierte la transacción. No instala SQL ni crea usuarios, roles,
permisos, datos o buckets. Termina sin iniciar el servidor HTTP.

- Salida 0: conexión verificada, presentes las 30 tablas y ocho vistas esperadas en `afrisan`,
  con RLS habilitado en las tablas.
- Salida 1: configuración, autenticación, certificado o red pendientes.
- Salida 2: conexión correcta, objetos esperados ausentes o tablas sin RLS.

Comprueba nombres de objetos y RLS; no certifica cada columna, trigger, política
ni permiso. Una conexión como propietario `postgres` tampoco acredita el acceso
de una futura identidad de aplicación con permisos restringidos.

**La API normal sigue usando `InventarioStore` y `TrabajoStore` en memoria.**
Este paso prepara y comprueba la conexión; quedan pendientes los repositorios
transaccionales, usuarios reales, Storage privado y las pruebas de conservación
de datos después de reiniciar. No se habilitan escrituras con las cuentas demo.

Referencias: [conexiones de Supabase](https://supabase.com/docs/guides/database/connecting-to-postgres),
[TLS en Npgsql](https://www.npgsql.org/doc/security.html) y
[User Secrets de .NET](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0).
