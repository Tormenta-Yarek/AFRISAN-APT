# Revisión de concordancia entre negocio, datos y prototipo

Fecha: 20 de octubre de 2026. Proyecto capstone AFRISAN. Responsable humano: Dante Moya.

Alcance de esta revisión: lectura de los acuerdos actuales de `01_NEGOCIO`, los tres scripts de `02_BASE_DATOS`, su diccionario, los contratos y stores del prototipo de `03_CODIGO`. No se ejecutó SQL, no se accedió a Supabase, no se modificó código ni se realizaron pruebas. Solo se guarda este informe local: no se crean commits ni se publica contenido, conforme a la instrucción de Dante. Las referencias `archivo:línea` se refieren a la copia local revisada, relativa a `AFRISAN_NUEVO`.

**Conclusión:** el prototipo ya representa reemplazos físicos de cilindros, pero el script 03 todavía representa consumo de gas desde un cilindro hacia un equipo y retorno del mismo envase. No basta agregar una conexión PostgreSQL: antes deben reconciliarse las entidades, estados, restricciones, cálculos y vistas BI. El esquema local no acredita el estado de la base en la nube.

## 1. Qué está confirmado y qué sigue siendo propuesta

| Tema | Acuerdo confirmado por Dante | Evidencia local y límite del acuerdo |
| --- | --- | --- |
| Operación principal | Reemplazo físico y agregado de cilindros. | `01_NEGOCIO/07_OT_DEFINICION_DANTE.md:27`; no equivale automáticamente a recarga de equipos. |
| Refrigerante | Un único tipo por cilindro. | `01_NEGOCIO/07_OT_DEFINICION_DANTE.md:22`. No obliga a que una OT contenga un solo tipo. |
| OT | Una sucursal solicita cilindros/refrigerantes y kilos; cada OT debe tener técnico. | `01_NEGOCIO/07_OT_DEFINICION_DANTE.md:19`, `:23`. Momento de identificación de los códigos, reasignación y relación con Defontana pendientes. |
| Autorizador | Rol distinto; crea, autoriza/asigna, verifica/cierra, corrige y participa en revisión de excesos. | `01_NEGOCIO/08_RESPONSABLES_PROCESOS.md:19`, `:25`, `:34`. No se ha confirmado un doble aprobador. |
| Jerarquía | Bodega → Autorizador → Jefatura. | `01_NEGOCIO/08_RESPONSABLES_PROCESOS.md:40`. No concede automáticamente todas las tareas de roles inferiores. |
| Límites | Semanales y mensuales por sucursal; Bodega y Jefatura los definen. Un cambio de Jefatura impide editar a Bodega hasta rehabilitación expresa. | `01_NEGOCIO/07_OT_DEFINICION_DANTE.md:29`, `:77`. Autorizador no puede saltarse esa protección; falta decidir su facultad de editar otros límites. |
| Excesos | Conservar hechos y autorizaciones para revisión con Jefatura. | `01_NEGOCIO/07_OT_DEFINICION_DANTE.md:49`. No está aprobado bloquear automáticamente el trabajo ni atribuir culpabilidad. |
| Fotos | Fotografías de los pesajes del retirado y del reemplazo, admitiendo varias por operación. | `01_NEGOCIO/09_PROCESOS_TECNICO.md:15`, `:75`. Mínimo de una por grupo es interpretación documentada; ocho por grupo y 4 MiB son límites técnicos del prototipo. Fotos del agregado y manómetros pendientes de precisar. |
| Power BI | Incluir un módulo de gráficos. | `01_NEGOCIO/11_MODULO_POWER_BI.md:7`. Indicadores, fórmulas, visibilidad, publicación y licencias están por definir. |
| Base sin poblar | Instalación de tablas sin datos precargados. | `02_BASE_DATOS/03_Crear_Tablas_Sin_Datos.sql:1`. No confundir con las cuentas demo en memoria del prototipo. |

Confirmado en conversación no significa por sí solo aceptación formal del cliente ni actualización del acta/backlog académico. El propio documento de Autorizador lo distingue en `01_NEGOCIO/10_PROCESOS_AUTORIZADOR.md:5`.

