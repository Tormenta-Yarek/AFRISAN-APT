# Control de Correcciones Documentales y Técnicas de AFRISAN (`Afrisan_DM`)

Última actualización: **02 de noviembre de 2026** · Responsable único: **Dante Moya**.

---

## 1. Actualización Integral al Corte `02/11/2026`

En esta actualización se sincronizan los informes Word, planillas Excel, presentaciones, diagramas y guías de las carpetas oficiales **`Fase 1`** y **`Fase 2`** con los avances implementados y verificados en **`AFRISAN_NUEVO`** hasta el **02/11/2026**, manteniendo la totalidad de la documentación **exclusivamente en español** por definición expresa de Dante Moya:

| Ámbito actualizado | Estado anterior (28–29/10/2026) | Estado vigente actualizado (`02/11/2026`) | Ubicación en el repositorio oficial |
| --- | --- | --- | --- |
| **1. Esquema de base de datos y alcance ambiental (`D-21` y `D-22`)** | Dos esquemas (`afrisan_actual` y `afrisan_bi_actual`), 31 tablas y 9 vistas con tabla `EstimacionEmisiones` (`CO₂e`/`GWP`). | **Esquema único `afrisan` con 30 tablas y 8 vistas operativas sin emisiones** (`05_Crear_Afrisan_Sin_Emisiones.sql`, `06_Reemplazar_Esquemas_Por_Afrisan.sql`, `07_Habilitar_Persistencia.sql`). Emisiones diferidas como módulo futuro opcional. | [`../../../AFRISAN_NUEVO/02_BASE_DATOS/README.md`](../../../AFRISAN_NUEVO/02_BASE_DATOS/README.md) |
| **2. Persistencia PostgreSQL y conexión Supabase (`SUP-01` y `G02`)** | API únicamente en memoria (`InventarioStore`, `TrabajoStore`); `G02` no iniciado. | **Completado (`02/11/2026`):** API conectada por defecto a PostgreSQL 16 en Supabase mediante rol restringido `afrisan_api` y TLS `VerifyFull`, con **37 comprobaciones de persistencia y reinicio aprobadas**. | [`../../../AFRISAN_NUEVO/04_SEGUIMIENTO/PERSISTENCIA_2026_11_02/README.md`](../../../AFRISAN_NUEVO/04_SEGUIMIENTO/PERSISTENCIA_2026_11_02/README.md) |
| **3. Usuarios reales y fotografías privadas (`G03`)** | Cuentas demo en memoria y fotografías temporales; `G03` no iniciado. | **Completado (`02/11/2026`):** Usuarios reales persistidos con hash en PostgreSQL (administrador inicial activo) y evidencias fotográficas en el bucket privado **`afrisan-evidencias`** de Supabase Storage (**55 comprobaciones aprobadas**). | [`../../../AFRISAN_NUEVO/04_SEGUIMIENTO/STORAGE_2026_11_02/README.md`](../../../AFRISAN_NUEVO/04_SEGUIMIENTO/STORAGE_2026_11_02/README.md) |
| **4. Módulo analítico Power BI (`G08`)** | Sin implementación en la aplicación; `G08` bloqueado/no iniciado. | **Implementado en aplicación (`02/11/2026`):** Ruta `/power-bi` y `PgPowerBi.cs` para `Administrador` y `Jefatura`, vista previa con datos reales de PostgreSQL y paquete descargable de 6 CSV + Power Query (`Afrisan.pq`) + DAX (`Medidas.dax`) + tema visual (**100 comprobaciones aprobadas**). Pendiente solo publicar el `.pbix` en la nube de Microsoft. | [`../../../AFRISAN_NUEVO/04_SEGUIMIENTO/POWER_BI_2026_11_02/README.md`](../../../AFRISAN_NUEVO/04_SEGUIMIENTO/POWER_BI_2026_11_02/README.md) |
| **5. Idioma y coherencia documental** | Textos mixtos o con referencias a rutas antiguas (`ANEXOS/FUENTES`). | **Exclusivamente en español** en todos los informes, guías, presentaciones y planillas, con rutas y referencias alineadas con la estructura oficial de `Fase 1`, `Fase 2` y `AFRISAN_NUEVO`. | [`../../../Fase 1`](../../../Fase%201) y [`../../../Fase 2`](../../../Fase%202) |

---

## 2. Registro de Correcciones Previas (28–29 de octubre de 2026)

| Punto | Corrección aplicada | Ubicación principal |
| --- | --- | --- |
| **1. Plan de Fase 1** | Incorporación de las siete columnas de la guía (`Competencia`, `Actividad`, `Descripción`, `Recursos`, `Duración`, `Responsable`, `Observaciones`). | [`../../../Fase 1/Documentación del proyecto/03_Plan_de_Trabajo_e_Hitos_Afrisan_DM.xlsx`](<../../../Fase 1/Documentación del proyecto/03_Plan_de_Trabajo_e_Hitos_Afrisan_DM.xlsx>) y [`05_Informe_Fase_1_Afrisan_DM.docx`](<../../../Fase 1/Documentación del proyecto/05_Informe_Fase_1_Afrisan_DM.docx>) |
| **2. Gantt y duraciones** | Calendario de 18 semanas y tres fases, hitos académicos y secuencia de continuidad individual de Dante Moya vinculada con `G01`–`G09`. | [`../../../Fase 1/Documentación del proyecto/04_Carta_Gantt_Afrisan_DM.xlsx`](<../../../Fase 1/Documentación del proyecto/04_Carta_Gantt_Afrisan_DM.xlsx>) y [`01_Plan_de_Trabajo_y_Backlog_Fase_2_Afrisan_DM.xlsx`](01_Plan_de_Trabajo_y_Backlog_Fase_2_Afrisan_DM.xlsx) |
| **3. Conclusiones y reflexión en español** | Redacción completa en español de resumen ejecutivo, conclusiones individuales, reflexión personal y proyección profesional de Dante Moya. | [`02_Informe_de_Avance_Fase_2_Afrisan_DM.docx`](02_Informe_de_Avance_Fase_2_Afrisan_DM.docx) y [`04_Informe_de_Cierre_Documental_Fase_2_Afrisan_DM.docx`](04_Informe_de_Cierre_Documental_Fase_2_Afrisan_DM.docx) |
| **4. Estados coherentes** | Separación entre `H-LOCAL` (borradores en `IndexedDB` implementados) y `G05`/`P03` (ensayos físicos en terreno con cámara móvil y sin red). | [`01_Plan_de_Trabajo_y_Backlog_Fase_2_Afrisan_DM.xlsx`](01_Plan_de_Trabajo_y_Backlog_Fase_2_Afrisan_DM.xlsx) y [`05_Evidencias_y_Pendientes_Afrisan_DM.xlsx`](05_Evidencias_y_Pendientes_Afrisan_DM.xlsx) |
| **5. Citas y referencias** | Citas numeradas en el cuerpo y referencias trazables a los documentos y evidencias vigentes del repositorio. | Informes de `Fase 1`, `Fase 2` y [`ANEXOS/01_Arquitectura_y_Modelo_de_Datos_Afrisan_DM.docx`](ANEXOS/01_Arquitectura_y_Modelo_de_Datos_Afrisan_DM.docx) |
