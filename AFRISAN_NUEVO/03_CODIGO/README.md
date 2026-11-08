# AFRISAN · Código Fuente de la Solución Vigente (`Afrisan.Nuevo.slnx`)

Corte de implementación: **02/11/2026** · Responsable: **Dante Moya**.

Solución en **.NET 8** compuesta por **`Afrisan.Nuevo.Contratos`**, **`Afrisan.Nuevo.Api` (ASP.NET Core Web API)** y **`Afrisan.Nuevo.Web` (Blazor WebAssembly)**, conectada por defecto a **PostgreSQL 16 en Supabase** (esquema `afrisan`), **Supabase Storage privado** (bucket `afrisan-evidencias`) y módulo analítico **Power BI (`/power-bi`)**.

---

## 1. Estado de los Módulos Implementados al `02/11/2026`

| Módulo / Capa | Implementación activa | Guía y evidencia de verificación |
| --- | --- | --- |
| **Persistencia PostgreSQL (`G02` / `SUP-01`)** | `PgDatabase`, `PgInventario`, `PgTrabajo` y `PgUsuarios` sobre el esquema `afrisan` (30 tablas y 8 vistas) con rol restringido `afrisan_api` y TLS `VerifyFull`. | [`PERSISTENCIA.md`](PERSISTENCIA.md) · [`../04_SEGUIMIENTO/PERSISTENCIA_2026_11_02/README.md`](../04_SEGUIMIENTO/PERSISTENCIA_2026_11_02/README.md) (37 comprobaciones aprobadas) |
| **Usuarios reales y Storage privado (`G03`)** | Autenticación JWT con cuentas reales en PostgreSQL, administración de usuarios/clientes/sucursales (`/administracion`) y fotografías privadas en Supabase Storage (`afrisan-evidencias`). | [`STORAGE.md`](STORAGE.md) · [`../04_SEGUIMIENTO/STORAGE_2026_11_02/README.md`](../04_SEGUIMIENTO/STORAGE_2026_11_02/README.md) (55 comprobaciones aprobadas) |
| **Flujo de OT, terreno y doble revisión (`G01`)** | Creación/asignación de OT por Autorizador, retiro desde Bodega por Técnico con peso y foto, reemplazo/agregado con evidencias por pesaje, recepción y aprobación de Bodega previa a la revisión y cierre del Autorizador. | [`TRABAJO_TECNICO.md`](TRABAJO_TECNICO.md) · [`../04_SEGUIMIENTO/FLUJO_TERRENO_2026_10_25/README.md`](../04_SEGUIMIENTO/FLUJO_TERRENO_2026_10_25/README.md) (274 comprobaciones aprobadas) |
| **Módulo Power BI (`/power-bi`, `G08`)** | Vista previa operativa conectada a PostgreSQL para `Administrador` y `Jefatura`, filtros por período/sucursal/refrigerante/técnico y descarga de paquete ZIP con 6 CSV, Power Query (`Afrisan.pq`), medidas DAX (`Medidas.dax`) y tema visual. | [`POWER_BI.md`](POWER_BI.md) · [`../04_SEGUIMIENTO/POWER_BI_2026_11_02/README.md`](../04_SEGUIMIENTO/POWER_BI_2026_11_02/README.md) (100 comprobaciones aprobadas) |
| **Trabajo local en navegador (`H-LOCAL`)** | `TrabajoLocalService` + `IndexedDB` (`trabajo-local.js`) para resguardar borradores y fotografías pendientes por usuario e instancia antes de confirmar su envío a la API. | [`Afrisan.Nuevo.Web/Services/TrabajoLocalService.cs`](Afrisan.Nuevo.Web/Services/TrabajoLocalService.cs) |

---

## 2. Ejecución de la Solución

### Opción A — Visual Studio
1. Abrir **`Afrisan.Nuevo.slnx`** (o **`AFRISAN.slnx`** en la raíz del repositorio).
2. Seleccionar el perfil **`AFRISAN NUEVO - AMBOS`** e iniciar (`F5`).
3. La API inicia en `http://localhost:5181` y la aplicación Web abre en `http://localhost:5286/acceso`.

### Opción B — PowerShell
```powershell
.\Iniciar-Revision.ps1
```
O en dos terminales independientes desde `AFRISAN_NUEVO/03_CODIGO`:
```powershell
dotnet run --project Afrisan.Nuevo.Api --launch-profile http
dotnet run --project Afrisan.Nuevo.Web --launch-profile http
```

### Comandos de diagnóstico (sin modificar datos)
```powershell
dotnet run --project Afrisan.Nuevo.Api --launch-profile http -- --verificar-supabase
dotnet run --project Afrisan.Nuevo.Api --launch-profile http -- --verificar-persistencia
dotnet run --project Afrisan.Nuevo.Api --launch-profile http -- --verificar-storage
dotnet run --project Afrisan.Nuevo.Api --launch-profile http -- --verificar-powerbi
```

---

## 3. Modos de Operación

1. **Modo predeterminado (`PostgreSQL` + `SupabaseStorage`):**
   - Utiliza la conexión segura configurada en `User Secrets` / variables de entorno con el login `afrisan_api` y el bucket privado `afrisan-evidencias`.
   - No vuelve silenciosamente a memoria ante un fallo de red para garantizar que ningún registro operativo se pierda sin confirmación.
2. **Modo explícito de demostración local (`Persistencia:Modo=Memoria`, solo en `Development`):**
   - Se conserva únicamente para pruebas sintéticas aisladas sin tocar Supabase. Habilita las 5 cuentas de revisión (`bodega`, `autorizador`, `jefatura`, `tecnico`, `administrador@afrisan.example`). En `Production` este modo está bloqueado por diseño.

---

## 4. Pendientes Funcionales Acordados para Cierre de Fase 2 / Fase 3

- **`G04` / `G05`:** Ensayos adicionales con cámara física en terreno y desconexión prolongada de red en dispositivos móviles.
- **`G06`:** Cómputo automático de límites semanales y mensuales por sucursal una vez ratificada la fórmula de masa aplicable y calendario con la contraparte.
- **`G07`:** Integración con Defontana (sujeta a entrega de credenciales/contrato ERP) e interfaz de lotes y certificados Regener.
- **`G08` (Publicación cloud):** Publicación del archivo `.pbix` en el servicio de Microsoft Power BI y configuración de `ReportId` / `TenantId`.