## 2. Incongruencias que bloquean conectar el SQL actual

### D-01 · Dos procesos físicos incompatibles — prioridad alta

En SQL, `Movimientos.Id_Despacho` referencia conjuntamente despacho, **mismo cilindro**, OT y técnico (`02_BASE_DATOS/03_Crear_Tablas_Sin_Datos.sql:146`). Un retorno exige ese despacho (`:148`). El trigger exige custodia abierta y que el peso de retorno no supere el del despacho (`:308`). `BalanceCilindro` calcula salida menos retorno menos gas utilizado (`:408`) y el cierre exige todos los ciclos conformes (`:460`).

El negocio actual deja el cilindro nuevo instalado y devuelve otro retirado. Eso ya ocurre en `03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs:187`: nuevo a Sucursal y retirado a Técnico. Aplicar la FK y cierre antiguos obligaría a inventar salidas del cilindro viejo o devoluciones del instalado para cerrar la OT.

**Acción propuesta:** representar por separado cada transferencia real y la relación retirado/instalado de la operación. Reescribir el criterio de conciliación/cierre para envases instalados, retirados y entregas sin uso. El balance de masa de un mismo envase puede conservarse para un proceso de recarga si ese proceso también existe y se aprueba; no reutilizarlo como fórmula del reemplazo.

### D-02 · Intervención exige un equipo y consumo que el cambio físico no demuestra — prioridad alta

`Intervencion` exige `Id_Equipo`, `Id_CausaFalla`, `GrUtilizados > 0` y `CapacidadNominalGrAplicada` (`02_BASE_DATOS/03_Crear_Tablas_Sin_Datos.sql:165`, `:170`, `:172`, `:175`). El trigger calcula un semáforo según capacidad del equipo y otra carga en 30 días (`:333`, `:354`).

La operación del prototipo contiene códigos retirado/instalado y dos pesos brutos (`03_CODIGO/Afrisan.Nuevo.Contratos/TrabajoTecnico.cs:47`). No tiene ni necesita inventar un equipo, una falla o una inyección de gas para reemplazar un envase. El punto de instalación sigue pendiente (`01_NEGOCIO/09_PROCESOS_TECNICO.md:52`).

**Acción propuesta:** ajustar `Intervencion` o crear una entidad de operación con semántica explícita de reemplazo/agregado. Decidir con el cliente si recarga de equipos, fallas y semáforo anterior siguen dentro del alcance. No eliminar esos requisitos documentales por inferencia y tampoco imponerlos al reemplazo.

### D-03 · El rol central Autorizador está rechazado por la base — prioridad alta

El CHECK de `Rol.Nombre_Rol` admite solo Administrador, Jefatura, Bodega y Tecnico (`02_BASE_DATOS/03_Crear_Tablas_Sin_Datos.sql:24`). El código incluye Autorizador (`03_CODIGO/Afrisan.Nuevo.Contratos/Inventario.cs:11`) y exige ese rol para crear, revisar y cerrar (`03_CODIGO/Afrisan.Nuevo.Api/TrabajoEndpoints.cs:17`, `:43`, `:47`).

**Riesgo:** el usuario necesario para el flujo no puede representarse con ese esquema. `Id_Autorizador` como FK a Usuario no valida por sí solo el rol ni la jerarquía.

**Acción propuesta:** admitir el rol y documentar permisos por acción en API, manteniendo un rol por usuario y sin añadir tabla Permisos. La jerarquía de límites requiere reglas de estado del límite; no se resuelve ordenando IDs de roles.

### D-04 · Faltan técnico obligatorio y detalle solicitado/autorizado en OT — prioridad alta

`OrdenTrabajo` no incluye técnico ni detalle por refrigerante (`02_BASE_DATOS/03_Crear_Tablas_Sin_Datos.sql:108`). Tener técnico en movimientos/intervenciones permite que registros de una misma OT señalen técnicos diferentes y no satisface la asignación desde la creación. El prototipo sí conserva `TecnicoEmail`, detalle y dos cantidades (`03_CODIGO/Afrisan.Nuevo.Contratos/TrabajoTecnico.cs:20`, `:25`, `:29`).

