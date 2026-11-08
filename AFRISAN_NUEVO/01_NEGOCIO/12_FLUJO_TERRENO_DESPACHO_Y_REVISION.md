# Retiro por el técnico, cambio y revisión posterior

Actualización del 25 de octubre de 2026, a partir de la explicación de Dante.
Prevalece sobre las propuestas anteriores que atribuían el registro del despacho
a Bodega o enviaban directamente el término técnico al Autorizador.

## Orden confirmado

1. El técnico elige en Bodega los cilindros del encargo de su OT.
2. Al llevárselos, registra el peso de salida y la fotografía de la balanza. El
   sistema los marca automáticamente en proceso de despacho, bajo su custodia.
3. Al realizar el cambio, identifica los cilindros y registra sus pesos con las
   fotos correspondientes. Se conservan las fotos del retirado y del instalado,
   exigidas en la definición anterior del reemplazo.
4. El técnico informa la entrega del retirado a Bodega y envía el trabajo a revisión.
5. Bodega recibe físicamente el cilindro, confirma su identificación y su peso,
   y aprueba el trabajo. Informar una devolución no equivale a recibirla.
6. Después de esa aprobación, el Autorizador revisa las fotografías contra el
   registro de salida, terreno y recepción. Su revisión queda registrada antes
   del cierre administrativo.

Se distinguen la autorización inicial del pedido y la revisión final del trabajo.
El Autorizador continúa creando/asignando la OT en el prototipo; la explicación
nueva cambia el responsable del retiro y el orden de revisión posterior.

## Implementación de revisión

- La selección del técnico muestra solamente cilindros disponibles y pesados en
  Bodega de los refrigerantes encargados en su OT. No permite listar el inventario
  completo desde la cuenta de Técnico.
- Cada retiro exige su peso y una foto JPEG, PNG o WebP. Guarda actor, fecha y
  código del cilindro. Un reintento idéntico no crea otro despacho.
- Estados de OT: Asignada → En proceso de despacho → En terreno → Pendiente de
  aprobación de Bodega → Pendiente de revisión del Autorizador → Cerrada.
- Los pesos de terreno y de recepción se conservan por separado. La diferencia
  queda visible para revisión humana; no se inventa una tolerancia de aceptación.
- Bodega no puede aprobar mientras haya retirados sin recibir o envases de esa
  OT todavía sin instalar ni devolver. Debe existir el término técnico.
- El Autorizador no puede revisar las fotos ni registrar su aprobación final
  antes de la aprobación de Bodega. La API aplica ese orden además de la pantalla.
- Las dos revisiones conservan usuario, fecha y observación. No se reemplazan
  libremente fotos ni pesos confirmados.

El botón de revisión registra la declaración del usuario; el software no acredita
automáticamente que una foto sea legible ni que corresponda a una balanza real.

## Límites del acuerdo y pendientes

Se implementó la selección por refrigerante del pedido. Una reserva previa de
códigos específicos, guía/ERP, rechazo y reenvío después de una revisión,
rectificación material de pesos y tratamiento de diferencias requieren definición
adicional. No se añadieron por inferencia consumos, cargas a equipos ni emisiones.

La persistencia de este flujo en PostgreSQL (`afrisan`) y el almacenamiento de las fotografías en el bucket privado `afrisan-evidencias` de Supabase Storage quedaron implementados y verificados el **02/11/2026** (37 comprobaciones de persistencia y 55 de Storage). El retiro requiere conexión para confirmar disponibilidad y custodia; los borradores de terreno conservan el mecanismo local en `IndexedDB`.

Evidencias: [flujo implementado y pruebas](../04_SEGUIMIENTO/FLUJO_TERRENO_2026_10_25/README.md) · [persistencia PostgreSQL](../04_SEGUIMIENTO/PERSISTENCIA_2026_11_02/README.md) · [Supabase Storage](../04_SEGUIMIENTO/STORAGE_2026_11_02/README.md).
