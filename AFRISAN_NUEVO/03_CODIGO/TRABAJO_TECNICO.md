# Entrega de trabajo técnico · 20 de octubre de 2026

Actualización del 25/10: [retiro del Técnico con foto y aprobación previa de Bodega](../04_SEGUIMIENTO/FLUJO_TERRENO_2026_10_25/README.md).
Esta versión prevalece sobre el despacho por Bodega y el envío directo al
Autorizador descritos abajo. Incluye tres pruebas HTTP completas y una OT
completada desde los formularios. La persistencia sigue en memoria.

Actualización posterior del 22/10: [selección, QR y correcciones de flujo](../04_SEGUIMIENTO/VALIDACION_QR_Y_SELECCION_2026_10_22/README.md).
Se añadieron listas para seleccionar envases, lectura de imágenes QR y cámara,
control de custodia por entrega, versiones en fotos/confirmación y descarte de
borradores. La validación nueva incluye regresión HTTP y comprobaciones de navegador;
las referencias a cámara o pruebas pendientes del texto original son históricas.

Actualización del 22/10: [corrección de ubicaciones](CORRECCION_UBICACIONES_2026_10_22.md).
El alta ahora distingue Bodega y existencias instaladas en sucursal. Crear OT ya no reubica envases de Bodega como preparación del ejemplo. Ese cambio sustituye el paso 3 y la primera decisión provisional de este documento. La corrección incluye una regresión aislada del recorrido, sin prueba integral en navegador.

Responsable: Dante Moya. Tecnología: misma solución .NET 8 / API / Blazor
WebAssembly. **Modo de revisión en memoria, sin modificar Supabase.**

## Recorrido disponible

| Proceso | Implementación de esta entrega |
| --- | --- |
| Consultar OT asignadas | Menú Mis órdenes de trabajo. La API filtra por la identidad del técnico y deniega OT ajenas incluso por URL. |
| Consultar entrega | Detalle de códigos, gas y peso entregados por bodega; no se inventó aceptación digital obligatoria del técnico. |
| Identificar envases | Selección de códigos previstos en OT y entregados para ese trabajo. Sigue disponible la consulta manual por código. |
| Cambio/agregado | Operación física con retirado/reemplazo diferenciados y pesos brutos en gramos. Un agregado no crea un retirado ficticio. |
| Evidencias múltiples | Hasta ocho fotos de cada pesaje, identificadas por cilindro y grupo. API exige pesajes y fotos del retirado y del instalado para confirmar un reemplazo. |
| Incidencias/rectificación | Motivo, actor y fecha; solicitud sin sobrescribir el cambio original. Autorizador registra su conclusión de revisión. |
| Devolución | Técnico informa entrega del retirado; bodega confirma la recepción y peso por separado. |
| Término técnico | Envío para revisión, separado del cierre administrativo de Autorizador. |
| Guardado local | Borradores, fotos e incidencias en IndexedDB, por usuario. Reintento manual y al recuperar red, con identificadores estables y control de versiones. |

No se crearon cuentas ni datos en la nube. Inventario y OT comienzan vacíos en
el servidor. Se usan las cuentas demo de README.md.

## Cómo preparar un recorrido sin poblar automáticamente

1. Entrar como **Bodega** y registrar el refrigerante y los envases necesarios.
   Para el reemplazo se necesitan dos envases identificables; el de reemplazo
   debe tener pesaje válido con contenido y estar en bodega.
2. Entrar como **Autorizador** → Órdenes → Preparar OT. Indicar sucursal,
   dirección, técnico, solicitado/autorizado por refrigerante y códigos de los
   envases ya existentes en la sucursal que se retirarán.
3. Esta preparación manual declara la ubicación inicial de esos envases en la
   sucursal. Es soporte temporal de revisión, no una importación Defontana ni
   una modificación de ubicación disponible al técnico.
4. Entrar como **Bodega**, abrir la OT y registrar la entrega del reemplazo.
5. Entrar como **Técnico**, abrir Mis órdenes y registrar el cambio físico.
6. Seleccionar ambos códigos, ingresar los dos pesos brutos y adjuntar las fotos
   reales de ambos pesajes. Guardar borrador o confirmar.
7. Informar la entrega del retirado y, después, el término del trabajo técnico.
8. Bodega registra recepción del retirado (o de una entrega sin uso que se devuelve).
9. Autorizador revisa incidencias y puede cerrar cuando no hay envases pendientes.

No existe carga automática de ejemplos. La aplicación no determina que una
fotografía es legible o que prueba un hecho solo por haber sido adjuntada.

