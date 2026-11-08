# PN-05 · Consultar cilindros pendientes de devolución

Estado: propuesta para revisión de Dante; no aprobada ni implementada.
Referencia: RN-02, RF-07 y HU-08. Seguimiento basado en registros, no geolocalización.

## Objetivo y actor

Bodega identifica qué envases siguen fuera, quién los tiene y a qué trabajo
pertenecen, para controlar su devolución. La consulta de jefatura debe definirse
en la matriz de accesos del proyecto.

## Entradas

Salidas confirmadas, técnicos custodios, referencias OT/guía, destinos y retornos
registrados. Los trabajos offline no sincronizados no son información confirmada
en el servidor.

## Flujo propuesto

1. Entrar a Pendientes de devolución.
2. Mostrar cada cilindro con despacho confirmado que aún no tiene retorno registrado.
3. Consultar código, refrigerante, técnico, OT, guía, destino y fecha de salida.
4. Filtrar por técnico, OT, guía, refrigerante o fecha.
5. Seleccionar un cilindro para revisar su salida, pesaje, custodio y trabajos asociados.
6. Ver el resumen de entregados/devueltos/pendientes por despacho, OT o técnico,
   conforme se defina la relación con las guías.
7. Si el envase vuelve físicamente, continuar al proceso de recepción/retorno,
   que se documentará después; consultar no registra por sí mismo su devolución.
8. Si se reporta pérdida, daño u otra incidencia, derivar al proceso autorizado
   correspondiente sin ocultar ni eliminar el despacho original.

## Ejemplo explicativo (sin poblar datos)

OT con tres cilindros entregados y dos recibidos: mostrar tres entregados, dos
devueltos y uno pendiente, identificando cuál falta y su custodio. No marcar toda
la OT como devuelta porque regresó parte del lote.

## Reglas

- Separar pendiente de devolución física de recibido con descalce. Un envase
  recibido no está físicamente pendiente aunque su conciliación siga sin resolver.
- La documentación impide cerrar administrativamente la OT si faltan envases
  despachados respecto de su guía.
- No inferir que un cilindro está en una sucursal concreta solo por el destino
  previsto. Mostrar destino registrado y estado confirmado; no prometer GPS.
- Un cilindro sin retorno no se clasifica automáticamente como perdido.
- No definir retraso si no existe plazo esperado de devolución aprobado.
- Las incidencias y correcciones deben conservar historial y responsables.
- Si no hay pendientes, informarlo. Si falla la consulta, mostrar error y permitir
  reintentar, sin confundirlo con cero pendientes.

## Resultado

Bodega identifica los envases pendientes y los responsables registrados; puede
continuar con recepción o revisión de una incidencia sin alterar datos al consultar.

## Preguntas pendientes

1. ¿Existe una fecha esperada o plazo para devolver los cilindros?
2. ¿Quién registra y autoriza estados extraviado, dañado o no conforme?
3. ¿Se controla cada cilindro, cada guía y cada OT con plazos diferentes?
4. ¿Se necesitan recordatorios? Frecuencia, destinatarios y canal por definir;
   este proceso no envía mensajes automáticamente.
5. ¿Puede volver el cilindro con gas restante? Esa definición afecta recepción
   y conciliación, que todavía no se dan por aprobadas.

Impacto en datos: el esquema actual necesita revisión de los estados de pérdida,
incidencias, guías y posibles plazos; no se presume que ya están representados.
