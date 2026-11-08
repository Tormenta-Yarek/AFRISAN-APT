# Procesos de negocio del técnico

Actualización confirmada por Dante el 25/10/2026:
[retiro por el Técnico y orden de revisión](12_FLUJO_TERRENO_DESPACHO_Y_REVISION.md).
El Técnico elige y retira con peso/foto; Bodega confirma y aprueba antes de la
revisión del Autorizador. Ese acuerdo prevalece sobre las propuestas anteriores.

Fecha inicial: 20 de octubre de 2026 · Actualizado al **02/11/2026**. Responsable del proyecto: Dante Moya.
Estado: recorrido implementado y persistido en PostgreSQL (`afrisan`, `PgTrabajo.cs`) y Supabase Storage privado (`afrisan-evidencias`), descrito en [Persistencia](../03_CODIGO/PERSISTENCIA.md) y [Storage](../03_CODIGO/STORAGE.md).

## Base confirmada

- Cada OT tiene un técnico asignado por el proceso a cargo del Autorizador.
- El técnico reemplaza físicamente los cilindros y registra cambios/agregados.
- Cada cilindro contiene un único refrigerante.
- En cada reemplazo es obligatorio que el técnico tome fotos del pesaje del
  cilindro retirado y del cilindro de reemplazo. Se admiten varias fotografías
  por operación; no es una sola foto general por OT.
- El límite por sucursal es semanal/mensual. El técnico no fue habilitado para
  configurarlo; la jerarquía definida es Bodega → Autorizador → Jefatura.
- Verificación/cierre de OT y corrección de información corresponden al
  Autorizador según la distribución recibida.
- Los pasos siguientes detallan la propuesta operativa; no asignan al técnico
  la recepción oficial de bodega, el cierre administrativo ni la gestión general
  de ubicaciones/custodia que sigue por definir.

## T-01 · Consultar las OT asignadas

Propuesta: tras autenticarse, consultar sus OT y abrir el detalle de sucursal,
dirección, refrigerantes, kilos solicitados/autorizados e instrucciones. Ver
qué cilindros se piden cambiar o qué asignaciones preparó bodega, según se
confirme el momento de identificación de envases concretos.

Resultado: conoce el trabajo asignado. No crea la OT ni se asigna trabajos
ajenos por cuenta propia. Alcance de visibilidad y estados de OT por definir.

## T-02 · Retirar de bodega los cilindros para el trabajo

Regla confirmada el 25/10/2026: el Técnico selecciona en Bodega los cilindros disponibles de los refrigerantes encargados en su OT y registra el peso de salida junto con la fotografía de la balanza. Al confirmarse el retiro (`afrisan.retirar_cilindro`), el cilindro pasa automáticamente a `EnProcesoDespacho` bajo custodia del Técnico.

## T-03 · Identificar los envases en la sucursal

Propuesta: abrir la OT correcta, confirmar sucursal y leer los códigos de los
cilindros que serán retirados y los que se instalarán. Permitir búsqueda manual
de respaldo cuando el QR sea ilegible, según lo documentado en el acta.

Si el envase no coincide o no está registrado, informar y resolver la incidencia;
no inventar un código ni asumir que el técnico puede crear inventario maestro.
Falta definir ubicación exacta/equipo/punto de instalación y su obligatoriedad.

## T-04 · Ejecutar y registrar el reemplazo o agregado

Confirmado: el técnico realiza físicamente el cambio/agregado y lo registra.

Propuesta de detalle:

| Operación | Registro |
| --- | --- |
| Reemplazo | Cilindro retirado, cilindro instalado, OT, sucursal, refrigerante, técnico y fecha. |
| Agregado | Cilindro instalado, OT, sucursal, refrigerante, técnico y fecha; sin inventar un retirado. |

Distinguir pedido/autorización de ejecución real y conservar la diferencia.
No registrar un reemplazo de envases como si fuera una inyección de gas en equipo.
El peso real entregado y el remanente retirado deben diferenciarse. Dante confirmó
pesajes fotografiados para ambos cilindros del reemplazo; sigue pendiente cómo
se computan los kilos del límite y el momento exacto de cada medición.

## T-05 · Adjuntar evidencia y reportar incidencias

### Evidencia obligatoria confirmada por Dante

En cada reemplazo el técnico debe sacar fotos de ambos pesajes:

| Evidencia | Cilindro al que corresponde |
| --- | --- |
| Pesaje del retirado | Envase que se retira de la sucursal. |
| Pesaje del reemplazo | Envase que se instala en lugar del retirado. |

Interpretación operativa para revisión: al menos una fotografía de cada pesaje,
con posibilidad de varias fotos en cada grupo. No se fijó un máximo de archivos.
Una OT con varios cambios requiere evidencias de cada cambio; una fotografía
general de la OT no reemplaza los dos grupos por operación.

