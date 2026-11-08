# Persistencia de AFRISAN

Actualización: **02/11/2026**. Implementación preparada y probada; actualización
07 aplicada en Supabase tras autorización explícita de Dante. La conexión de
ejecución restringida está verificada. El usuario creó su administrador por
terminal: se verificó un usuario y un administrador activo en Supabase.
La API y la web están iniciadas en los puertos 5181 y 5286; inventario sin sesión
responde 401. Ya se puede entrar con la cuenta real creada por el usuario.

## Qué implementa

- PostgreSQL mediante EF Core 8 y Npgsql, usando consultas parametrizadas y tipos
  explícitos sobre el esquema existente. No ejecuta EnsureCreated ni migraciones
  automáticas al iniciar.
- Transacciones por comando con actor verificado en `afrisan.id_usuario`.
- Usuarios reales, hashes de contraseña, sesiones JWT con firma estable,
  comprobación de usuario/rol/versión en cada solicitud y bloqueo persistido.
- Creación inicial de administrador solo desde terminal y solo cuando no existen
  usuarios. Los usuarios de trabajo se crean desde Administración.
- Clientes/sucursales por ID, refrigerantes, cilindros y pesaje inicial atómico.
- OT, reservas, retiro con peso/foto, borradores/versiones, evidencias,
  confirmación, recepción, incidencias, revisiones y cierre.
- Reintentos con UUID y comprobación del contenido. Sin contadores de códigos
  mantenidos en memoria. El ámbito offline permanece entre reinicios de la API
  y cambia al reinstalar el esquema.
- Formularios de Administración y selección de sucursal/técnico persistidos.

Las emisiones quedan fuera del alcance. Límites, Defontana y Power BI conservan
sus pendientes funcionales; este incremento no implementa esos módulos.

## Cambio de base preparado para revisión

[07_Habilitar_Persistencia.sql](../02_BASE_DATOS/07_Habilitar_Persistencia.sql)
agrega una columna a OrdenTrabajo y dos columnas JSONB a OperacionTerreno. Los
datos/fotos de borrador se guardan allí; Pesaje y EvidenciaPesaje solo reciben
hechos confirmados y conservan sus triggers de inmutabilidad.

También prepara el rol `afrisan_api`, sin superusuario, creación de bases/roles,
replicación ni BYPASSRLS. Los permisos se limitan a las 20 tablas que utiliza
este incremento, sus secuencias necesarias y cinco funciones operativas. No
otorga acceso a tablas de límites, costos, certificados ni otros esquemas.
UPDATE de Pesaje se necesita para los bloqueos FOR UPDATE del SQL; su trigger
rechaza modificaciones materiales. Auditoria no tiene permiso DELETE/UPDATE.

Las políticas RLS `BackendConfiable` permiten al servidor acceder a las filas
de esas tablas. El aislamiento de roles de negocio y OT se aplica en la API y
en los triggers con el actor de sesión. El navegador no recibe credenciales
PostgreSQL. `anon`/`authenticated` no reciben permisos.

**Riesgo y autorización registrada:** la identidad del servidor puede acceder a
los datos operativos de esas tablas; sus credenciales deben permanecer privadas.
La revisión automática rechazó la primera ejecución remota por crear una
identidad con privilegios amplios y políticas de servidor sin autorización
explícita del alcance. Se redujeron los permisos y se preparó este alcance
concreto para aprobación. Dante respondió «Autorizar y activar la persistencia»;
la actualización se ejecutó y se verificó la conexión real como `afrisan_api`.
En Session pooler el nombre de conexión incluye `.proyecto`; current_user en
PostgreSQL sigue siendo `afrisan_api`. Las credenciales no se publican.

## Preparación y administrador inicial

La preparación remota y el administrador ya están completados en este equipo.
Para una instalación nueva que todavía no tenga usuarios, el alta inicial se hace
desde una terminal local:

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File "D:\Afrisan\AFRISAN_NUEVO\03_CODIGO\Preparar-Persistencia.ps1" -SoloAdministrador
```

Sin `-SoloAdministrador`, el comando usa la conexión de diagnóstico para aplicar 07 y habilitar
el login `afrisan_api` con contraseña aleatoria. Guarda la conexión de ejecución,
una clave JWT estable y la ruta privada de fotos en User Secrets. Conserva la
conexión de propietario solo para diagnóstico/preparación, no para las peticiones
normales. Repetir la preparación conserva el password de ejecución si ya está
configurado; no rota la firma JWT.

Después solicita nombre, apellido, correo y contraseña del administrador. La
contraseña solo se introduce en la entrada oculta de la terminal; no se pasa
en argumentos ni se imprime. Si la preparación ya se ejecutó y solo falta el
administrador, añadir `-SoloAdministrador`.

No hay usuarios, contraseñas de ejemplo, clientes ni sucursales precargados.
El administrador crea las cuentas y catálogos reales desde la aplicación.
No repetir el archivo 06: ese archivo borra la base completa.

Para comprobar la preparación sin iniciar HTTP:

```powershell
dotnet run --project Afrisan.Nuevo.Api --launch-profile http -- --verificar-persistencia
```

## Fotografías privadas

La ampliación posterior del 02/11/2026 ya usa **Supabase Storage privado**,
configurado y verificado. Las fotos nuevas no se guardan en el disco local.
El disco anterior se conserva como respaldo de los originales de la migración;
la copia a nube se verifica antes de activar el proveedor. Las referencias
históricas se resuelven en nube sin reescribir los pesajes inmutables.

La API autoriza la lectura por OT/rol y verifica firma de formato, tamaño, MIME
y SHA-256. Se mantienen UUID/ruta/hash durante reintentos y se rechaza cualquier
bucket público. Las claves solo están en User Secrets del backend, nunca en
Blazor. [Configuración, pruebas y detalles](STORAGE.md).

Los objetos sin referencia tras una transacción fallida no se sirven; su
limpieza administrativa sigue pendiente. Los respaldos deben abarcar PostgreSQL
y Storage. Validación física de cámara y pruebas completas de red siguen pendientes.

## Inicio y modo de revisión

El modo predeterminado es PostgreSQL y se detiene con un mensaje si falta la
preparación; no vuelve silenciosamente a memoria ante fallos de red.
La conexión debe usar `afrisan_api` y TLS VerifyFull. La excepción sin TLS existe
solo para pruebas locales explícitas en Development contra localhost.

El prototipo anterior se conserva para revisión seleccionando explícitamente
`Persistencia__Modo=Memoria` en Development. Sus datos siguen siendo temporales;
no se migran sus cuentas o registros demo a la base real.

## Validación

[Pruebas de API y reinicios](../04_SEGUIMIENTO/PERSISTENCIA_2026_11_02/README.md).
La solución completa compila con cero errores y advertencias. No se han escrito
datos ficticios en el Supabase real durante estas pruebas.
