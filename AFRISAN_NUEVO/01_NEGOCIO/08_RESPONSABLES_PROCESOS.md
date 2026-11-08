# Asignación de responsables: revisión con Dante

Fecha: 19 de octubre de 2026. Fuente: mensaje directo de Dante.
Estado: numeración, responsable del cambio físico y rol Autorizador confirmados
por Dante. Aún quedan recorridos y permisos concretos por precisar. No se
implementaron roles ni permisos y no se modificó la base.

## Mensaje original

> la uno t 12 7 8 9 autorizador, la 3 , 4 6 8 bodega

La numeración corresponde a los diez grupos de la última explicación de procesos
pendientes, no a los números PN ni a los nombres de archivos.

## Distribución corregida y confirmada

| Número de la conversación | Proceso | Responsable indicado | Aclaración pendiente |
| --- | --- | --- | --- |
| 1 | Solicitud y creación de OT | Autorizador | Confirmado por Dante; recorrido y campos pendientes. |
| 2 | Autorización y asignación del técnico | Autorizador | Confirmado por Dante; cambios/anulaciones pendientes. |
| 3 | Preparación y entrega en bodega | Bodega | Recorrido detallado aún por revisar. |
| 4 | Cambio/agregado en sucursal | Técnico | Dante corrigió la asignación anterior a bodega: el técnico realiza y registra el cambio físico. |
| 5 | Ubicación y custodia | No asignado en este mensaje | No completar automáticamente. |
| 6 | Recepción de cilindros retirados | Bodega | Recorrido detallado aún por revisar. |
| 7 | Verificación/cierre de OT | Autorizador | Rol nuevo confirmado; condiciones de cierre pendientes. |
| 8 | Límites e investigación de excesos | Autorizador y bodega | No son permisos idénticos: siguen vigentes prioridad de jefatura y bloqueo de edición por bodega. Distribución del resto de acciones pendiente. |
| 9 | Corrección de información | Autorizador | Rol confirmado; alcance, motivo y trazabilidad pendientes. |
| 10 | Procesos posteriores | No asignado en este mensaje | No completar automáticamente. |

## Respuestas recibidas

1. «Sí: 1, 2, 7, 8 y 9» para Autorizador.
2. «el tecnico me equiboque»: el proceso 4 corresponde al técnico, no a bodega.
3. «Es un rol nuevo llamado Autorizador»: no es un alias de Jefatura.

## Roles y reglas pendientes de implementación

### Jerarquía confirmada por Dante

De menor a mayor autoridad: **Bodega → Autorizador → Jefatura**.

| Nivel | Rol | Relación de autoridad |
| --- | --- | --- |
| 1 | Bodega | Por debajo de Autorizador y Jefatura. |
| 2 | Autorizador | Por encima de Bodega y por debajo de Jefatura. |
| 3 | Jefatura | Mayor autoridad dentro de esta jerarquía de negocio. |

No se asigna al Técnico ni al Administrador un lugar en esta cadena sin una
definición expresa. Jerarquía no significa que todos los roles superiores
hereden automáticamente todas las tareas operativas: la distribución de
procesos anterior sigue vigente.

Autorizador es un rol adicional al diseño reciente: Administrador, Jefatura,
Bodega y Tecnico se conservan; no se indicó que se deban eliminar o reemplazar.
Se mantiene un rol por usuario y no se agrega tabla Permisos.

La asignación del proceso 8 a Autorizador y Bodega no revoca la regla expresa
de jefatura: si jefatura modifica un límite, solo jefatura puede cambiarlo o
habilitar de nuevo a bodega. Falta precisar las acciones del Autorizador dentro
del proceso compartido (por ejemplo, revisión y resolución); no se le concede
automáticamente capacidad de saltarse ese bloqueo.

La nueva jerarquía confirma que Autorizador tampoco puede modificar ni liberar
un límite protegido por Jefatura. Falta detallar si una modificación de
Autorizador bloquea automáticamente a Bodega y quién puede rehabilitar ese
caso específico; no se aplica sin confirmación el mecanismo de Jefatura a
todos los niveles.

El CHECK de Nombre_Rol del último script enumera solo cuatro nombres. La futura
migración deberá admitir Autorizador y luego reflejar las acciones autorizadas
en la API. Esta actualización es documental; no se insertaron roles en Supabase.

La regla de límites sigue documentada en 07_OT_DEFINICION_DANTE.md. Los procesos
5 y 10 no recibieron responsable en esta conversación; no se asignan por defecto.
