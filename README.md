# AFRISAN · Sistema Integral de Gestión y Trazabilidad de Refrigerantes

Repositorio oficial del Proyecto Capstone (**PTY4614 — Duoc UC**) para la empresa **AFRISAN**.
- **Integrante y responsable único actual:** **Dante Amaro Moya Negrete** (`da.moyan@duocuc.cl`).
- **Corte documental y técnico:** **02 de noviembre de 2026**.

---

## 1. Estructura Oficial del Repositorio

El repositorio está organizado conforme a la estructura oficial exigida para las **Fases 1, 2 y 3**, manteniendo la implementación técnica vigente en [`AFRISAN_NUEVO/`](AFRISAN_NUEVO/README.md):

| Carpeta | Contenido principal |
| --- | --- |
| [`Fase 1/Evidencias Individuales`](<Fase 1/Evidencias Individuales>) | Autoevaluación de competencias (`1.1`), diario de reflexión (`1.2`) y autoevaluación de Fase 1 (`1.3`) de Dante Moya (`MOYA_DANTE_...`). |
| [`Fase 1/Evidencias Grupales`](<Fase 1/Evidencias Grupales>) | Presentación de Fase 1 (`Presentación Proyecto.pptx`), pauta formativa (`1.4`) y guía de definición del proyecto (`1.5`) completa (Parte I + II). |
| [`Fase 1/Documentación del proyecto`](<Fase 1/Documentación del proyecto>) | Acta de Constitución (`00`), Product Backlog (`01`), Sprint Backlog (`02`), Plan de Trabajo e Hitos de 7 columnas (`03`), Carta Gantt de 18 semanas (`04`), Informe Fase 1 (`05`), Plan de Comunicación (`06`), Entrevista (`07`), Matriz RACI (`08`), Retrospectivas (`09`), `Sprint 0/` y `Arquitectura/`. |
| [`Fase 2/Evidencias Individuales`](<Fase 2/Evidencias Individuales>) | Diario de reflexión individual de Fase 2 (`MOYA_DANTE_2.1_APT122_DiarioReflexionFase2.docx`). |
| [`Fase 2/Evidencias Grupales`](<Fase 2/Evidencias Grupales>) | Guías del estudiante `2.4` y `2.6`, y pauta de evaluación sumativa de avance `2.5`. |
| [`Fase 2/Evidencias Proyecto`](<Fase 2/Evidencias Proyecto>) | `Presentación Proyecto.pptx`, `Evidencias de documentación/` (`01` a `05` + `ANEXOS`), `Evidencias de sistema Aplicación/` y `Base de datos/`. |
| [`Fase 3`](<Fase 3>) | Estructura preparada para `Evidencias Individuales/` (`3.1`) y `Evidencias Grupales/` (Presentación Final). |
| [`AFRISAN_NUEVO`](AFRISAN_NUEVO/README.md) | Reglas de negocio (`01_NEGOCIO`), scripts SQL y esquema `afrisan` (`02_BASE_DATOS`), solución `.NET 8` (`03_CODIGO`) y evidencias de pruebas y seguimiento (`04_SEGUIMIENTO`). |

---

## 2. Estado Técnico Vigente al `02/11/2026` (`AFRISAN_NUEVO`)

1. **Arquitectura e implementación (`.NET 8`):**
   - **Frontend:** `Afrisan.Nuevo.Web` (**Blazor WebAssembly**, puerto `5286`) con almacenamiento local de borradores y fotografías en `IndexedDB` (`TrabajoLocalService`) y lector de códigos QR.
   - **Backend:** `Afrisan.Nuevo.Api` (**ASP.NET Core Web API**, puerto `5181`) con autenticación JWT, validación de versión de seguridad por petición, control de acceso por rol/OT y repositorios transaccionales (`PgInventario`, `PgTrabajo`, `PgUsuarios`, `PgPowerBi`).
2. **Base de datos en nube (`PostgreSQL 16` en Supabase — `SUP-01` y `G02` completados):**
   - Esquema único **`afrisan`** con **30 tablas y 8 vistas operativas sin emisiones de carbono** (`05_Crear_Afrisan_Sin_Emisiones.sql`, `06_Reemplazar_Esquemas_Por_Afrisan.sql` y `07_Habilitar_Persistencia.sql`).
   - Conexión con TLS `VerifyFull`, rol restringido `afrisan_api` (sin `SUPERUSER` ni `BYPASSRLS`), **Row Level Security (RLS)** en las 30 tablas y **37 comprobaciones de persistencia y reinicio aprobadas**.
3. **Usuarios reales y almacenamiento privado de fotografías (`G03` completado):**
   - Cuentas reales con hash de contraseña en PostgreSQL y módulo `/administracion`.
   - Fotografías de retiro, cilindro retirado e instalado almacenadas en el bucket privado **`afrisan-evidencias`** de **Supabase Storage** (`SupabaseFotos.cs`), verificando firma binaria, MIME, límite de 4 MiB y hash `SHA-256` (**55 comprobaciones aprobadas**).
4. **Módulo Power BI (`/power-bi`, `G08` implementado en aplicación):**
   - Vista previa operativa conectada a PostgreSQL para `Administrador` y `Jefatura`, filtros por período, sucursal, refrigerante y técnico, y descarga de paquete ZIP con 6 tablas CSV, script Power Query (`Afrisan.pq`), medidas DAX (`Medidas.dax`) y tema visual (**100 comprobaciones aprobadas**).

---

## 3. Ejecución Rápida del Sistema

1. Abrir **[`AFRISAN.slnx`](AFRISAN.slnx)** en Visual Studio y ejecutar con el perfil **`AFRISAN NUEVO - AMBOS`** (`F5`), o ejecutar en PowerShell:
   ```powershell
   .\AFRISAN_NUEVO\03_CODIGO\Iniciar-Revision.ps1
   ```
2. La API se levanta en `http://localhost:5181` y la aplicación Web en `http://localhost:5286/acceso`.
