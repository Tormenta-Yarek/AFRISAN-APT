# AFRISAN (`AFRISAN_NUEVO`) · Núcleo Técnico del Proyecto

Responsable e integrante único actual: **Dante Moya** · Corte actualizado: **02 de noviembre de 2026**.

Esta carpeta concentra la definición operativa del negocio, el modelo de datos en PostgreSQL/Supabase, el código fuente en `.NET 8` y las baterías de validación ejecutadas.

---

## 1. Organización de `AFRISAN_NUEVO`

| Carpeta | Contenido vigente al `02/11/2026` |
| --- | --- |
| [`01_NEGOCIO`](01_NEGOCIO/README.md) | Reglas e invariantes del negocio: catálogo de refrigerantes y cilindros (`PN-01`), consulta de inventario y QR (`PN-02`), órdenes de trabajo por reemplazo o agregado físico, retiro por el Técnico con peso y foto, recepción y aprobación de Bodega previa a la revisión del Autorizador, jerarquía de límites (`Bodega → Autorizador → Jefatura`) y alcance operativo de Power BI sin emisiones de carbono. |
| [`02_BASE_DATOS`](02_BASE_DATOS/README.md) | Esquema único **`afrisan`** en PostgreSQL 16 (Supabase) con **30 tablas y 8 vistas operativas** (`05_Crear_Afrisan_Sin_Emisiones.sql`, `06_Reemplazar_Esquemas_Por_Afrisan.sql`, `07_Habilitar_Persistencia.sql`), diccionario de claves (`TABLAS_PK_FK_TIPOS_MANUAL.md`) y pruebas de validación SQL. |
| [`03_CODIGO`](03_CODIGO/README.md) | Solución [Afrisan.Nuevo.slnx](03_CODIGO/Afrisan.Nuevo.slnx) en `.NET 8` (`Afrisan.Nuevo.Contratos`, `Afrisan.Nuevo.Api`, `Afrisan.Nuevo.Web`) con persistencia PostgreSQL, Supabase Storage privado (`afrisan-evidencias`), borradores locales en `IndexedDB` y módulo `/power-bi`. |
| [`04_SEGUIMIENTO`](04_SEGUIMIENTO/DECISIONES_Y_PENDIENTES.md) | Registro de decisiones (`D-01` a `D-22`) y evidencias verificables: [`SUPABASE_2026_11_02.md`](04_SEGUIMIENTO/SUPABASE_2026_11_02.md), [`PERSISTENCIA_2026_11_02`](04_SEGUIMIENTO/PERSISTENCIA_2026_11_02/README.md) (37 pruebas), [`STORAGE_2026_11_02`](04_SEGUIMIENTO/STORAGE_2026_11_02/README.md) (55 pruebas), [`POWER_BI_2026_11_02`](04_SEGUIMIENTO/POWER_BI_2026_11_02/README.md) (100 pruebas) y [`FLUJO_TERRENO_2026_10_25`](04_SEGUIMIENTO/FLUJO_TERRENO_2026_10_25/README.md) (274 pruebas). |

---

## 2. Documentación Académica Sincronizada (`Fase 1` y `Fase 2`)

- **[`Fase 1/Documentación del proyecto`](<../Fase 1/Documentación del proyecto>):** Acta de Constitución (`00`), Product Backlog (`01`), Sprint Backlog (`02`), Plan de Trabajo e Hitos (`03`), Carta Gantt (`04`), Informe de Fase 1 (`05`), Plan de Comunicación (`06`), Entrevista (`07`), Matriz RACI (`08`), Retrospectivas (`09`), `Sprint 0/` y `Arquitectura/`.
- **[`Fase 2/Evidencias Proyecto/Evidencias de documentación`](<../Fase 2/Evidencias Proyecto/Evidencias de documentación>):** Plan de Trabajo y Backlog de Fase 2 (`01`), Informe de Avance de Fase 2 (`02`), Monitoreo y Ajustes (`03`), Informe de Cierre Documental de Fase 2 (`04`), Evidencias y Pendientes (`05`) y Anexo de Arquitectura y Modelo de Datos (`ANEXOS/01_Arquitectura_y_Modelo_de_Datos_Afrisan_DM.docx`).