Cada fotografía debe quedar relacionada con OT, cambio, cilindro y condición
(retirado o reemplazo). Como propuesta de trazabilidad, vincular también lectura
bruta registrada, unidad, usuario y fecha del registro, y permitir comprobar
que el valor de la balanza sea legible. No asumir autenticidad solo por metadatos.

El formulario debe admitir borrador incompleto, pero no presentar un cambio como
completo sin evidencia de los dos pesajes. Esa validación debe existir también
en la API. En offline, conservar fotos y datos locales, mostrando que están
pendientes; no confundir guardado local con recepción del servidor.

Para un agregado sin retiro no inventar un envase retirado. Falta confirmar
expresamente qué pesaje/fotos exige esa operación. Las fotos de manómetros
mencionadas en documentación previa y las de despacho/retorno de bodega son
evidencias distintas; esta regla no las sustituye ni confirma su obligatoriedad.

Registrar hechos como daño, código ilegible, envase distinto, imposibilidad de
ejecutar el pedido o diferencias de cantidades. Una incidencia no prueba por sí
sola culpabilidad, robo, fuga ni pérdida física.

Los excesos semanales/mensuales deben conservar información y autorizaciones
para revisión. No suponer que el técnico define límites, los desbloquea o puede
autorizar por sí mismo cantidades adicionales. Tampoco se confirmó un bloqueo
automático de la operación: registrar siempre lo efectivamente ocurrido.

## T-06 · Entregar en bodega los envases retirados

Propuesta: identificar qué envases retiró y trasladó a bodega, e informar si
regresan vacíos, con remanente o con una incidencia. Bodega registra la recepción
oficial, sus pesajes y observaciones.

El envase que queda instalado en sucursal es distinto del retirado: no exigir
automáticamente el regreso de todos los envases nuevos entregados al técnico.
Plazo, posibilidad de otros viajes y gestión de pendientes por definir.

## T-07 · Informar el término del trabajo para revisión

Propuesta: comprobar que están registrados cambios/agregados, evidencias y
pendientes, y remitir el trabajo al Autorizador. Distinguir término del trabajo
en sucursal, recepción física de envases y cierre administrativo de OT.

Confirmado: el Autorizador es responsable de verificación/cierre. El técnico
no cierra unilateralmente la OT administrativa. Los estados y condiciones de
envío, devolución para corrección y cierre siguen pendientes.

## T-08 · Solicitar rectificación de información

Propuesta: si identifica un error en un registro ya confirmado, solicitar su
corrección indicando dato, motivo y evidencia. El Autorizador revisa y realiza
la corrección que corresponda, conservando historial.

No se habilita al técnico para borrar pesajes, modificar límites ni sobrescribir
el pasado. Falta definir si puede editar borradores antes de confirmarlos.

## Soporte transversal: trabajo sin conexión

HU-04 exige guardar trabajos localmente y sincronizarlos al recuperar conexión.
Datos y fotografías pendientes deben mostrarse como tales; no como recepción
confirmada del servidor. Reutilizar identificador de operación al reintentar,
resolver conflictos y decidir qué ocurre si vuelve a bodega sin sincronizar.

Actualización: se añadieron borradores, fotos e incidencias locales y sincronización
de revisión. El arranque offline requiere publicación y primera instalación de
archivos estáticos; los límites están en la entrega técnica, sin validación
offline de extremo a extremo declarada.

## Decisiones a revisar con Dante

1. ¿El técnico acepta digitalmente los cilindros que recibe de bodega?
2. Confirmar momento de los dos pesajes, criterio de legibilidad y evidencia del
   agregado sin retiro; las fotos de ambos pesajes en un reemplazo ya son obligatorias.
3. ¿El cilindro pertenece a un equipo/punto de instalación específico o solo a la sucursal?
4. ¿Puede ejecutar parcialmente la OT o agregar un envase no previsto? ¿Qué autorización requiere?
5. ¿Cuándo devuelve los retirados y cómo se reconoce el remanente?
6. ¿Qué información puede editar antes de confirmar y cómo pide una rectificación posterior?
7. ¿Qué debe completar para informar trabajo terminado al Autorizador?

## Relación con la documentación actual

Lectura de contexto: 07_OT_DEFINICION_DANTE.md. Responsabilidades y jerarquía:
08_RESPONSABLES_PROCESOS.md. El significado de carga en equipo de los documentos
anteriores no se traslada automáticamente a este reemplazo físico. Resolver esas
diferencias antes de modificar las tablas o implementar las pantallas del técnico.

## Impacto sobre el diseño de datos (Implementado al 02/11/2026)

Una columna `FotoBalanza` única no basta para representar varias fotos de distintos
envases y pesajes. Por ello, el esquema vigente `afrisan` modela la tabla `EvidenciaPesaje`
relacionada con cada `Pesaje` (`Despacho`, `Instalado`, `Retirado`), almacenando los objetos
en el bucket privado `afrisan-evidencias` de Supabase Storage y verificando su integridad con `SHA-256`.
