# Flujo de terreno corregido según Dante

Fecha: 25 de octubre de 2026. Aplicación nueva .NET 8, API 5181 / Web 5286.
Acuerdo: [retiro del técnico y orden de revisión](../../01_NEGOCIO/12_FLUJO_TERRENO_DESPACHO_Y_REVISION.md).

## Cambio realizado

El técnico asignado selecciona el cilindro del refrigerante encargado en su OT,
registra el peso de salida y adjunta la foto de la balanza. Al confirmar se registra
el retiro y el cilindro queda **En proceso de despacho**, bajo su custodia.

Después registra el cambio con fotos y pesos de los cilindros retirado e instalado,
informa la devolución y envía el término a **Bodega**. Bodega confirma el cilindro
y el peso recibido y aprueba el trabajo. Solamente entonces se habilita la revisión
de fotografías y registros del **Autorizador**, previa al cierre administrativo.

Se conservan por separado el peso de terreno y el peso recibido por Bodega, con
su diferencia visible. No se sobrescribe una recepción confirmada con otro peso.
Las revisiones guardan usuario y fecha, y las fotos se consultan mediante endpoints
autenticados. Reintentar un retiro idéntico conserva la misma operación y fotografía.

## Validación automática

Compilación final de `Afrisan.Nuevo.slnx`: **0 errores y 0 advertencias**.

[verificar_flujo_terreno.py](verificar_flujo_terreno.py) ejecutó tres recorridos
con datos sintéticos: tres refrigerantes, nueve cilindros y tres OT cerradas.
Cada caso incluye retiro de dos envases por el Técnico, reemplazo con dos fotos,
recepción del retirado, devolución de la reserva sin uso, incidencia, aprobación
de Bodega, revisión del Autorizador y cierre.

La ejecución final [20261025-212141-bb482c](ejecuciones/20261025-212141-bb482c/resultados.json)
registró **274 comprobaciones correctas, 0 fallos**. Instancia:
`86da61efde184269bd57a0ae77c2e2a6`. Las comprobaciones incluyen solicitudes HTTP
y verificaciones de contenido; no son 274 escenarios independientes.

La primera ejecución satisfactoria, `20261025-212010-8a5937`, utilizó una API
temporal en 5183. La ejecución final se repitió en 5181 después de añadir el bloqueo
de cambio del peso de recepción. Los resultados se conservan en carpetas separadas.

Se verificaron, entre otros:

- Técnico consulta únicamente candidatos disponibles del gas encargado.
- Bodega no puede registrar el retiro en lugar del Técnico.
- Falta de foto, formato inválido o peso inferior a tara no cambian la custodia.
- OT y cilindro pasan automáticamente a En proceso de despacho; se guarda el
  peso de salida ingresado por el Técnico y no se reutiliza silenciosamente el inicial.
- Reintento idéntico no duplica el retiro; usar el mismo UUID con otro peso falla.
- Cambio sin fotos y confirmación con versión antigua se rechazan.
- Bodega espera a que el Técnico informe la devolución del retirado.
- Bodega no aprueba antes del término técnico ni con recepciones pendientes.
- Autorizador no accede a las fotos de salida o terreno ni aprueba antes de Bodega.
- Se conservan ambos pesos de recepción/terreno; el caso 2 incluye una diferencia
  sintética de 50 g para comprobar que se muestra sin borrar ninguno de los registros.
- No se cierra sin las revisiones ni con incidencias sin resolver.

## Recorrido adicional completo desde las pantallas

Se creó y completó **OT-000004** desde los formularios del navegador, con las
cuentas Autorizador, Técnico y Bodega. Reutiliza cilindros ya registrados: el
inventario sigue teniendo nueve envases; quedan cuatro OT cerradas en esta instancia.

| Etapa | Registro observado |
| --- | --- |
| Autorizador prepara OT | Sucursal prueba terreno 3; encargo R404A; retiro previsto AFR-000008. |
| Técnico retira de Bodega | AFR-000007; peso bruto 1.600 g; foto de salida; En proceso de despacho. |
| Técnico registra el cambio | Retira AFR-000008 a 1.700 g e instala AFR-000007 a 1.550 g; dos fotos de terreno. |
| Técnico devuelve y termina | Informa entrega del retirado; OT Pendiente de aprobación de Bodega. |
| Bodega recibe | Confirma AFR-000008 a 1.700 g; diferencia con terreno 0 g. |
| Bodega aprueba | Revisa fotos y confirma cilindros/pesos; habilita al Autorizador. |
| Autorizador revisa y cierra | Consulta las fotos de salida y terreno; registra revisión; OT Cerrada. |

La comprobación HTTP final y el historial están en
[resultado_recorrido_navegador.json](resultado_recorrido_navegador.json).

Capturas:

- [Autorizador espera aprobación de Bodega](AUTORIZADOR_ESPERA_BODEGA.png).
- [Técnico elige cilindro y adjunta foto de salida](TECNICO_RETIRO_CON_FOTO.png).
- [Retiro confirmado: En proceso de despacho](TECNICO_EN_PROCESO_DESPACHO.png).
- [Término enviado a Bodega](TECNICO_PENDIENTE_BODEGA.png).
- [Bodega confirma peso y revisa fotos](BODEGA_CONFIRMA_PESO_Y_FOTOS.png).
- [Aprobación de Bodega anterior al Autorizador](BODEGA_APRUEBA_ANTES_AUTORIZADOR.png).
- [Autorizador revisa fotos contra registros](AUTORIZADOR_REVISA_FOTOS.png).
- [Cierre con las dos revisiones registradas](OT_CERRADA_DOS_REVISIONES.png).

Las imágenes adjuntadas son fixtures claramente marcados **PRUEBA SINTÉTICA**.
No son fotos reales de balanza ni evidencia de trabajos físicos. El ensayo de
navegador utilizó carga de archivos, no cámara, y se realizó con conexión.

## Repetición y alcance

Con API nueva iniciada y Python 3/Pillow disponibles:

```powershell
python AFRISAN_NUEVO/04_SEGUIMIENTO/FLUJO_TERRENO_2026_10_25/verificar_flujo_terreno.py
```

El script solo admite localhost/127.0.0.1 y comprueba modo Memoria y ausencia de
Supabase antes de crear ejemplos. Guarda un resultado independiente por ejecución,
sin tokens. Los códigos de OT y cilindro varían si ya existen datos.

Esta prueba sustituye el guion anterior de despacho registrado por Bodega. Los
resultados de `VALIDACION_ROLES_2026_10_25` se conservan como evidencia histórica
del flujo anterior y no acreditan el proceso corregido.

La aplicación permanece en memoria y no se modificó SQL ni Supabase. No se declara
completo todo el módulo: siguen pendientes persistencia, autenticación real,
almacenamiento durable de fotos, validación sin red/cámara física, reservas de
códigos específicos, rechazo/reenvío y rectificaciones materiales posteriores a
la aprobación. Las diferencias de pesaje quedan visibles para revisión humana;
no se implementó una tolerancia de conciliación que no haya sido definida.
