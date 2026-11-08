# Agenda de procesos por revisar

No constituyen flujos aprobados. Son la continuación del alcance documental
después de PN-01 y PN-02, para revisarlos con Dante uno a uno.

## Agenda actual después de aclarar la OT

Dante confirmó reemplazo físico de cilindros, técnico obligatorio por OT,
límites semanales/mensuales por sucursal y prioridad de jefatura sobre la edición
de límites. Esos acuerdos están en [Definición de OT](07_OT_DEFINICION_DANTE.md).
No implican que se hayan cerrado los siguientes recorridos:

| Proceso aún por revisar | Definiciones que faltan |
| --- | --- |
| Solicitar y crear OT | Quién ingresa la solicitud de la sucursal, datos obligatorios, detalle por refrigerante/kilos, identificación de cilindros y vínculo con Defontana. |
| Autorizar y asignar OT | Quién autoriza el pedido y sus cantidades, quién asigna al técnico, cambios de técnico, rechazo y anulaciones. Definir límites no equivale automáticamente a autorizar una OT. |
| Preparar/despachar desde bodega | Selección y posible reserva de cilindros, pesos, evidencias, guía, confirmación de entrega y entregas parciales. |
| Cambiar o agregar cilindros en sucursal | Identificar retirados y reemplazos, agregados sin retiro, contenido real entregado/retirado, fecha y evidencia. |
| Controlar ubicación y custodia | Cuándo un envase pasa de bodega al técnico, queda en sucursal, se retira y vuelve a bodega. No inferir GPS ni exigir el retorno del mismo envase que queda instalado. |
| Recibir y verificar envases retirados | Quién recibe, vacío/remanente, pesaje y estado, discrepancias, pérdidas o daños y aceptación de entregas parciales. |
| Revisar cantidades y cerrar OT | Qué se compara entre pedido, autorización y ejecución; quién acepta diferencias y quién cierra cada etapa. Reconciliar con la regla documental de retorno de envases. |
| Aplicar límites y revisar excesos | Qué kilos cuentan, desglose por gas, calendario y fecha del cómputo, registro del exceso, revisión de autorizaciones y resolución de jefatura. Jerarquía de edición ya definida. |
| Corregir registros | Qué puede rectificarse, por qué rol, motivo/evidencia y tratamiento de hechos cerrados sin borrar historia. |
| Recibir/recargar o retirar de servicio cilindros | Confirmar si ocurre y cómo cambia el inventario; no asumir venta, devolución a proveedor o recarga como procesos ya aprobados. |
| Certificar disposición Regener | Entrega física de lote, pendiente de certificado y recepción/vinculación del documento. |
| Costos, reportes y emisiones | Qué se valora, quién registra costos y metodología aplicable al reemplazo físico; no equiparar entrega de gas con emisión. |

Adicionalmente, autenticación/roles, trabajo offline y sincronización deben
revisarse como soporte transversal de esos procesos. No se agregan al desarrollo
inmediato: se mantiene PN-01 y PN-02 como alcance seleccionado.

## Referencias documentales para contrastar con el flujo aclarado

La tabla siguiente conserva la agenda documental previa. Sus menciones a cargas
e intervenciones de equipos deben contrastarse con el reemplazo físico que Dante
explicó; no se consideran automáticamente el mismo negocio.

| Proceso | Referencia | Definiciones pendientes |
| --- | --- | --- |
| Obtener OT y guías de Defontana | HU-01 / RF-02 | Importación CSV/Excel o servicio simulado; relación OT-guías-sucursales y autorización. |
| Despachar cilindros | HU-01 / HU-02 | Técnico custodio, pesajes, fotos y permisos; bloqueo de doble custodia. |
| Consultar pendientes de devolución | RN-02 / RF-07 | Estados, extraviados, responsables y seguimiento. |
| Registrar cargas en terreno | HU-05 | Equipo, causa, cilindro asignado, consumo y posible uso de varios cilindros por trabajo. |
| Capturar/revisar evidencias | HU-06 / RF-05 | Balanza y manómetros; múltiples fotos; rol revisor y momento de aprobación. |
| Evaluar criticidad | HU-07 / RN-03 | Umbrales, recurrencia, plan de corrección y alerta por sucursal. |
| Registrar offline y sincronizar | HU-04 | Persistencia local, fotografías, conflictos, recepción tardía y retorno antes de sincronizar. |
| Recibir y conciliar retornos | HU-08 | Remanente o vacío, despacho exacto, tolerancia menor a 200 g y resolución de descalces. |
| Cerrar OT | RN-02 | Separar término técnico, recepción de bodega y cierre administrativo; autorizador final. |
| Valorizar intervención | RN-05 | Refrigerante + mano de obra + repuestos + traslado; tarifas y responsables. |
| Calcular indicadores/CO2e | HU-10 / RN-04 | Resolver fórmula documental basada en kg cargados frente a estimación de masa emitida. |
| Entregar a Regener y certificar | HU-09 | Lotes entregados pendientes de certificado y disposición certificada. |
| Corregir y auditar | RNF-04 | Usuario, fecha/hora, motivo y valores anteriores/nuevos; procedimiento de rectificación. |

## Diferencias detectadas frente al último esquema SQL

El último script es una propuesta, no la representación definitiva del negocio.
Revisar antes de programar:

- Pesaje e ingreso inicial de cilindros, separado de despacho/retorno.
- Referencias y multiplicidad de guías de despacho.
- Una o varias sucursales por OT.
- Motivo explícito de correcciones y procedimientos de rectificación.
- Mano de obra, repuestos y traslado, exigidos por RN-05.
- Evidencias múltiples, incluidas fotografías de manómetros según RF-05.
- Auditor mencionado en el acta frente a Jefatura en el diseño reciente.
- Estados de envases extraviados/no conformes.
- Entrega de lotes a Regener antes de recibir un certificado.
- Fórmula de emisiones y presentación correcta de su significado.

No modificar la base en nube automáticamente a partir de esta lista. Confirmar
el proceso y luego preparar un cambio de esquema concreto.