**Acción propuesta:** una FK de técnico asignado en OT y un detalle con refrigerante, gramos solicitados y autorizados. El técnico del hecho debe poder conservarse históricamente si más adelante se aprueba reasignación. Mantener cantidades reales en ejecución, sin sobrescribir el pedido. Una tabla separada Solicitud no es imprescindible para el MVP mientras no se apruebe un circuito separado.

### D-05 · Estados, ubicación y pesaje inicial no tienen correspondencia — prioridad alta

La base permite cilindros Disponible, Despachado, RetornadoConDescalce, Chatarra, DisposicionFinalCertificada e Inactivo (`02_BASE_DATOS/03_Crear_Tablas_Sin_Datos.sql:81`). El código usa además PendientePesaje, Vacio, Instalado y RetiradoPendienteRecepcion (`03_CODIGO/Afrisan.Nuevo.Api/InventarioStore.cs:90`; `TrabajoStore.cs:187`, `:215`).

La base admite OT Abierta/EnCurso/Cerrada/Anulada (`02_BASE_DATOS/03_Crear_Tablas_Sin_Datos.sql:116`); el prototipo usa Asignada/PendienteRevision/Cerrada (`03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs:263`, `:324`). El prototipo distingue ubicación/custodio (`03_CODIGO/Afrisan.Nuevo.Contratos/Inventario.cs:49`); SQL no tiene un registro equivalente para un cilindro instalado en sucursal. Además, todo movimiento SQL exige OT/técnico y ser salida o retorno (`:134`, `:137`, `:138`), mientras PN-01 necesita registrar un pesaje inicial sin OT (`03_CODIGO/Afrisan.Nuevo.Api/InventarioStore.cs:63`).

**Acción propuesta:** definir catálogo de estados y transiciones comunes; separar estado físico, ubicación y avance administrativo. Incorporar pesajes iniciales sin inventar una OT. Las ubicaciones actuales deben derivar de hechos o mantenerse en la misma transacción que estos.

### D-06 · Una columna de foto no representa las evidencias acordadas — prioridad alta

SQL tiene `Movimientos.FotoPesaje` y `Intervencion.FotoBalanza` como textos individuales (`02_BASE_DATOS/03_Crear_Tablas_Sin_Datos.sql:141`, `:174`). No hay colección de evidencias por pesaje ni pareja de cilindros. El código sí conserva fotos con grupo y cilindro (`03_CODIGO/Afrisan.Nuevo.Contratos/TrabajoTecnico.cs:69`) y valida ambos grupos para reemplazo (`03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs:173`, `:182`).

**Acción propuesta:** evidencias relacionadas al pesaje/cilindro/operación, con ID estable, autor, fecha, tipo, tamaño y referencia privada al archivo. Mantener validación de evidencia mínima al confirmar; una ruta en BD no demuestra existencia o acceso al archivo. El archivo debe persistir en almacenamiento privado y su metadato en BD.

### D-07 · Límites semanales/mensuales e historial de protección faltan — prioridad alta

La regla confirmada está en `01_NEGOCIO/07_OT_DEFINICION_DANTE.md:29` y `:77`. Las 16 tablas del SQL 03 no incluyen límites, versiones aplicadas ni investigación de excesos; el prototipo lo reconoce pendiente (`03_CODIGO/TRABAJO_TECNICO.md`, sección Pendientes explícitos). El semáforo 5/15 % y tres intervenciones críticas no implementa esos límites.

**Acción propuesta:** entidad de límite por sucursal/período, vigencia e historial; evento de edición/bloqueo/rehabilitación con actor; referencia a la versión del límite aplicada a un exceso. Antes de definir fórmula: confirmar qué kilos cuentan, si se separan por gas, calendario o ventana móvil y fecha de cómputo. No asignar a Autorizador permiso de edición pendiente ni bloquear ejecución automáticamente.

### D-08 · IDs y relaciones del prototipo requieren adaptación explícita — prioridad media

