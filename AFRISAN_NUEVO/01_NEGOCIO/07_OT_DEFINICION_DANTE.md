# OT · Definición del negocio proporcionada por Dante

Fecha: 19 de octubre de 2026. Fuente: explicación directa de Dante en la conversación.
Estado: definición inicial recibida; preguntas abiertas. No es una implementación.

## Alcance actual

Solo PN-01 (registro de refrigerantes/cilindros) y PN-02 (consulta de inventario)
están seleccionados para el trabajo inmediato. Esta definición prepara la
revisión posterior de OT; no autoriza su programación ni cambios de base de datos.

Actualización posterior, 20/10/2026: Dante solicitó programar los procesos del
técnico. Se añadió una entrega en memoria con OT manual de soporte, sin
modificar Supabase. Ver [TRABAJO_TECNICO.md](../03_CODIGO/TRABAJO_TECNICO.md)
para el alcance real y las decisiones que siguen pendientes.

## Definición proporcionada

1. La OT comienza cuando una sucursal necesita un cambio de cilindros/refrigerante.
2. La solicitud indica los cilindros a cambiar por su tipo de refrigerante y
   la cantidad de kilos por refrigerante.
3. Cada cilindro tiene un único tipo de refrigerante.
4. Cada OT debe tener un técnico asignado.
5. Ese técnico registra los cilindros efectivamente cambiados y/o agregados.
6. Debe controlarse que el técnico no exceda ciertos kilos por sucursal porque
   esos excesos representan pérdidas para el negocio.
7. Dante aclaró que el técnico **reemplaza físicamente los cilindros**. Esta OT
   no debe describirse automáticamente como inyección de gas en los equipos.
8. **Bodega y jefatura pueden definir límites**, con prioridad de jefatura.
   Cuando jefatura modifica un límite, bodega no puede cambiarlo hasta que
   jefatura habilite expresamente de nuevo su edición por bodega.
9. Los límites son **semanales y mensuales por sucursal**. No se trata de un
   límite aislado por OT. La edición por bodega respeta el bloqueo de jefatura.
10. Cuando se supera un límite, se conserva el registro de la información
    incorrecta o del exceso y se deriva a jefatura para investigar las
    autorizaciones de los técnicos y bodegueros involucrados. No se atribuye
    responsabilidad automáticamente solo por aparecer asociado a una OT.
11. Dante confirmó un rol nuevo **Autorizador**, responsable de crear la OT,
    autorizar/asignar técnico, verificar/cerrar y corregir información; participa
    también en el proceso de límites/revisión de excesos. El técnico ejecuta y
    registra los cambios físicos. Ver 08_RESPONSABLES_PROCESOS.md.

## Control semanal y mensual por sucursal

Confirmado por Dante:

- Bodega y jefatura establecen los valores de cada sucursal para semana y mes,
  respetando la jerarquía de edición detallada más abajo.
- Un exceso debe quedar registrado y permitir identificar autorizaciones y
  participantes para revisión de jefatura. No se descarta ni oculta el hecho.
- La acción confirmada ante exceso es registro e investigación; no se ha
  definido un bloqueo automático ni una aprobación previa adicional.

Propuesta de información para respaldar la revisión, pendiente de diseñar:

| Registro | Información a conservar |
| --- | --- |
| Límite aplicado | Sucursal, semana/mes, valor, vigencia y usuario que lo definió. |
| Exceso detectado | Período, kilos computados, límite y diferencia; OT/operaciones relacionadas. |
| Participantes | Técnico asignado, bodeguero registrador y quien autorizó efectivamente. No asumir que son la misma persona. |
| Información cuestionada | Qué se registró, por quién y cuándo; evidencia y explicación del exceso o error. |
| Revisión | Jefatura revisora, observaciones y resultado de la investigación. |

Conservar el límite vigente en el momento del hecho y su historial de cambios;
editar el límite posteriormente no debe ocultar una incidencia anterior.
Exceso, error de información y pérdida física son conceptos distintos: la
revisión determina qué ocurrió, no un señalamiento automático del sistema.

## Jerarquía de edición de los límites

Regla expresa de Dante, que completa la definición anterior:

La cadena de autoridad es **Bodega → Autorizador → Jefatura**, de menor a mayor.
Autorizador está por encima de Bodega, pero no puede anular ni desbloquear una
decisión de Jefatura. La asignación de tareas de cada rol sigue diferenciada.

