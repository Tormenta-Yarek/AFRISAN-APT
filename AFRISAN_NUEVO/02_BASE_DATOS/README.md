# Base de Datos del Contexto Actual de AFRISAN (`Esquema afrisan · 02/11/2026`)

## 1. Instalación Vigente en Supabase: Esquema Único `afrisan` Sin Emisiones

Por definición de alcance del **02/11/2026** (`D-21` y `D-22`), la base de datos operativa de **AFRISAN** utiliza un único esquema **`afrisan`** en **PostgreSQL 16 (Supabase)** compuesto por **30 tablas relacionales y 8 vistas operativas**, retirando las tablas y columnas de emisiones de carbono (`CO₂e` / `GWP`), que quedan diferidas como módulo opcional futuro.

### Scripts vigentes del modelo actual (`02/11/2026`)

| Archivo SQL | Propósito y estado al `02/11/2026` |
| --- | --- |
| [`05_Crear_Afrisan_Sin_Emisiones.sql`](05_Crear_Afrisan_Sin_Emisiones.sql) | Instalador limpio del esquema `afrisan` (30 tablas, 8 vistas, funciones transaccionales, triggers y RLS) para instancias nuevas donde `afrisan` aún no existe. |
| [`06_Reemplazar_Esquemas_Por_Afrisan.sql`](06_Reemplazar_Esquemas_Por_Afrisan.sql) | Script transaccional ejecutado el `02/11/2026` para reemplazar los esquemas previos (`afrisan_actual`, `afrisan_bi`, `afrisan_bi_actual`) por el esquema único `afrisan` vacío. *(No volver a ejecutar sobre la base activa para no borrar registros).* |
| [`07_Habilitar_Persistencia.sql`](07_Habilitar_Persistencia.sql) | **Actualización aditiva aplicada en Supabase (`02/11/2026`):** agrega columnas de soporte para borradores (`HuellaCreacion`, `DatosBorrador`, `FotosBorrador`), crea y configura el login restringido `afrisan_api` (sin `SUPERUSER` ni `BYPASSRLS`) y habilita las políticas RLS `BackendConfiable`. |

- **Verificación de esquema y flujo sin emisiones:** [`SIN_EMISIONES_2026_11_02/README.md`](SIN_EMISIONES_2026_11_02/README.md) (140 comprobaciones en PGlite).
- **Verificación autenticada contra Supabase real (`SUP-01`):** [`../04_SEGUIMIENTO/SUPABASE_2026_11_02.md`](../04_SEGUIMIENTO/SUPABASE_2026_11_02.md) (TLS `VerifyFull`, 30 tablas, 8 vistas y RLS activo).
- **Activación de persistencia y reinicio de API (`G02`):** [`../03_CODIGO/PERSISTENCIA.md`](../03_CODIGO/PERSISTENCIA.md) y [`../04_SEGUIMIENTO/PERSISTENCIA_2026_11_02/README.md`](../04_SEGUIMIENTO/PERSISTENCIA_2026_11_02/README.md) (37 comprobaciones aprobadas).

---

## 2. Correspondencia de Tablas y Vistas con el Negocio (`AFRISAN_NUEVO`)

| Dominio / Necesidad operativa | Tablas y objetos en el esquema `afrisan` |
| --- | --- |
| **Roles y usuarios reales (5 roles)** | `Rol` (`Administrador`, `Jefatura`, `Autorizador`, `Bodega`, `Tecnico`) y `Usuario` (hash de contraseña, bloqueo temporal tras 5 intentos fallidos, versión de seguridad). Sin tabla `Permisos`. |
| **Clientes, sucursales y catálogo** | `Cliente`, `Sucursales`, `Refrigerantes` y `TarifaConcepto`. |
| **Inventario físico y pesajes en gramos** | `Cilindros` (`CilindroQR` único, `Id_Refrigerante`, `TaraGr`, `Capacidadgr`), `Pesaje` (`Inicial`, `Despacho`, `Instalado`, `Retirado`, `Recepcion`), `EvidenciaPesaje` (metadatos del bucket privado `afrisan-evidencias` y `SHA256`), `CustodiaActual` e `HistorialCilindro`. |
| **Orden de Trabajo y retiro por Técnico** | `OrdenTrabajo` (`Id_Tecnico` y `Id_Autorizador` obligatorios, `Id_Publico` UUID), `DetalleOT` (`GrSolicitados`, `GrAutorizados`), `RetiroPrevisto`, `GuiaDespacho` y `Despacho` + función transaccional `afrisan.retirar_cilindro`. |
| **Terreno, recepción y doble revisión** | `OperacionTerreno` (`Reemplazo` con cilindro instalado y retirado o `Agregado` solo con instalado), `RecepcionBodega`, `RevisionOT` (orden obligatorio `Bodega` → `Autorizador`) e `Incidencia`. |
| **Límites por sucursal y costos** | `LimiteSucursal` (semanal/mensual con bloqueo de edición por `Jefatura`), `HistorialLimiteSucursal`, `MedicionLimite`, `MedicionLimiteOT`, `RevisionExceso` y `CostoOperacion`. |
| **Bajas, disposición Regener y auditoría** | `BajaCilindro`, `LoteDisposicion`, `LoteDisposicionCilindro`, `CertificadoDisposicion`, `EventoOT` y `Auditoria`. |
| **8 Vistas Operativas y de BI** | `Inventario`, `ResumenOT`, `PesajesRecepcion`, `LimitesVigentes`, `ExcesosRegistrados`, `CostosOperaciones`, `DisposicionesCertificadas` y `PedidoYEjecucion`. |

---

## 3. Flujo Operativo e Integridad Transaccional

```mermaid
flowchart LR
  A["OT asignada (Autorizador)"] --> B["Técnico retira con peso y foto"]
  B --> C["En proceso de despacho"]
  C --> D["Reemplazo / Agregado con fotos"]
  D --> E["Pendiente de Bodega"]
  E --> F["Bodega recibe, confirma y aprueba"]
  F --> G["Autorizador revisa fotos y cierra OT"]
```

- **Unidades y precisión:** Todas las masas operativas (`TaraGr`, `Capacidadgr`, `PesoBrutoGr`, `GrSolicitados`, `GrAutorizados`) se almacenan en **gramos enteros (`1 kg = 1.000 g`)**. El contenido neto según pesaje equivale a `PesoBrutoGr - TaraGr`.
- **Seguridad en base de datos:** Las 30 tablas mantienen **Row Level Security (RLS)** activo y permisos revocados para `PUBLIC`, `anon` y `authenticated`. La API se conecta mediante el rol restringido `afrisan_api` y fija en cada transacción el actor autenticado mediante `set_config('afrisan.id_usuario', :id_usuario::text, true)`.
- **Antecedentes históricos (`01` a `04`):** Los archivos `01_Crear_Afrisan_Supabase.sql`, `02_MER_Completo_Afrisan_Nuevo.sql`, `03_Crear_Tablas_Sin_Datos.sql` y `04_Crear_Afrisan_Contexto_Actual.sql` se conservan únicamente como registro de evolución del modelo de datos; no deben ejecutarse sobre la instalación vigente.