OT y operación usan Guid en código (`03_CODIGO/Afrisan.Nuevo.Contratos/TrabajoTecnico.cs:29`, `:49`); el SQL utiliza PK bigint para OT/Intervencion y UUID separado para idempotencia (`02_BASE_DATOS/03_Crear_Tablas_Sin_Datos.sql:109`, `:161`). El prototipo identifica usuario por correo y sucursal por texto (`03_CODIGO/Afrisan.Nuevo.Contratos/TrabajoTecnico.cs:17`, `:20`); SQL tiene FK enteras.

**Riesgo:** conexión directa no funciona y dos nombres escritos de manera distinta fragmentan reportes/límites. El mismo nombre de sucursal podría existir para dos clientes; el texto del prototipo no los distingue.

**Acción propuesta:** decidir PK/API ID sin cambiar IDs offline ya emitidos arbitrariamente; mantener una clave de idempotencia estable. Utilizar FK de sucursal/cliente y usuario; conservar descripciones necesarias como instantáneas históricas cuando corresponda. No utilizar correo modificable como única identidad durable.

## 3. Brechas de datos del prototipo que afectan el siguiente desarrollo

### P-01 · Los gramos autorizados se registran, pero no se concilian

`TrabajoStore.Crear` conserva cantidades positivas (`03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs:40`). `Entregar` comprueba tipo de gas y disponibilidad, sin comparar total de gramos contra lo autorizado (`:74`). `Confirmar` valida pesos dentro de capacidad, pero no el acumulado autorizado (`:169`). No hay registro automático de un exceso cuantitativo por OT o período.

No se afirma que deba bloquearse la entrega: ese comportamiento no está aprobado. Sí hay una brecha para la expectativa de control de cantidades. Debe definirse qué cantidad se compara, mostrar diferencias y conservar las autorizaciones necesarias.

### P-02 · Faltan fecha del hecho y recepción de sincronización separadas

`GuardarOperacionRequest` no recibe fecha de ejecución (`03_CODIGO/Afrisan.Nuevo.Contratos/TrabajoTecnico.cs:47`). La operación toma `Actualizada` del reloj de servidor al guardar/confirmar (`03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs:97`, `:110`, `:189`). En offline, un trabajo del domingo enviado el lunes quedaría fechado por recepción si se reutiliza ese campo para BI o límites.

Propuesta: conservar fecha declarada del hecho con zona/offset, fecha de recepción y fecha de confirmación, sin confiar ciegamente en el reloj del dispositivo. Definir qué evento determina el período. El SQL anterior ya separaba FechaIntervencion/FechaCreacionLocal/FechaRecepcion (`02_BASE_DATOS/03_Crear_Tablas_Sin_Datos.sql:171`, `:190`), concepto reutilizable.

### P-03 · La recepción no tiene entidad propia con pesaje histórico estructurado

`Recibir` actualiza el último peso, marca recibido y guarda peso/observación en texto del historial (`03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs:215`). `OperacionResponse` conserva actor/fecha de recepción, pero no su pesaje ni observación estructurados (`03_CODIGO/Afrisan.Nuevo.Contratos/TrabajoTecnico.cs:72`). `InventarioStore.Ubicar` sobrescribe UltimoPesaje (`:30`) y agrega un evento de ubicación sin peso (`:32`).

Los dos pesajes técnicos sí quedan en la operación y la recepción deja texto; no se afirma que desaparezca toda evidencia. El problema es que BI y conciliación no deberían extraer gramos de frases ni depender del último peso mutable de un cilindro que se reutiliza.

Propuesta: registrar cada pesaje y recepción como hecho identificable con OT/operación, cilindro, bruto, tara aplicada, autor y fecha. Comparar el mismo envase en momentos compatibles. No restar pesos brutos de dos envases con taras distintas como si fueran pérdida de gas.

### P-04 · Crear OT cambia una ubicación inicial como atajo de revisión

`TrabajoStore.Crear` admite cilindros de Bodega para la lista de retirados y los ubica en Sucursal como Instalado (`03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs:52`, `:59`). Está declarado como soporte temporal (`03_CODIGO/TRABAJO_TECNICO.md`, pasos 2 y 3). No debe convertirse en el flujo de alta real, porque autorizar trabajo no demuestra traslado físico ni instalación previa.

