# Decisiones y Seguimiento Individual del Proyecto AFRISAN

Responsable único actual: **Dante Moya** · Corte actualizado: **2 de noviembre de 2026**.

---

## 1. Estado Consolidado al `02/11/2026`

1. **Alcance ambiental diferido (`D-21` y `D-22`):**
   - Por definición del `02/11/2026`, las emisiones de carbono (`CO₂e`, factores `GWP` y estimaciones ambientales) quedan diferidas como módulo opcional futuro, ya que el reemplazo físico de un cilindro en sucursal no equivale a gas emitido a la atmósfera.
   - Se reemplazaron los esquemas previos por un **único esquema `afrisan` con 30 tablas y 8 vistas operativas** en PostgreSQL 16 (Supabase) mediante [`05_Crear_Afrisan_Sin_Emisiones.sql`](../02_BASE_DATOS/05_Crear_Afrisan_Sin_Emisiones.sql) y [`06_Reemplazar_Esquemas_Por_Afrisan.sql`](../02_BASE_DATOS/06_Reemplazar_Esquemas_Por_Afrisan.sql).
2. **Conexión a Supabase verificada (`SUP-01` — Completado):**
   - Se verificó la conexión autenticada con TLS `VerifyFull`, comprobando las **30 tablas, 8 vistas y RLS activo** ([`SUPABASE_2026_11_02.md`](SUPABASE_2026_11_02.md)).
3. **Persistencia PostgreSQL del flujo operativo (`G01` y `G02` — Completados):**
   - Se aplicó [`07_Habilitar_Persistencia.sql`](../02_BASE_DATOS/07_Habilitar_Persistencia.sql) habilitando el rol restringido `afrisan_api` (sin `SUPERUSER` ni `BYPASSRLS`).
   - La API opera por defecto contra PostgreSQL (`PgDatabase`, `PgInventario`, `PgTrabajo`, `PgUsuarios`), superando **37 comprobaciones automatizadas de persistencia, permisos y reinicio** ([`PERSISTENCIA_2026_11_02/README.md`](PERSISTENCIA_2026_11_02/README.md)).
4. **Usuarios reales y fotografías privadas en Supabase Storage (`G03` — Completado):**
   - Se creó y verificó el administrador inicial real en PostgreSQL y el módulo `/administracion` para usuarios, clientes y sucursales.
   - Se configuró y verificó el bucket privado **`afrisan-evidencias`** en Supabase Storage (`SupabaseFotos.cs`, límite 4 MiB, `JPEG/PNG/WebP`, validación `SHA-256`), superando **55 comprobaciones de carga, migración, permisos y manejo de fallos** ([`STORAGE_2026_11_02/README.md`](STORAGE_2026_11_02/README.md)).
5. **Módulo Power BI preparado y verificado en aplicación (`G08` — Implementado en aplicación):**
   - Se implementó la página `/power-bi` y los servicios `PgPowerBi.cs` / `PowerBiPaquete.cs` restringidos a `Administrador` y `Jefatura`, con indicadores reales desde PostgreSQL, filtros operativos y descarga de paquete ZIP (6 CSV, `Afrisan.pq`, `Medidas.dax`, `Tema_AFRISAN.json` y guía `LEEME.md`), superando **100 comprobaciones** ([`POWER_BI_2026_11_02/README.md`](POWER_BI_2026_11_02/README.md)). Solo resta publicar el archivo `.pbix` en el servicio cloud de Microsoft.

---

## 2. Secuencia de Trabajo y Estado por Paquete (`G01` a `G09`)

| Código | Paquete de trabajo | Estado al `02/11/2026` | Evidencia verificable |
| --- | --- | --- | --- |
| **`SUP-01`** | Verificar instalación y acceso autenticado a Supabase | **Completado** | [`SUPABASE_2026_11_02.md`](SUPABASE_2026_11_02.md) |
| **`G01`** | Revisar contratos persistentes y correspondencia con SQL | **Completado** | [`REVISION_PERSISTENCIA_2026_11_02.md`](REVISION_PERSISTENCIA_2026_11_02.md) |
| **`G02`** | Persistencia PostgreSQL del flujo e integridad transaccional | **Completado** | [`PERSISTENCIA_2026_11_02/README.md`](PERSISTENCIA_2026_11_02/README.md) (37 pruebas) |
| **`G03`** | Identidad real en BD y fotografías en Supabase Storage privado | **Completado** | [`STORAGE_2026_11_02/README.md`](STORAGE_2026_11_02/README.md) (55 pruebas) |
| **`H-LOCAL`** | Borradores locales en IndexedDB y control de versiones | **Completado en código** | `TrabajoLocalService.cs` + `PgTrabajo.cs` |
| **`G04`** | Pruebas de reinicio, aislamiento entre técnicos y concurrencia | **Completado en API aislada** | [`PERSISTENCIA_2026_11_02/README.md`](PERSISTENCIA_2026_11_02/README.md) |
| **`G05`** | Ensayos físicos en terreno con cámara móvil y corte de red | **En curso / Pendiente físico** | Lector QR validado por archivo/navegador; falta ensayo en terreno |
| **`G06`** | Límites semanales/mensuales por sucursal e investigación de excesos | **Pendiente definición cliente** | Tablas SQL creadas (`LimiteSucursal`, `MedicionLimite`); falta fórmula de cómputo |
| **`G07`** | Integración Defontana y certificados de disposición Regener | **Pendiente acceso cliente** | Tablas SQL creadas (`LoteDisposicion`, `CertificadoDisposicion`); falta contrato ERP |
| **`G08`** | Módulo Power BI e indicadores operativos | **Implementado en app (falta publicar `.pbix`)** | [`POWER_BI_2026_11_02/README.md`](POWER_BI_2026_11_02/README.md) (100 pruebas) |
| **`G09` / `F3-01`** | Aceptación con usuarios, cierre documental y defensa final | **En curso** | Paquete documental actualizado al `02/11/2026` |

