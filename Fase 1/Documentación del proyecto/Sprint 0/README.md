# Sprint 0 · Preparación e Insumos de Diseño

Material de base alineado con el contexto actual de **AFRISAN** (`AFRISAN_NUEVO`, corte 02/11/2026) para el desarrollo iterativo del proyecto:

| Elemento requerido | Ubicación en el repositorio | Descripción vigente |
| --- | --- | --- |
| **Primer backlog (historias de usuario priorizadas)** | [`01_Product_Backlog_Afrisan_DM.xlsx`](../01_Product_Backlog_Afrisan_DM.xlsx) | 10 historias de usuario (`HU-01` a `HU-10`) priorizadas y alineadas con el proceso de reemplazo físico de cilindros y pesajes en gramos. |
| **Diagrama de componentes** | [`Diagramas/AFRISAN_05_DIAGRAMA_COMPONENTES.png`](Diagramas/AFRISAN_05_DIAGRAMA_COMPONENTES.png) | Cliente Web Blazor WebAssembly (`.NET 8`), API REST ASP.NET Core (`.NET 8`), PostgreSQL 16 en Supabase, Supabase Storage privado y módulo Power BI. |
| **Diagrama de despliegue** | [`Diagramas/AFRISAN_04_DIAGRAMA_DESPLIEGUE.png`](Diagramas/AFRISAN_04_DIAGRAMA_DESPLIEGUE.png) | Nodos de ejecución en navegador web (PC/móvil con IndexedDB), servidor de aplicación `.NET 8`, Supabase Cloud (PostgreSQL + Storage) y Power BI. |
| **Diagrama Entidad-Relación (MER)** | [`Modelo de datos/MER - SISTEMA AFRISAN.png`](<Modelo de datos/MER - SISTEMA AFRISAN.png>) | Modelo relacional del esquema único `afrisan` (30 tablas y 8 vistas operativas sin emisiones). |
| **Mockups / Pantallas iniciales** | [`Mockups/`](Mockups) | Vistas principales de resumen operativo, catálogo de refrigerantes, inventario de cilindros e identificación por código QR. |