Propuesta: alta de inventario inicial por ubicación con responsable y fecha, separada de solicitud/OT; creación de OT solo referencia el inventario existente. Determinar cómo registrar cilindros ya instalados al inicio del sistema.

### P-05 · Corrección es una solicitud y conclusión, todavía no un ajuste

El Autorizador agrega una respuesta a la incidencia (`03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs:240`); no cambia el dato físico ni registra un ajuste antes/después. El cierre solo exige que exista respuesta (`:277`). El documento lo reconoce (`01_NEGOCIO/10_PROCESOS_AUTORIZADOR.md:108`).

Propuesta: definir campos corregibles, revisión, ajuste compensatorio, efecto en inventario/BI y tratamiento de OT cerrada antes de programar. Resolver una incidencia con texto no significa haber corregido la medición. El SQL protege movimientos cerrados y auditabilidad, pero no ofrece procedimiento de rectificación (`02_BASE_DATOS/03_Crear_Tablas_Sin_Datos.sql:379`, `:455`).

### P-06 · El estado en memoria no es una base operativa durable

`TrabajoStore` usa diccionarios y fotos byte[] (`03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs:12`, `:336`); `InventarioStore` usa listas (`:11`). Se registran como singleton (`03_CODIGO/Afrisan.Nuevo.Api/Program.cs:15`). El reinicio pierde inventario, OT y fotografías de servidor. La memoria local del navegador conserva pendientes, pero no sustituye la base central ni las evidencias ya confirmadas.

Esto está explícitamente señalado como prototipo y no es ocultamiento de su estado. Para una entrega capstone que acredite persistencia, el recorrido debe sobrevivir a reinicios con acceso autenticado a sus evidencias.

### P-07 · Falta precisar qué ocurre con cilindros vacíos recibidos

La recepción puede dejar el cilindro como Vacio (`03_CODIGO/Afrisan.Nuevo.Api/TrabajoStore.cs:215`), pero la entrega exige Disponible (`:74`) y el pesaje inicial no puede reemplazar pesajes existentes (`03_CODIGO/Afrisan.Nuevo.Api/InventarioStore.cs:80`). No hay un proceso de reposición/recarga del envase que permita volver a despacharlo con contenido.

Esto es una **pregunta de alcance**, no un requisito nuevo impuesto: ¿AFRISAN recarga esos envases, los entrega a proveedor y recibe llenos, o solo registra disposición/reemplazo externo? La respuesta determina el movimiento real, pesaje y documentación necesarios. No habilitar una edición libre de peso para simular una reposición ni añadir un módulo de compras sin confirmación. Esta decisión puede quedar en el plan posterior a la defensa si el recorrido de evaluación usa existencias iniciales suficientes.

## 4. Power BI y cálculos

El SQL contiene vistas de ConsumoIntervenciones, Conciliacion y SucursalesCriticasMes (`02_BASE_DATOS/03_Crear_Tablas_Sin_Datos.sql:523`, `:533`, `:536`). Son consultas preparadas para el modelo anterior, no una integración Power BI operativa. La última agrupa semáforos de equipos, no excesos contra límites configurados por sucursal. El módulo confirmado sigue pendiente (`01_NEGOCIO/11_MODULO_POWER_BI.md:42`).

La tabla Costos exige una intervención con GrUtilizados y conserva tarifa aplicada (`02_BASE_DATOS/03_Crear_Tablas_Sin_Datos.sql:224`). Este enfoque de precio histórico es útil, pero la cantidad valorizada en un reemplazo sigue sin definir. El catálogo en memoria no contiene precio ni GWP (`03_CODIGO/Afrisan.Nuevo.Contratos/Inventario.cs:22`).

La fórmula SQL de CO₂e distingue GrEmitidos de GrUtilizados y exige fuente/metodología si hay factor (`02_BASE_DATOS/03_Crear_Tablas_Sin_Datos.sql:183`, `:203`). Es una precaución útil: un cambio de envase no demuestra emisión. Debe acordarse qué indicador ambiental exige la evaluación y cómo se obtiene la masa estimada; desconocido debe seguir siendo desconocido y no cero.

Propuesta mínima de modelo analítico, pendiente de diseño y sin dar por acordados todos los indicadores:

