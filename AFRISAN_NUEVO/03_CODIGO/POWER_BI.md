# Power BI operativo de AFRISAN

Preparación del **02/11/2026**, solicitada por Dante. El usuario confirmó que
todavía no tiene un informe publicado. Las emisiones quedan fuera del alcance.

## Qué está preparado

- Página `/power-bi` y menú para **Administrador y Jefatura**, con comprobación
  de rol también en cada endpoint del servidor. Alcance global de gestión.
- Vista previa con registros PostgreSQL reales: inventario al corte, OT por
  estado, kilos en cilindros instalados, retirados pendientes e incidencias.
- Filtros por fechas, sucursal, refrigerante y técnico; gráficos de barras,
  estados vacíos, carga, errores y botón para actualizar.
- Descarga autenticada de un ZIP con seis CSV, consulta Power Query, medidas
  DAX, tema AFRISAN, manifiesto del corte e instrucciones para crear el PBIX.
- Espacio para insertar un informe **privado** cuando se publique. La API
  construye su URL con ReportId/TenantId GUID y dominio Microsoft fijo.
  No acepta enlaces públicos ni HTML suministrado por el usuario.

No se declara creado un PBIX ni publicado un informe. La vista previa propia
de AFRISAN permite revisar las fuentes y no sustituye el informe de Power BI.

## Usar ahora

1. Entrar a `http://localhost:5286/acceso` con el administrador real.
2. Abrir **Power BI** en el menú.
3. Revisar la vista previa y descargar el paquete. La base puede estar vacía:
   no se cargan ejemplos ni valores ficticios.
4. Descomprimir en una carpeta privada estable y seguir su `LEEME.md` en
   Power BI Desktop. Incluye el modelo y las páginas/gráficos sugeridos.

El paquete siempre contiene el corte completo, aunque se hayan aplicado filtros
en la vista previa. Los CSV se actualizan juntos descargando otro paquete;
después hay que pulsar Actualizar en Power BI Desktop. No hay sincronización
automática con Supabase o Power BI Service en este incremento.

## Definiciones y fechas

| Indicador | Regla |
| --- | --- |
| Cilindros | Estado actual al corte; filtros de ubicación/sucursal, gas y custodio técnico. No usa fecha |
| OT abiertas/cerradas | Estado actual de las OT creadas entre Desde y Hasta, ambos incluidos |
| Kg instalados | Suma del peso neto medido al instalar, solo operaciones Registradas confirmadas en el período |
| Retirados pendientes | Reemplazos confirmados en el período que todavía no tienen RecepcionBodega |
| Incidencias abiertas | Registros del período sin FechaRevision; con filtro de gas se consideran OT que pidieron ese gas |

Los días corresponden a America/Santiago y las marcas horarias se conservan
en UTC. Una OT creada fuera del período puede tener una operación dentro:
los indicadores usan sus propias fechas. Para consultar todos los pendientes,
ampliar el período. No se reconstruyen estados de inventario o pendientes a una
fecha pasada. Kg instalados no significa consumo. No hay indicadores de
límites/cumplimiento, costos ni emisiones mientras falte completar sus módulos.

Tablas del ZIP: Inventario (cilindro), Ordenes (OT), DetalleOT (OT/gas),
Operaciones (operación confirmada), Despachos (retiro desde bodega) e Incidencias.
Recepciones se enlazan por sus claves únicas, sin multiplicar hechos.
No aplanar detalles y operaciones en una sola tabla: duplicaría cantidades.

## Conectar el informe publicado

Publicar el PBIX en un área de trabajo privada de Power BI Service y conceder
acceso a sus lectores. Obtener ReportId desde Archivo > Insertar informe >
Sitio web o portal (inserción segura). Después, en este equipo:

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File "D:\Afrisan\AFRISAN_NUEVO\03_CODIGO\Configurar-PowerBI.ps1" -ReportId "GUID-DEL-INFORME"
```

Opcionalmente añadir `-TenantId "GUID-DEL-TENANT"`. El script guarda únicamente
identificadores en User Secrets. Reiniciar la API para aplicar la configuración.
En despliegue se pueden establecer `PowerBi__ReportId` y `PowerBi__TenantId`
como variables del servidor. No hacen falta secretos de Microsoft para este
método de inserción; cada lector inicia sesión directamente con Microsoft.

Los permisos/licencias de Microsoft se comprueban aparte del rol de AFRISAN.
La inserción segura no concede acceso automáticamente. Los navegadores pueden
requerir iniciar sesión de nuevo; se ofrece Abrir en Power BI como alternativa.
[Inserción segura y condiciones de acceso](https://learn.microsoft.com/power-bi/collaborate-share/service-embed-secure).

La actualización programada en Service requiere configurar un origen accesible
y, según la instalación, un gateway. La carpeta local y este ZIP no la activan.
[Actualización de datos](https://learn.microsoft.com/power-bi/connect-data/refresh-data).

## Acceso y operación

Endpoints JWT, solo Administrador/Jefatura:

- `GET /api/v1/power-bi/configuracion`
- `GET /api/v1/power-bi/resumen?desde=yyyy-MM-dd&hasta=yyyy-MM-dd&sucursalId=...&refrigeranteId=...&tecnicoId=...`
- `GET /api/v1/power-bi/paquete`

Usan los permisos existentes de `afrisan_api`; no requieren SQL nuevo,
cambios RLS ni acceso desde Power BI a tablas de usuarios. Lecturas en una sola
transacción REPEATABLE READ / READ ONLY. Límite inicial de 50.000 filas por
tabla; si se supera, responde 413 y no entrega un paquete parcial.

No se exportan contraseñas, JWT, correos, fotos, rutas de Storage, direcciones
ni texto libre de incidencias. Sí incluye códigos, nombres de sucursal/técnico,
estados y pesajes operativos. El archivo descargado debe conservarse de forma
privada. Cache-Control no-store en el servidor; descarga por HttpClient con JWT,
sin tokens en enlaces. CSV UTF-8, números invariantes, nulos conservados y
neutralización de fórmulas en cadenas libres.

El acceso inicial es global para gestión. Antes de habilitar otros roles o
compartir un informe por sucursal, acordar y probar su RLS en Power BI; ocultar
un menú o aplicar filtros visuales no constituye aislamiento.

## Validación y pendientes

Diagnóstico de las fuentes reales, sin iniciar HTTP ni cambiar datos:

```powershell
dotnet run --project Afrisan.Nuevo.Api --no-build --launch-profile http -- --verificar-powerbi
```

Muestra conteos y estado de configuración, sin nombres ni credenciales.

[Evidencia y comandos de prueba](../04_SEGUIMIENTO/POWER_BI_2026_11_02/README.md).
Pendientes externos: creación/revisión visual del PBIX en Power BI Desktop,
cuenta y área de trabajo Microsoft, publicación, permisos/licencias, verificación
del iframe real y estrategia de actualización automática. No se ha comprado,
publicado ni configurado ningún servicio Microsoft en nombre del usuario.
