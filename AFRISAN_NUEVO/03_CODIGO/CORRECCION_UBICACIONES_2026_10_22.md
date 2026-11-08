# Corrección de ubicaciones en reemplazos

22 de octubre de 2026. Corrige el atajo de ubicación inicial descrito en la entrega del 20/10.

## Cambio

- El alta distingue Bodega y un cilindro ya instalado en Sucursal. En este último caso exige nombre de sucursal y no genera stock disponible en Bodega.
- Crear la OT solo referencia cilindros existentes en esa sucursal. Rechaza códigos ubicados en Bodega u otra sucursal y no realiza traslados.
- El detalle de OT permite elegir reemplazos disponibles en Bodega del refrigerante autorizado. La API exige OT en estado Asignada para entregar.
- El inventario abre filtrado por Bodega y permite consultar sucursales, custodia del técnico o todas las ubicaciones. El resumen de disponibles cuenta únicamente Bodega.
- El reemplazo confirmado instala el entregado y deja el retirado con el técnico. La recepción física incorpora el retirado a Bodega.

## Recorrido

1. Registrar el envase antiguo con ubicación inicial Sucursal y su nombre.
2. Registrar el reemplazo en Bodega y su pesaje con contenido.
3. Autorizador crea la OT para la misma sucursal, indicando el antiguo como retiro.
4. Bodega selecciona y entrega el reemplazo. El técnico puede elegirlo en Cilindro instalado, y el antiguo en Cilindro retirado.
5. Registrar pesajes/fotos, confirmar reemplazo y recibir el retirado en Bodega.

La ubicación inicial solo representa existencias anteriores al uso del sistema. Para traslados posteriores se conserva el flujo de entrega, operación y recepción. No se cambia el refrigerante del envase.

## Verificación

Compilación de la solución nueva: cero errores y advertencias. Regresión aislada ejecutada sobre los stores reales, con datos e imágenes sintéticos en memoria: nueve comprobaciones satisfactorias, desde alta por ubicación hasta cierre de OT. Incluye rechazo de retiro de un envase en Bodega o en otra sucursal, ausencia de traslado al crear OT y ubicación correcta tras entregar, instalar y recibir.

No es una prueba HTTP ni del navegador y no acredita conservación en Supabase. No se cargaron datos de cliente ni se reiniciaron servidores. Para ver el cambio, iniciar o reiniciar ambos proyectos de la solución nueva y recargar la Web; el reinicio del servidor demo elimina sus datos temporales. Los datos de pruebas no se incorporan a la aplicación.