- Hechos separados para línea solicitada/autorizada, operación física, transferencia/recepción y evento de exceso. Cada conjunto declara qué representa una fila.
- Dimensiones de fecha, sucursal/cliente, refrigerante, cilindro, técnico y estado según permiso aprobado.
- Medidas de solicitado, autorizado, instalado y retirado separadas. No sumarlas como consumo total ni multiplicar cantidades al unir varias fotos con una operación.
- Historial de precio y límite aplicado; corregir un catálogo no reescribe un reporte histórico.
- Vista de inventario actual; inventario histórico a una fecha necesita hechos de ubicación o instantáneas, no solo el estado actual.
- Excluir claves, hashes, contenido de fotografías y rutas privadas. Definir lectura global o por cliente/rol antes de dar acceso al informe.

Es posible preparar estas vistas dentro de la misma base de operación, con una identidad de consulta de BI; no se justifica crear un almacén de datos independiente como requisito automático para una persona sola. El modo final de consumo y publicación requiere decisión de alcance y recursos.

## 5. Instrucciones y diccionario desactualizados

`02_BASE_DATOS/README.md:9` presenta el script 03 sin poblar como versión reciente, pero `:23` recomienda script 02 ante esquema existente. Ese script **inserta cuatro roles** (`02_BASE_DATOS/02_MER_Completo_Afrisan_Nuevo.sql:36`) y crea otros esquemas. Seguir esa alternativa ahora puede contradecir la petición de cero poblamiento y crear otra fuente de tablas con el mismo modelo antiguo. Recomendación: marcar versiones antiguas como históricas y sustituir la receta por inventario del estado real y migración explícita; esta revisión no las ejecuta ni las modifica.

`02_BASE_DATOS/TABLAS_PK_FK_TIPOS_MANUAL.md:3` corresponde al script 03, así que hereda sus diferencias de negocio. Crear las tablas manualmente tampoco instala los triggers/vistas; el diccionario lo advierte correctamente en `:593`. Un MER/SVG evidencia tablas y enlaces visibles, no demuestra que la nube tenga todas las reglas, RLS, funciones ni datos correctos. Revisar la base real con acceso autorizado antes de definir una migración.

Hay anotaciones históricas aún expresadas en presente, por ejemplo D-18 «todavía sin implementar» en `04_SEGUIMIENTO/DECISIONES_Y_PENDIENTES.md:27`, seguido de D-19 que sí describe evidencias múltiples. Conviene mantener el historial, pero añadir una columna/encabezado inequívoco de estado vigente para que no se interprete como pendientes ya resueltos en el prototipo.

## 6. Modelo mínimo recomendado para diseñar, no SQL aprobado

Los nombres son propuestas. Reutilizar tablas existentes cuando representen correctamente el hecho y mantener el alcance en una sola API y una base.