## Fotografías

- JPEG, PNG o WebP; máximo 4 MiB por archivo y ocho archivos por grupo/pesaje.
- La API verifica tamaño, MIME y firma de formato; no es validación visual,
  antivirus ni comprobación de autenticidad de la toma.
- El mismo archivo binario no puede utilizarse dos veces en una operación.
- Acceso a imágenes mediante endpoint autenticado, con acceso a la OT y sin
  rutas públicas ni credenciales de Storage. No se incluyen en los listados.
- En revisión se conservan en memoria del servidor, con tope conjunto de 256 MiB;
  en el dispositivo se conservan en IndexedDB, sujeto al espacio del navegador.
- Fotos/operaciones confirmadas no se eliminan ni se reemplazan libremente.
  Solo fotos de borradores pueden retirarse; la acción deja traza en servidor.

## Offline y conflictos

Los campos se guardan localmente al cambiar/confirmar el valor del control y las
fotos al adjuntarlas. El botón Guardar borrador local permite comprobar el estado
antes de salir. La pantalla indica fallo si el dispositivo no puede persistir.
No se promete conservar texto que aún no haya generado un evento de cambio.

Abrir previamente la OT con conexión para disponer de su copia. Las fotos que
se están editando quedan en el borrador local; al recuperar la red se envían
pendientes fuera de edición, y la pantalla activa ofrece Guardar y sincronizar
para evitar que un envío automático sobrescriba campos que el técnico está editando.
Las incidencias enviadas se conservan localmente y se reintentan con el mismo UUID.

La versión de cada borrador detecta ediciones concurrentes. Los conflictos se
conservan para resolución; no se sustituyen automáticamente por el último dato.
La carga de fotos usa identificadores estables para reintentar sin duplicados.

Si API se reinicia, sus OT e imágenes se borran. Los borradores locales de esa
instancia **se conservan pero no se aplican a OT de otra ejecución**. Requieren
recuperación/regularización antes de continuar; no cambiar el identificador a mano.

La sesión previamente abierta puede revisar su copia local mientras no expire;
no se permite iniciar sesión nueva sin API ni se omiten las comprobaciones del
servidor. Cerrar sesión no elimina automáticamente los borradores del dispositivo.

### Arranque de la Web sin red

- Desarrollo con F5: no se cachean los ensamblados para evitar código obsoleto.
  IndexedDB funciona mientras la aplicación esté abierta; no se garantiza un
  arranque desde cero sin acceso al servidor Web de desarrollo.
- Publicación: se incluye un service worker de archivos estáticos para arrancar
  offline tras una primera carga e instalación completa con conexión. Requiere
  HTTPS o localhost. No cachea respuestas de la API ni hace públicos los datos.
- No se ejecutó publicación ni validación offline de extremo a extremo en esta entrega.

## Decisiones provisionales

- Una OT de revisión usa sucursal/dirección de texto y códigos de envases ya
  registrados. Los catálogos persistentes y el vínculo con guías siguen pendientes.
- Un reemplazo exige gas del mismo tipo entre retirado y reemplazo. No habilita
  cambiar el refrigerante de un envase; cambios de tipo de servicio requieren definición.
- Para agregado se exige foto/peso del instalado como decisión conservadora de
  esta implementación; falta confirmación específica del negocio sobre ese caso.
- Se admiten trabajos parciales con observación de pendientes al informar término.
- El cierre exige recepción de retirados/entregas sin uso y revisión de incidencias.
  La conciliación cuantitativa específica del reemplazo aún debe definirse.

## Pendientes explícitos

- Límites semanales/mensuales por sucursal y jerarquía de edición: siguen
  documentados, sin implementar. No se confunden con gramos autorizados por OT.
- EF Core/PostgreSQL, esquema de reemplazos y colección de evidencias, Storage
  privado durable, autenticación de usuarios reales y Defontana.
- Ubicación por equipo/punto de instalación, plazo de devolución, firma de
  recepción, política de corrección material y fotos de manómetros.
- El Autorizador puede registrar conclusión de una solicitud de rectificación;
  aún no existe un procedimiento de ajuste que modifique pesos confirmados.
- La foto capturada pertenece al pesaje declarado; su legibilidad/correspondencia
  física requiere revisión humana.

## Estado de entrega

Se realizó compilación de los proyectos. No se añadieron ni ejecutaron pruebas
automatizadas; no se afirma aceptación del cliente ni validación integral de
fotos, conflictos, offline o permisos en ejecución. Las pantallas y la compilación
no acreditan por sí mismas la corrección del flujo completo.
