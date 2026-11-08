# Validación del esquema único sin emisiones

Fecha: **02/11/2026**. Motor: PGlite 0.5.11, PostgreSQL embebido aislado.
No conecta a Supabase ni utiliza credenciales reales.

- `resultados_reemplazo.json`: reemplazo de los cuatro esquemas anteriores,
  instalación vacía, conservación de datos ajenos en public/auth/storage y
  reversión de las eliminaciones ante un fallo posterior.
- `resultados_flujo.json`: instalación nueva, ausencia de objetos ambientales,
  30 tablas con RLS, ocho vistas y regresión del flujo operativo. Incluye
  tres reemplazos, agregado, ejecución parcial, reutilización, roles,
  recepción, aprobaciones, límites, costos, auditoría y certificados.

Se conserva `FechaEmision` en los certificados; no representa emisiones de gases.
El archivo 04 y las revisiones de octubre permanecen como antecedentes.

Para repetir desde la raíz, con `@electric-sql/pglite` 0.5.11 instalado en
`.local/validacion_sql_contexto` o indicando `PGLITE_MODULE_PATH`:

```powershell
node AFRISAN_NUEVO/02_BASE_DATOS/SIN_EMISIONES_2026_11_02/validar.mjs
```

No acredita ejecución remota, políticas de una identidad real de backend,
almacenamiento privado, concurrencia de conexiones ni integración de la API.