| Grupo lógico | Información mínima | Decisión pendiente relevante |
| --- | --- | --- |
| Rol / Usuario | Identidad estable, rol incluyendo Autorizador, activo; autenticación segura. | Aprovisionamiento real y permisos por acción. No tabla Permisos. |
| Cliente / Sucursal | PK/FK y nombre/dirección. | Catálogo real y propiedad/alcance de acceso. |
| Refrigerante | Código único, nombre; GWP/fuente/metodología cuando se use. | Fuente y versión del factor; catálogo aceptado. |
| Cilindro | Código QR estable, único refrigerante, tara, capacidad, serie cuando corresponda. | Alta de existencias ya instaladas y cambios legítimos de datos maestros. |
| OT / DetalleOT | Sucursal, técnico, autorizador, fechas/estado; una línea por refrigerante con solicitado y autorizado. | Estados, autorización en uno o dos pasos, rechazo/reasignación/anulación. |
| Operación física | OT, tipo reemplazo/agregado, instalado obligatorio, retirado cuando reemplazo, actor, fechas, versión e ID de idempotencia. | Igualdad de gas retirado/nuevo, trabajo parcial y punto de instalación. |
| Pesaje | Cilindro, contexto de alta/entrega/instalación/retiro/recepción, bruto, tara aplicada, autor y fechas. | Momento de medición, tolerancias y regulación de registros offline. |
| EvidenciaPesaje | Relación a pesaje, ID estable, referencia privada, tipo/tamaño, autor/fecha. | Evidencia de agregado y de bodega; máximo técnico configurable. |
| Movimiento / Recepción | Envase, origen/destino/custodio, OT/operación cuando corresponde, pesaje vinculado. | Plazos, faltantes, viajes adicionales y excepciones. Puede resolverse con un solo registro de transferencia bien definido. |
| LímiteSucursal / historial | Semana/mes, sucursal, valor/vigencia, autor, protección de Jefatura, eventos de rehabilitación. | Base de kilos, calendario, gas, retroactividad y alcance de Autorizador. |
| Incidencia / revisión / ajuste | Hecho o dato cuestionado, motivo, evidencia, participantes, revisión y ajuste si se autoriza. | Datos corregibles y tratamiento de cierre. No culpas automáticas. |
| Precio / valoración | Precio vigente y precio aplicado preservados, moneda, cantidad base y concepto. | Qué se cobra en reemplazo y qué componentes de costo exige la entrega. |
| Auditoría | Actor, fecha, operación, motivo cuando aplique, antes/después de cambios. | Retención y acceso a revisiones. |
| Vistas BI | Datos operativos sin secretos y medidas con definición. | Indicadores finales y visibilidad; publicación Power BI. |

No se propone eliminar Equipos, CausasFalla o CertificadosDisposicion sin reconciliar primero el alcance formal. Pueden quedar posteriores si el cliente y la evaluación admiten ese recorte, pero no desaparecer de una historia comprometida silenciosamente.

## 7. Próximo avance eficiente para un único desarrollador

1. Resolver en una sesión las decisiones que cambian el modelo: reemplazo y posible recarga adicional, kilos que computan, período, reglas de cierre/corrección y representación de existencias iniciales. Registrar los acuerdos nuevos y su efecto sobre historias anteriores.
2. Diseñar un MER actualizado y diccionario único a partir del recorrido físico. Mantener scripts anteriores como evidencia; no volver a crear esquemas por ensayo y error.
3. Implementar una sola trayectoria persistente: alta de refrigerante/cilindros → OT con técnico → entrega → reemplazo con dos pesajes y fotos múltiples → recepción de retirado → revisión/cierre. Transacciones, identidades e idempotencia forman parte de esta trayectoria.
4. Incorporar límites con historial/protección de Jefatura, cálculo acordado y registro de excesos. Evitar programar una fórmula provisional que después cambie toda la reportería.
5. Construir el primer informe Power BI desde los mismos registros persistidos, priorizando inventario/OT y solicitado/autorizado/real. Agregar costos y emisiones cuando exista definición y datos suficientes.
6. Acreditar después con evidencias verificables: reinicio conservando datos, acceso por rol, varios envases, múltiples fotos, semana/mes, exceso, corrección y sincronización duplicada. Esta revisión no ejecutó esos casos.

Actualización recibida durante la revisión: Dante debe presentar el **jueves 22 de octubre de 2026 a las 7** y dispone de **3 horas por día**. La planificación inmediata debe ajustarse a aproximadamente **6 horas entre el martes 20 y miércoles 21**, según el plan consolidado de esta revisión. No se asume AM/PM ni tiempo adicional el día de la presentación.

La secuencia anterior es la ruta de desarrollo posterior; **no es una promesa de implementar persistencia, límites y Power BI dentro de esas seis horas**. Para esa entrega inmediata conviene priorizar alcance coherente, recorrido demostrable del prototipo, lista explícita de faltantes y evidencia de los avances que realmente puedan verificarse. Conectar apresuradamente el SQL incompatible puede bloquear incluso el recorrido que hoy existe. MER corregido, migración, datos durables y BI deben planificarse después, salvo que la rúbrica haga imprescindible una parte concreta y el plan consolidado la priorice.

El mayor ahorro ahora es alinear el significado de los datos antes de añadir pantallas. Se conserva este informe local sin commits ni publicación; la revisión no acredita aceptación del cliente ni ejecución de pruebas.
