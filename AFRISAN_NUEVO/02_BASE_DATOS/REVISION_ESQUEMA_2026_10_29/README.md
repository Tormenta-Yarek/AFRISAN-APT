# Corrección del esquema recibido — 29 de octubre de 2026

Archivo para instalar: **[01_AFRISAN_Corregida.sql](01_AFRISAN_Corregida.sql)**.
El [original recibido](00_Esquema_Original_Recibido.sql) queda conservado sin cambios y no debe ejecutarse.

La corrección reutiliza íntegramente el instalador vigente `../04_Crear_Afrisan_Contexto_Actual.sql`, comprobado contra el esquema adjunto y los acuerdos de negocio actuales. No se creó un modelo alternativo: se entrega una copia identificada para esta revisión, reproducible desde la fuente vigente.

## Qué cambia respecto del esquema adjunto

| Hallazgo en el original | Corrección y representación actual |
| --- | --- |
| El rol Autorizador no está permitido. | Cinco roles admitidos: Administrador, Jefatura, Autorizador, Bodega y Tecnico; un rol por usuario, sin tabla Permisos. No se insertan cuentas ni roles. |
| OT sin técnico asignado y autorizador opcional. | OrdenTrabajo exige técnico activo y autorizador con su rol, sucursal, descripción y UUID público. El técnico solo ejecuta sus OT. |
| No distingue pedido y autorización por gas. | DetalleOT conserva gramos solicitados y autorizados por refrigerante. RetiroPrevisto identifica los cilindros de sucursal que se esperan retirar. |
| Intervencion obliga a declarar equipo, causa de falla y gas consumido. | OperacionTerreno representa Reemplazo o Agregado físico. Vincula instalado, retirado cuando corresponde y despacho exacto. No exige inventar equipos, fallas ni consumos. Equipos/CausasFalla/Intervencion del original se conservan en el esquema anterior, sin migrarlos a este flujo. |
| Movimientos usa Salida y no separa todos los hechos. | Despacho, Pesaje, OperacionTerreno, RecepcionBodega y CustodiaActual conservan etapas y responsables distintos. El técnico registra su retiro con peso/foto. |
| Una sola FotoPesaje/FotoBalanza. | EvidenciaPesaje permite varias fotos por pesaje; se diferencian salida, instalado, retirado y recepción. El reemplazo exige evidencia de ambos cilindros; no permite usar la misma foto para los dos. Se conservan metadatos privados, tamaño, MIME y SHA-256. |
| Se confunde devolución del entregado con recepción del retirado. | Bodega recibe el cilindro retirado aunque no haya salido en esa OT, o el despacho devuelto sin uso. El instalado queda en sucursal. |
| No hay aprobación sucesiva de Bodega y Autorizador. | RevisionOT exige Bodega primero y Autorizador después. El cierre comprueba recepciones, ausencia de borradores e incidencias resueltas. |
| Faltan incidencias y solicitudes de corrección con historial. | Incidencia conserva solicitud, motivo, respuesta, revisor y fecha. No reescribe pesajes o evidencias confirmadas. |
| Faltan límites y prioridad de Jefatura. | LimiteSucursal semanal/mensual, historial de versiones, bloqueo de edición a Bodega por Jefatura y habilitación expresa. MedicionLimite y RevisionExceso conservan antecedentes e investigación. |
| Las claves foráneas compuestas aparecen partidas y repiten nombres. | Relaciones ejecutables con PK/UNIQUE compatibles y claves compuestas reales, reforzadas por validaciones de OT, cilindro, técnico, sucursal y tipo de pesaje. No se aceptan enlaces ajenos al flujo. |
| Identificadores sin comillas junto a CHECK que usa mayúsculas entre comillas. | Identificadores consistentemente entre comillas: evita que PostgreSQL convierta los nombres a minúsculas y luego no encuentre las columnas citadas. |
| PrecioTotal y EmisionesCO2eTon usan DEFAULT con otras columnas. | Columnas GENERATED ALWAYS AS (...) STORED. Costos por líneas con cantidad, unidad, tarifa y moneda; emisiones separadas con masa emitida explícita, factor, fuente y metodología. Instalar un cilindro no equivale a consumir ni emitir todo su contenido. |
| Cilindros sin ubicación, custodia completa ni pesaje inicial independiente. | Separa ubicación, estado, custodia, último pesaje e historial. Comprueba tara/capacidad y contenido. Un envase de Bodega sin pesaje queda PendientePesaje. |
| Certificado obligatorio desde la entrega. | Baja administrativa, lote entregado pendiente y certificado recibido posteriormente. Los envases certificados permanecen fuera de servicio. |
| Correo sin unicidad normalizada y auditoría solo definida como tabla. | Índice único de correo normalizado; disparadores de auditoría y protección de hechos históricos. La auditoría excluye hash de contraseña y versión de seguridad. |

## Requisitos usados