---

## 3. Registro Completo de Decisiones (`D-01` a `D-22`)

| ID | Decisión | Estado y aplicación actual |
| --- | --- | --- |
| **D-01** | Concentrar el desarrollo vigente en `AFRISAN_NUEVO`. | Aplicado; la solución raíz `AFRISAN.slnx` compila los proyectos de `AFRISAN_NUEVO/03_CODIGO`. |
| **D-02** | Modelar las reglas del negocio antes de diseñar tablas y pantallas. | Aplicado en `01_NEGOCIO/00` a `12`. |
| **D-03** | Un único tipo de refrigerante por cilindro; el catálogo no guarda stock propio. | Aplicado en `Cilindros` (`FK Id_Refrigerante`) y validado en API/BD. |
| **D-04** | Mantener stack `.NET 8`, ASP.NET Core Web API, Blazor WebAssembly, EF Core y PostgreSQL/Supabase. | Aplicado e integrado. |
| **D-05** | Documentar y validar consulta de inventario, ficha y QR. | Aplicado y verificado. |
| **D-06** | Instalación de esquema SQL sin precargar datos ficticios ni contraseñas de ejemplo. | Aplicado en scripts `05`, `06` y `07`. |
| **D-07** | No crear tabla `Permisos`; usar un rol por usuario (`Rol` + `Usuario`). | Aplicado en los 5 roles del sistema. |
| **D-08** | Iniciar por `PN-01` y `PN-02` antes de expandir al flujo de órdenes de trabajo. | Completado. |
| **D-09 / D-10** | La OT consiste en **reemplazo físico o agregado de cilindros** en sucursal con técnico asignado obligatorio, no en recarga directa de equipos. | Aplicado en `OrdenTrabajo`, `DetalleOT`, `OperacionTerreno` y pantallas. |
| **D-11 / D-12** | Límites de kilos por sucursal en períodos **semanales y mensuales**. | Representado en `LimiteSucursal` e `HistorialLimiteSucursal`. |
| **D-13** | Ante exceso de límite, registrar incidencia para investigación de Jefatura sin atribuir culpabilidad automática ni bloquear la operación sin acuerdo. | Representado en `MedicionLimite` y `RevisionExceso`. |
| **D-14** | Si Jefatura modifica un límite, Bodega queda bloqueada para editarlo hasta que Jefatura la habilite expresamente. | Representado mediante `BloqueadoPorJefatura` y trigger en BD. |
| **D-15 / D-16** | Rol **Autorizador** separado de Jefatura (crea/autoriza OT, asigna técnico, resuelve incidencias y cierra OT); **Técnico** ejecuta el retiro y cambio físico; **Bodega** recibe y aprueba. | Aplicado en API, Web y PostgreSQL. |
| **D-17** | Jerarquía de autoridad: **Bodega → Autorizador → Jefatura**. | Aplicada y documentada. |
| **D-18 / D-19** | Fotografías obligatorias del pesaje del cilindro retirado y del instalado (múltiples fotos por pesaje), más peso y foto al retirar de Bodega; Bodega aprueba antes de la revisión del Autorizador. | Implementado en `PgTrabajo.cs`, `SupabaseFotos.cs` y triggers de `RevisionOT`. |
| **D-20** | Incorporar módulo de gráficos Power BI en la aplicación. | Implementado en `/power-bi` y `PgPowerBi.cs` (`02/11/2026`). |
| **D-21** | Diferir las emisiones de carbono (`CO₂e` / `GWP`) como módulo opcional futuro; mantener Power BI con indicadores operativos reales. | Aplicado el `02/11/2026`. |
| **D-22** | Retirar objetos de emisiones de la base de datos y consolidar todo en el esquema único `afrisan` (30 tablas y 8 vistas). | Aplicado y verificado en Supabase el `02/11/2026`. |
