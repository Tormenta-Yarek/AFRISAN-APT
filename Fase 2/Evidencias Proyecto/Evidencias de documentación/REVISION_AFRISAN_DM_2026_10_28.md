# Revisión de Cumplimiento Documental y Técnico de AFRISAN (`Afrisan_DM`)

Fecha de revisión original: 28 de octubre de 2026 · **Actualizado al cierre técnico del 02 de noviembre de 2026**.

Véase el detalle de las correcciones aplicadas en [`CONTROL_CORRECCIONES_2026_10_28.md`](CONTROL_CORRECCIONES_2026_10_28.md).

---

## 1. Estado de Cumplimiento por Ámbito al `02/11/2026`

| Ámbito | Estado al `02/11/2026` | Fundamento verificable en el repositorio |
| --- | --- | --- |
| **Contexto de negocio (`AFRISAN_NUEVO`)** | **Alineado** | Autorizador crea y asigna OT; Técnico selecciona y retira cilindros desde Bodega con peso y foto; registra reemplazo o agregado en sucursal con fotos por pesaje; Bodega recibe, pesa y aprueba; Autorizador revisa fotos y cierra la OT ([`12_FLUJO_TERRENO_DESPACHO_Y_REVISION.md`](../../../AFRISAN_NUEVO/01_NEGOCIO/12_FLUJO_TERRENO_DESPACHO_Y_REVISION.md)). |
| **Fase 1 (Definición y Planificación)** | **Completo y alineado** | Acta, Product Backlog, Sprint Backlog, Plan de 7 columnas, Carta Gantt de 18 semanas, Informe Fase 1, Plan de Comunicación, Entrevista, RACI, Retrospectivas, Guía 1.5 y Evidencias Individuales (`1.1`, `1.2`, `1.3`) actualizados en [`../../../Fase 1`](../../../Fase%201). |
| **Fase 2 (Desarrollo y Evidencias Técnicas)** | **Implementado y verificado** | Persistencia PostgreSQL en esquema único `afrisan` (30 tablas y 8 vistas sin emisiones, 37 pruebas), usuarios reales y Supabase Storage privado `afrisan-evidencias` (55 pruebas), flujo de terreno validado (274 pruebas) y módulo `/power-bi` (100 pruebas) documentados en [`01` a `05` de Fase 2](.) y [`ANEXOS/01_Arquitectura_y_Modelo_de_Datos_Afrisan_DM.docx`](ANEXOS/01_Arquitectura_y_Modelo_de_Datos_Afrisan_DM.docx). |
| **Idioma y formato** | **100 % en español** | Todos los informes, resúmenes ejecutivos, conclusiones, reflexiones individuales, guías, presentaciones y planillas se encuentran redactados íntegramente en español. |

---

## 2. Trazabilidad de Documentos Principales

1. **Fase 1 (`Fase 1/Documentación del proyecto/`):**
   - [`00_Acta_de_Constitucion_Afrisan_DM.docx`](<../../../Fase 1/Documentación del proyecto/00_Acta_de_Constitucion_Afrisan_DM.docx>)
   - [`01_Product_Backlog_Afrisan_DM.xlsx`](<../../../Fase 1/Documentación del proyecto/01_Product_Backlog_Afrisan_DM.xlsx>)
   - [`02_Sprint_Backlog_Afrisan_DM.xlsx`](<../../../Fase 1/Documentación del proyecto/02_Sprint_Backlog_Afrisan_DM.xlsx>)
   - [`03_Plan_de_Trabajo_e_Hitos_Afrisan_DM.xlsx`](<../../../Fase 1/Documentación del proyecto/03_Plan_de_Trabajo_e_Hitos_Afrisan_DM.xlsx>)
   - [`04_Carta_Gantt_Afrisan_DM.xlsx`](<../../../Fase 1/Documentación del proyecto/04_Carta_Gantt_Afrisan_DM.xlsx>)
   - [`05_Informe_Fase_1_Afrisan_DM.docx`](<../../../Fase 1/Documentación del proyecto/05_Informe_Fase_1_Afrisan_DM.docx>)
   - [`06_Plan_de_Comunicacion_Afrisan_DM.docx`](<../../../Fase 1/Documentación del proyecto/06_Plan_de_Comunicacion_Afrisan_DM.docx>)
   - [`07_Pauta_y_Registro_de_Entrevista_Afrisan_DM.docx`](<../../../Fase 1/Documentación del proyecto/07_Pauta_y_Registro_de_Entrevista_Afrisan_DM.docx>)
   - [`08_Matriz_RACI_Afrisan_DM.docx`](<../../../Fase 1/Documentación del proyecto/08_Matriz_RACI_Afrisan_DM.docx>)
   - [`09_Revision_de_Retrospectivas_Afrisan_DM.docx`](<../../../Fase 1/Documentación del proyecto/09_Revision_de_Retrospectivas_Afrisan_DM.docx>)
2. **Fase 2 (`Fase 2/Evidencias Proyecto/Evidencias de documentación/`):**
   - [`01_Plan_de_Trabajo_y_Backlog_Fase_2_Afrisan_DM.xlsx`](01_Plan_de_Trabajo_y_Backlog_Fase_2_Afrisan_DM.xlsx)
   - [`02_Informe_de_Avance_Fase_2_Afrisan_DM.docx`](02_Informe_de_Avance_Fase_2_Afrisan_DM.docx)
   - [`03_Monitoreo_y_Ajustes_Afrisan_DM.docx`](03_Monitoreo_y_Ajustes_Afrisan_DM.docx)
   - [`04_Informe_de_Cierre_Documental_Fase_2_Afrisan_DM.docx`](04_Informe_de_Cierre_Documental_Fase_2_Afrisan_DM.docx)
   - [`05_Evidencias_y_Pendientes_Afrisan_DM.xlsx`](05_Evidencias_y_Pendientes_Afrisan_DM.xlsx)
   - [`ANEXOS/01_Arquitectura_y_Modelo_de_Datos_Afrisan_DM.docx`](ANEXOS/01_Arquitectura_y_Modelo_de_Datos_Afrisan_DM.docx)
3. ** Guías y Pautas Oficiales (`Fase 1/Evidencias Grupales/` y `Fase 2/Evidencias Grupales/`):**
   - [`1.4_APT122_FormativaFase1.docx`](<../../../Fase 1/Evidencias Grupales/1.4_APT122_FormativaFase1.docx>)
   - [`1.5_GuiaEstudiante_Fase 1_Definicion Proyecto APT (Español).docx`](<../../../Fase 1/Evidencias Grupales/1.5_GuiaEstudiante_Fase 1_Definicion Proyecto APT (Español).docx>)
   - [`2.4_GuiaEstudiante_Fase 2_DesarrolloProyecto APT (Español).docx`](<../../Evidencias Grupales/2.4_GuiaEstudiante_Fase 2_DesarrolloProyecto APT (Español).docx>)
   - [`2.5_APT122_Sumativa Avance Fase2.pdf`](./02_Informe_de_Avance_Fase_2_Afrisan_DM.docx)
   - [`2.6_GuiaEstudiante_Fase 2_Informe Final Proyecto APT (Español).docx`](<../../Evidencias Grupales/2.6_GuiaEstudiante_Fase 2_Informe Final Proyecto APT (Español).docx>)