| Situación del límite | Bodega | Jefatura |
| --- | --- | --- |
| Edición por bodega habilitada | Puede modificarlo. | Puede modificarlo; al hacerlo bloquea su edición por bodega. |
| Jefatura lo modificó y mantiene el bloqueo | Solo consulta; no puede cambiarlo. | Puede modificarlo o habilitar la edición por bodega. |
| Jefatura vuelve a habilitar a bodega | Puede modificarlo nuevamente. | Conserva su facultad de modificación y la prioridad indicada. |

Una nueva modificación por jefatura vuelve a dejar el límite bloqueado para
bodega. No se presume desbloqueo automático por tiempo, cambio de semana/mes o
ediciones de otros usuarios de bodega. El bloqueo no equivale a una aprobación
doble obligatoria ni limita la consulta del valor vigente.

La aplicación debe conservar quién modificó el valor, cuál era antes, cuál
quedó vigente, cuándo se bloqueó y quién volvió a habilitar la edición. El control
debe aplicarse en la API, no solo deshabilitando un botón de la pantalla.
No se añade una tabla Permisos: se trata de una regla de rol y estado del límite.

Al diseñar los datos se debe precisar el alcance de cada registro de límite
(sucursal, período y posible refrigerante). Esa decisión no está aún cerrada.
También falta precisar si el cambio de un límite por Autorizador genera un
bloqueo de Bodega y cómo se habilita después; por ahora el bloqueo definido
expresamente es el originado por una modificación de Jefatura.

## Recorrido interpretado, pendiente de confirmación

Solicitud de sucursal → detalle de cilindros/refrigerantes y kilos → asignación
de técnico → ejecución y registro de cambios/agregados → control de kilos por
sucursal.

Propuesta de representación: distinguir lo solicitado de lo realmente ejecutado.
No confundir kilos pedidos, entregados y consumidos. El reemplazo de envases
por sí solo no demuestra cuánto gas se utilizó ni cuánto se emitió.

Como propuesta de trazabilidad, identificar el cilindro retirado y el que lo
reemplaza; en un agregado, identificar el cilindro incorporado sin inventar un
envase retirado. Falta confirmar cuándo se registra cada hecho y qué sucede
con el cilindro retirado hasta su recepción en bodega.

## Preguntas abiertas

- ¿Los cilindros concretos se identifican desde el pedido o se asignan después
  según tipo y cantidad?
- ¿El acumulado se distingue por tipo de refrigerante o suma todos los gases?
- ¿Qué kilos computan: solicitados, autorizados, entregados, instalados o una
  cantidad neta al descontar lo retirado? No sumar varias etapas del mismo hecho.
- ¿La semana y el mes son de calendario o ventanas móviles? ¿Qué fecha de la
  operación determina el período y desde cuándo rigen los cambios de límite?
- ¿Cómo ingresa el Autorizador el pedido y qué valida antes de asignar al técnico?
  ¿Puede reemplazarse el técnico y bajo qué condiciones?
- ¿Cómo se relaciona la solicitud con OT y guías de Defontana?
- ¿Cómo se registran los envases retirados de la sucursal, los reemplazos y los
  agregados sin reemplazo? ¿Cuándo deben regresar los retirados a bodega?

El límite de sucursal es una regla distinta del semáforo documental por carga
y capacidad de un equipo. No aplicar automáticamente 5/15 % como límite de
sucursal ni equiparar todo exceso con pérdida física demostrada.

## Impacto pendiente sobre los datos

Actualización del 20/10/2026: Dante exige fotos de los pesajes del cilindro
retirado y del de reemplazo, con varias fotos por operación. Las evidencias deben
asociarse a cada cilindro/pesaje, no a un único archivo por OT. Ver la regla en
[Procesos del técnico](09_PROCESOS_TECNICO.md).

El esquema anterior no representa completamente el pedido con detalle por tipo
de refrigerante/kilos ni un límite configurable por sucursal y período.
Debe incorporar límites semanales/mensuales con vigencia y autor, incidencias
por exceso y trazabilidad de las autorizaciones/revisión de jefatura.
El último script tampoco incluye técnico asignado obligatorio en OrdenTrabajo,
aunque exista Id_Tecnico en movimientos e intervenciones.

También debe representar la permanencia de envases en la sucursal y el vínculo
entre retirado y reemplazo. No aplicar sin revisión la regla anterior de que
todo cilindro despachado debe volver en el mismo servicio: puede quedar instalado.
La relación entre esa operación y las reglas documentales de retorno de envases
debe aclararse antes de modificar el esquema.

Confirmar agregado, medición y aplicación del límite. Luego diseñar los datos de solicitud,
ejecución, técnico y control. La propuesta anterior de PN-03 se conserva como
antecedente; esta explicación humana guía la siguiente revisión de OT.
