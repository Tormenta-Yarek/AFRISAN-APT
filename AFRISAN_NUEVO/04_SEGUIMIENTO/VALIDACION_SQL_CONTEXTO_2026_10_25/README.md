# Validación del SQL del contexto actual

Fecha: 25/10/2026. Script:
[04_Crear_Afrisan_Contexto_Actual.sql](../../02_BASE_DATOS/04_Crear_Afrisan_Contexto_Actual.sql).

**Resultado: CORRECTO, 144 comprobaciones.**
[Detalle JSON](resultados.json) y [guion ejecutado](validar_sql.mjs).
Motor: PostgreSQL 18.6 embebido en PGlite 0.5.11, sin conexión ni credenciales de Supabase.

## Cobertura observada

- Las 31 tablas quedan vacías; no se cargan usuarios ni ejemplos al instalar.
- Se conserva una tabla/dato de prueba en el esquema anterior `afrisan`.
- Reinstalar se detiene sin reemplazar esquemas; anon no puede leer usuarios y
  todas las tablas tienen RLS habilitado.
- Autorizador admitido; tres casos con tres refrigerantes y nueve cilindros.
- Técnico propio retira con foto/peso; Bodega y Técnico ajeno no pueden suplantarlo.
- Retiro sin foto/peso válido se rechaza. Reintento idéntico no duplica; mismo UUID
  con otro contenido falla. Doble custodia existente se rechaza.
- Cambio exige las fotos; no admite fotos posteriores a confirmación ni devolución
  fabricada desde un borrador. Cambian las ubicaciones del retirado/instalado.
- Bodega recibe el retirado aunque nunca haya salido en esa OT. Espera devolución
  informada y no aprueba con retirados/reservas pendientes.
- Autorizador espera aprobación de Bodega e incidencias resueltas. No se fabrican
  metadatos de revisión incompletos. Cierre exige las dos revisiones.
- Pesos y revisiones son inmutables. Un caso conserva una diferencia de 50 g sin
  alterar el pesaje de terreno ni atribuir pérdida automáticamente.
- Agregado sin retirado ficticio; ejecución parcial con explicación y liberación
  de reserva al cerrar. Reutilización posterior no invalida el cierre anterior.
- Límites: bloqueo de Jefatura, desbloqueo explícito, cuatro versiones conservadas
  y exceso comparado contra la versión histórica elegida.
- Costos con cantidad/tarifa explícitas, emisiones separadas y certificado de baja.
- Auditoría de hechos, sin hash/versión de seguridad y sin borrado del historial.

Además de los tres reemplazos principales, las regresiones de agregado y
reutilización completan dos OT adicionales en el motor efímero. Todos los datos y
metadatos de archivos son sintéticos. Se eliminan del motor al cerrarlo; el
resultado JSON queda como evidencia. No son fotos reales ni servicios del cliente.

## Repetición

Requiere Node.js y PGlite 0.5.11. Se instaló solo en `.local/validacion_sql_contexto`,
sin añadir dependencias a los proyectos .NET. Desde la raíz, con pnpm y Node:

```powershell
pnpm add @electric-sql/pglite@0.5.11 --dir .local/validacion_sql_contexto --ignore-scripts
python AFRISAN_NUEVO/02_BASE_DATOS/contexto_actual/construir_sql.py
node AFRISAN_NUEVO/04_SEGUIMIENTO/VALIDACION_SQL_CONTEXTO_2026_10_25/validar_sql.mjs
```

También se admite `PGLITE_MODULE_PATH` con la ruta absoluta al `dist/index.js` del
paquete. El guion guarda el resultado de la última ejecución en `resultados.json`.

## Alcance y límites

No se ejecutó nada en Supabase. PGlite usa una conexión embebida: la doble custodia
se probó secuencialmente, sin una prueba de dos sesiones concurrentes. Se revisó
el orden de bloqueos, pero no se acredita rendimiento ni todas las combinaciones
de concurrencia del servidor destino.

No se comprobó Storage, cámara, autenticación real ni integración de la API. La
prueba como propietario no acredita los permisos de una futura identidad de
backend. La API debe verificar sesión/archivo, controlar versiones y comparar
reintentos. El cómputo automático de límites y las rectificaciones materiales
posteriores a aprobación siguen por definir.

Referencias: [restricciones PostgreSQL](https://www.postgresql.org/docs/17/ddl-constraints.html),
[RLS y alcance del propietario](https://www.postgresql.org/docs/17/ddl-rowsecurity.html),
[PGlite](https://pglite.dev/docs/).
