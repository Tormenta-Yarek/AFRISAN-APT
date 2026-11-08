# Procesos del negocio

Último acuerdo, 25/10/2026:
[retiro del Técnico, cambio, confirmación de Bodega y revisión del Autorizador](12_FLUJO_TERRENO_DESPACHO_Y_REVISION.md).
Prevalece sobre las propuestas previas de despacho y orden de revisión.

Responsable: Dante Moya. Fecha: 19 de octubre de 2026.

Los documentos distinguen **acuerdos expresos**, **propuestas en revisión** y
**pendientes**. No son evidencia de implementación ni aceptación del cliente.

Alcance inicial: PN-01 y PN-02. El 20 de octubre Dante autorizó programar los
procesos del técnico; su entrega en memoria y pendientes están en
[TRABAJO_TECNICO.md](../03_CODIGO/TRABAJO_TECNICO.md). Los demás procesos se
mantienen en revisión. La explicación nueva de la OT está en
[Definición de Dante](07_OT_DEFINICION_DANTE.md), con las preguntas abiertas.

La última distribución de responsabilidades está registrada en
[Responsables de procesos](08_RESPONSABLES_PROCESOS.md): Autorizador es un rol
nuevo, el proceso 4 corresponde al técnico y la numeración ya fue confirmada.
Los detalles de acciones y la regla de jerarquía de límites siguen diferenciados.

El recorrido del técnico está en [Procesos del técnico](09_PROCESOS_TECNICO.md).
La versión programada no constituye aprobación de sus pasos pendientes.

El recorrido del Autorizador está en
[Procesos del Autorizador](10_PROCESOS_AUTORIZADOR.md): solicitud y creación,
autorización y asignación, revisión y cierre, excesos y correcciones. Distingue
los acuerdos, los detalles pendientes y el alcance del prototipo actual.

También forma parte del alcance el [Módulo de gráficos con Power BI](11_MODULO_POWER_BI.md),
recordado expresamente por Dante el 20/10/2026; sus indicadores y permisos están
por definir y la integración aún no está implementada.

| ID | Proceso | Estado |
| --- | --- | --- |
| PN-01 | [Registro de refrigerantes y cilindros](01_REGISTRO_REFRIGERANTES_Y_CILINDROS.md) | Primera versión en memoria implementada; reglas provisionales y aprobación funcional pendientes. |
| PN-02 | [Consulta de inventario](02_CONSULTA_INVENTARIO.md) | Primera versión en memoria con filtros, ficha e identificación por código; aceptación pendiente. |
| PN-03 | [OT: definición de Dante](07_OT_DEFINICION_DANTE.md) | Revisión futura. La propuesta anterior de OT/guía se conserva como antecedente. |
| PN-04 | [Despacho de cilindros](05_DESPACHO_CILINDROS.md) | Propuesta para revisión; confirmación de entrega y evidencia pendientes. |
| PN-05 | [Pendientes de devolución](06_SEGUIMIENTO_CUSTODIA.md) | Propuesta para revisión; incidencias y plazos pendientes. |
| Siguientes | [Despacho, terreno, retorno y demás procesos](03_PROCESOS_PENDIENTES.md) | Identificados en documentos; falta revisar cada recorrido con Dante. |

Antes de alterar el MER o programar un proceso, registrar aquí sus cambios de
actores, entradas, pasos, reglas y resultados. Las decisiones comunes están en
[Contexto y acuerdos](00_CONTEXTO_Y_ACUERDOS.md).
