# Corrección de selección de cilindros y lectura QR

Fecha de ejecución: 22 de octubre de 2026. Aplicación nueva, Web 5286 y API 5181.

## Cambios

- El Autorizador selecciona con casillas los cilindros instalados en la sucursal.
  La lista muestra refrigerante, serie, contenido y reservas de otras OT. El filtro
  por código es opcional. Ya no se introducen listas de códigos en un textarea.
- La creación de una OT valida que se seleccionen retiros o se indique explícitamente
  que solo se agregarán envases, y que sus gases figuren en el pedido.
- Identificar cilindro admite una foto PNG/JPEG/WebP y lectura desde cámara. La foto
  se decodifica localmente con jsQR. La cámara requiere permiso y HTTPS o localhost.
- La descarga de QR utiliza un archivo Blob PNG en lugar del enlace directo data URI.
- Bodega selecciona envases de una lista tanto para entrega como para recepción.
- Cada entrega mantiene su estado dentro de la OT: ConTecnico, Instalado o Devuelto.
  El cierre no depende de la ubicación global posterior de un cilindro reutilizado.
- Una recepción de una OT anterior no puede apropiarse de una entrega vigente en otra.
- Se admite volver a entregar a la misma OT un envase devuelto sin uso.
- Las modificaciones de fotos y la confirmación comprueban versión para evitar
  aceptar sobrescrituras de otra pestaña sin conflicto.
- El técnico puede descartar un borrador con conexión. Se conserva el historial y
  las evidencias, y deja de impedir el término de la OT. No permite borrar trabajos confirmados.
- La caché de consultas de OT se separa por usuario e instancia. Los pendientes de
  otra instancia siguen conservados y no se reasignan automáticamente.

## Resultados observados

La solución compiló con cero errores y advertencias. `resultados_http.json` registra
17 comprobaciones HTTP satisfactorias con datos simulados en memoria, incluyendo
reemplazo con dos grupos de fotos, rechazo de confirmación sin fotos, rechazo de
versiones antiguas, descarte de borrador, recepción, cierre y reutilización entre OT.

En el navegador se comprobó:

1. Selección de un cilindro de sucursal mediante casilla y creación de la OT-000005.
2. Lectura de un PNG QR generado por la API y navegación a la ficha AFR-000002.
3. Rechazo de una imagen PNG sin QR con un mensaje legible, sin bloquear la página.
4. Descarga de la etiqueta como AFR-000002.png mediante el nuevo botón.

Capturas: AUTORIZADOR_LISTA.png, QR_LEIDO_FICHA.png y QR_INVALIDO.png.
Los códigos, sucursales, fotos de pesaje y gas PRUEBA-QR son datos sintéticos de
validación; no representan servicios del cliente. No se cargaron datos en Supabase.

La cámara física no se probó ni se concedieron permisos de cámara durante esta
verificación. Tampoco se acredita publicación en URL pública, persistencia durable,
cumplimiento de límites ni una prueba completa de todas las condiciones offline.

## Repetición de la regresión

Con la API nueva iniciada, ejecutar `verificar_flujo_http.py` desde la raíz del
repositorio con Python y Pillow disponibles. Genera datos de prueba en la instancia
en memoria y archivos auxiliares en `.local/verificacion_qr`. No utiliza secretos
ni cuentas reales. Los usuarios de demostración son los del README de la aplicación.

Las capturas y resultados corresponden al código de esta entrega. Un ajuste final
añade rechazo explícito del guardado de borradores ya descartados; se validó en la compilación final y en la regresión HTTP repetida.