- [Contexto y acuerdos](../../01_NEGOCIO/00_CONTEXTO_Y_ACUERDOS.md).
- [Definición de OT](../../01_NEGOCIO/07_OT_DEFINICION_DANTE.md).
- [Responsables](../../01_NEGOCIO/08_RESPONSABLES_PROCESOS.md).
- [Procesos del Técnico](../../01_NEGOCIO/09_PROCESOS_TECNICO.md).
- [Procesos del Autorizador](../../01_NEGOCIO/10_PROCESOS_AUTORIZADOR.md).
- [Acuerdo posterior del flujo de terreno](../../01_NEGOCIO/12_FLUJO_TERRENO_DESPACHO_Y_REVISION.md), que prevalece sobre atribuciones anteriores del despacho a Bodega.
- [Especificación técnica y límites del instalador](../README.md).

## Instalación y conservación de la base anterior

El archivo crea **afrisan_actual** y **afrisan_bi_actual**, con **31 tablas y 9 vistas**, en una sola transacción. No borra, altera ni traslada datos de `afrisan`, `afrisan_nuevo`, `public`, `auth` o Storage. No contiene datos de ejemplo ni cuentas precargadas.

Esta es una instalación nueva en esquemas separados, **no un ALTER ni una migración de registros existentes**. El adjunto es una exportación de contexto: no incluye el inventario real, políticas, funciones, índices ni datos de la base de origen. La migración requiere examinar esos elementos y mapear los hechos históricos; no se deducen consumos, reemplazos ni pesajes que el original no registró.

1. Ejecutar únicamente `01_AFRISAN_Corregida.sql` completo con una identidad administradora PostgreSQL, en el entorno destinado a la nueva instalación.
2. Si los esquemas destino ya existen, detenerse y revisar su contenido. El instalador falla sin reemplazarlos; no agregar DROP para forzarlo.
3. Aprovisionar después roles y usuarios mediante el backend, con hashes seguros. El CHECK de longitud no implementa el algoritmo de hash.
4. Integrar repositorios y transacciones de la API con el esquema nuevo. Instalar tablas no conecta EF Core; el prototipo revisado sigue usando memoria.

RLS está habilitado y el acceso de PUBLIC/anon/authenticated se revoca. El instalador no habilita acceso desde el navegador. Falta aprovisionar una identidad PostgreSQL de servidor con los permisos/políticas adecuados. El backend establece el actor autenticado dentro de cada transacción; el identificador no se acepta como campo libre del cliente. Las fotografías requieren Storage privado y verificación del archivo real por la API.

## Decisiones que siguen pendientes

- Fórmula y calendario del cómputo semanal/mensual, masa aplicable y desglose por gas. Se guardan límites y mediciones declaradas con metodología; no se inventa un cálculo automático ni un bloqueo del trabajo.
- Procedimiento material para rectificar pesajes aprobados, reapertura, anulación y reasignación de OT. El esquema protege el original y registra solicitudes; no habilita estas acciones sin definirlas.
- Responsables de negocio de tarifas, costos, emisiones, bajas y certificados. Su registro queda provisionalmente restringido a Administrador técnico, según el instalador vigente.
- Prueba en Supabase, autenticación real, permisos del backend, Storage, concurrencia entre conexiones y sincronización de dispositivos.

## Validación de esta entrega

Ejecución del 29/10/2026: **CORRECTO, 144 comprobaciones y 0 fallos**, con las 31 tablas vacías al instalar. Se comprobó además que el SHA-256 del SQL probado coincide con el archivo entregado y que la copia del original coincide con el adjunto recibido.

El resultado de la ejecución del archivo entregado queda en [resultados_validacion.json](resultados_validacion.json), con su SHA-256 y las fechas de ejecución. [manifiesto.json](manifiesto.json) identifica el original, la fuente y el SQL corregido.

La regresión cubre instalación vacía, conservación de un dato del esquema anterior, rechazo de reinstalación, acceso anon denegado, RLS, tres reemplazos con diferentes gases/cilindros y los roles, recepciones, revisiones y cierres. También incluye agregado, ejecución parcial, reutilización posterior, historial de límites, costos, emisiones, baja y certificado.

Se ejecuta en PostgreSQL embebido PGlite, con datos y metadatos de fotos sintéticos. No verifica un servidor Supabase ni archivos reales; es una prueba local y secuencial.

Para reconstruir y repetir desde `D:\Afrisan` con Node.js y la dependencia PGlite local ya instalada:

```powershell
node AFRISAN_NUEVO/02_BASE_DATOS/REVISION_ESQUEMA_2026_10_29/preparar_entrega.mjs
node AFRISAN_NUEVO/02_BASE_DATOS/REVISION_ESQUEMA_2026_10_29/validar_entrega.mjs
```

La nueva ejecución guarda su resultado en esta carpeta y conserva la evidencia histórica del 25/10/2026.
