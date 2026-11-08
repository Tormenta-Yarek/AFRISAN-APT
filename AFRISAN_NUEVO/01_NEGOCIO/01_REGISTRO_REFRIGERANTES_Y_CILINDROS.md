# PN-01 · Registro de refrigerantes y cilindros

## Estado y objetivo

**Primer proceso a programar, según instrucción de Dante.** La secuencia general
está acordada; el detalle de ingreso, responsables y pesaje inicial sigue en revisión.

Actualización de implementación: existe una primera versión en memoria en
03_CODIGO. Permite alta de refrigerante, cilindro con gas único, tara/capacidad,
primer pesaje con actor/fecha y QR. El pesaje inicial opcional y la matriz de
permisos son decisiones provisionales de revisión, documentadas en
[README de código](../03_CODIGO/README.md). No se aplicaron cambios a Supabase.

Objetivo: comenzar el monitoreo con un catálogo de refrigerantes y un registro
individual de cada cilindro, su gas y su cantidad inicial conocida.

## Actores

- Usuario autorizado para mantener el catálogo: rol exacto pendiente.
- Usuario de bodega que registra el envase y su pesaje: asignación propuesta,
  pendiente de confirmar permisos y posibles autorizaciones.

## Entradas

1. Código/nombre del refrigerante.
2. Identificación del cilindro y número de serie, cuando corresponda.
3. Refrigerante contenido: uno por cilindro.
4. Capacidad máxima de gas y tara del envase.
5. Peso bruto inicial, si se obtiene mediante balanza.

GWP y precios no son necesarios para identificar un cilindro; su fuente,
vigencia y uso en reportes se revisarán en los procesos correspondientes.

## Secuencia propuesta

1. Consultar si el tipo de refrigerante ya está registrado.
2. Si no existe, registrarlo mediante el usuario autorizado; no duplicarlo por
   diferencias de mayúsculas o espacios.
3. Iniciar el registro del cilindro físico.
4. Asignarle el único refrigerante que contiene.
5. Registrar capacidad y tara, distinguiendo sus unidades.
6. Registrar u obtener su identificador único y generar la etiqueta QR.
7. Registrar el primer pesaje con fecha y usuario, cuando se confirme cómo se
   realiza el ingreso. Ese pesaje no debe confundirse con un despacho.
8. Calcular el contenido como peso bruto menos tara, si hay una lectura válida.
9. Confirmar el alta y mostrar el cilindro en la consulta de inventario.

## Reglas propuestas para validar

- Un código identifica un único cilindro.
- Un cilindro contiene un único tipo de refrigerante.
- Capacidad y tara deben ser positivas.
- Un pesaje bruto válido no debe ser menor que la tara ni mayor que tara más capacidad.
- Capacidad máxima no se usa como sustituto del contenido real.
- No inventar peso cero cuando no existe pesaje.
- El contenido no se altera editando un número libre: los registros operativos
  deben explicar sus variaciones.
- Las correcciones y cambios de características con historial necesitan un
  procedimiento específico; no asumir que se puede reemplazar el pasado.

## Resultado esperado

El cilindro queda identificado, asociado a su refrigerante y consultable.
Su disponibilidad operativa y habilitación para despacho dependen de confirmar
las condiciones del ingreso y su estado inicial.

## Pendientes antes de programar

1. ¿Se reciben cilindros nuevos, recargados o ambos?
2. ¿Se pesa cada cilindro al recibirlo? ¿Quién lo registra?
3. ¿El pesaje inicial exige foto o documento de recepción?
4. ¿Se permite crear un cilindro sin pesarlo y bloquear su despacho hasta pesarlo?
5. ¿Cómo se registra una recarga del mismo envase?
6. ¿Qué rol crea/modifica refrigerantes y qué rol registra cilindros?

## Impacto sobre el esquema SQL existente

El modelo actual tiene pesajes de salida/retorno en Movimientos, pero el ingreso
y pesaje inicial aún no tienen un proceso confirmado. No representar un ingreso
como una salida ficticia. La solución de persistencia debe definirse después de
resolver las preguntas anteriores.
