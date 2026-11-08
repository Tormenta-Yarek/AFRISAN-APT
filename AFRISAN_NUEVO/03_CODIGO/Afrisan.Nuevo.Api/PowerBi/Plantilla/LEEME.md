# AFRISAN · Paquete para Power BI Desktop

Este ZIP contiene un corte real de la base, seis CSV UTF-8, Power Query,
medidas DAX y un tema. No es un archivo PBIX ni un informe ya publicado.
Manifiesto.json indica cuándo se obtuvo y cuántas filas contiene.

## Crear el informe

1. Descomprimir el ZIP en una carpeta privada y estable, por ejemplo
   `D:\Afrisan\DatosPowerBI`. No combinar diferentes cortes en esa carpeta.
2. Abrir Power BI Desktop. En Transformar datos > Administrar parámetros,
   crear un parámetro de texto llamado `CarpetaAfrisan` con esa ruta.
3. Crear una consulta en blanco llamada `Afrisan`. En Editor avanzado pegar
   el contenido de Afrisan.pq. La consulta devuelve un registro de tablas.
4. Crear seis consultas en blanco con estos nombres y fórmulas:

   | Nombre | Fórmula en la barra de fórmulas |
   | --- | --- |
   | Inventario | `= Afrisan[Inventario]` |
   | Ordenes | `= Afrisan[Ordenes]` |
   | DetalleOT | `= Afrisan[DetalleOT]` |
   | Operaciones | `= Afrisan[Operaciones]` |
   | Despachos | `= Afrisan[Despachos]` |
   | Incidencias | `= Afrisan[Incidencias]` |

5. Cerrar y aplicar. En Modelo crear relaciones de `Ordenes[OtId]` (1) a
   `DetalleOT[OtId]`, `Operaciones[OtId]`, `Despachos[OtId]`, `Incidencias[OtId]`
   (*), todas en dirección única desde Ordenes. No crear relaciones adicionales
   automáticamente: Inventario es una foto del estado actual y queda independiente.
6. Crear por separado las medidas de Medidas.dax. No sumar identificadores.
7. En Vista > Temas > Examinar temas importar Tema_AFRISAN.json.
8. Guardar como AFRISAN_Operativo.pbix.

## Páginas y gráficos sugeridos

| Página | Visuales | Segmentadores |
| --- | --- | --- |
| Inventario actual | Tarjetas de cilindros registrados, sin pesaje y disponibles; barras por Estado; barras por Refrigerante; Ubicacion | Campos de Inventario: Refrigerante, Estado, Ubicacion, Sucursal, Tecnico. Sin filtro de fecha |
| Órdenes | Tarjetas de OT abiertas/cerradas; barras de OT por Estado y Sucursal; kilos solicitados/autorizados por refrigerante en DetalleOT | Ordenes: DiaCreacion, Sucursal, Tecnico. Refrigerante de DetalleOT solo filtra sus kilos |
| Trabajo en terreno | Tarjeta de Kg en cilindros instalados; barras por Operaciones[Refrigerante]; serie por DiaConfirmacion; tabla de retirados pendientes | Operaciones: DiaConfirmacion, Refrigerante, Tipo. Ordenes: Sucursal y Tecnico. No añadir DiaCreacion aquí |
| Incidencias | Tarjeta de incidencias abiertas; barras por Tipo; tabla de fechas de registro/revisión | Incidencias: Dia, Tipo, Abierta. Ordenes: Sucursal y Tecnico |

Fechas de día calculadas en America/Santiago, con cambios de horario. Marcas
horarias completas en UTC. Las fechas significan cosas distintas: creación de
OT, confirmación de operación, despacho o registro de incidencia. No usar un
único filtro de fecha de creación para fingir que representa todas las actividades.

## Significado de los datos

- Inventario: una fila por cilindro al corte; peso neto del último pesaje,
  que puede ser antiguo. Un peso ausente permanece vacío, no cero.
- Ordenes: una fila por OT. Estado y pendientes son actuales al corte,
  aunque se filtre una fecha de registro anterior; no son un histórico de estados.
- DetalleOT: una fila por OT y refrigerante. No unir físicamente a operaciones:
  multiplicaría los kilos solicitados cuando haya varias operaciones.
- Operaciones: solo confirmadas, una fila por operación; excluye borradores
  y descartes. KgInstalados = (peso bruto de instalación - tara) / 1000.
  Representa contenido en el cilindro al instalar, no consumo de equipos.
  KgRetirados y KgRecepcion usan sus propios pesajes. PendienteRecepcion
  cuenta reemplazos todavía sin recepción; no cuenta despachos sin usar.
- Despachos: una fila por despacho, con devolución si se devolvió sin usar.
- Incidencias: una fila por incidencia/rectificación, sin texto libre de motivos
  ni respuestas. Abierta significa que aún no tiene FechaRevision.
- No incluye límites, costos o emisiones; esos módulos aún no aportan un
  cómputo operativo aprobado. No inventar valores o indicadores de cumplimiento.
- Cadenas que podrían ejecutarse como fórmula al abrir CSV se anteponen con
  un apóstrofo. Comillas, comas y saltos de línea se conservan mediante CSV.

## Actualizar y publicar después

La actualización inicial es MANUAL: descargar otro paquete en AFRISAN,
descomprimir reemplazando los seis CSV juntos y pulsar Actualizar en Desktop.
No existe sincronización automática ni conexión directa desde Power BI a Supabase.
No introducir contraseñas de PostgreSQL, claves Storage o tokens de AFRISAN en M.

Al disponer de Power BI Service, publicar el PBIX en un área de trabajo privada
y conceder acceso a las personas correspondientes. Cada lector necesita la
autenticación de Microsoft y los permisos/licencia o capacidad aplicables.
Usar Archivo > Insertar informe > Sitio web o portal (inserción segura).
En AFRISAN configurar el ReportId y, si corresponde, TenantId del informe.
No usar Publicar en la web: haría públicos los datos del informe.

Los permisos del módulo AFRISAN (Administrador/Jefatura) y los permisos de
Microsoft son independientes. Este informe tiene alcance global de gestión,
no aplica aislamiento por sucursal para lectores de otras sucursales. Antes
de compartir con otros roles, diseñar y verificar su RLS en Power BI.
La actualización programada en Service requiere un origen accesible/gateway
y su configuración; este paquete local no la habilita automáticamente.

Referencias oficiales:
- https://learn.microsoft.com/power-bi/collaborate-share/service-embed-secure
- https://learn.microsoft.com/power-query/connectors/folder
- https://learn.microsoft.com/power-query/connectors/text-csv
