# Diccionario de tablas AFRISAN: creación manual

Referencia: `03_Crear_Tablas_Sin_Datos.sql`. Fecha: 19 de octubre de 2026.
Este documento describe el archivo local; no certifica el estado de tu Supabase.
No inserta datos. No crea una tabla Permisos.

## Antes de empezar

1. Revisa si las tablas ya existen para no duplicarlas. El esquema de este diseño es **afrisan**. El esquema **afrisan_bi** contiene vistas, no tablas operativas.
2. Conserva exactamente los nombres de tablas y columnas, incluidas mayúsculas. En SQL se escriben con comillas: `afrisan."Usuario"`.
3. Crea las tablas y sus PK en el orden de este documento. Después crea los UNIQUE necesarios y las FK, especialmente las compuestas.
4. PK = clave primaria. FK = clave foránea. UNIQUE = valor o combinación que no puede repetirse. Nullable = permite NULL.
5. Activa Identity únicamente en las PK numéricas indicadas. **Las FK no son Identity ni autoincrementales.**
6. Todas las FK de este diseño usan **ON DELETE RESTRICT** y ON UPDATE NO ACTION por defecto. Así no se borran registros relacionados automáticamente.
7. Crear columnas, PK y FK manualmente **no instala** triggers, reglas CHECK, índices especiales, vistas ni RLS. Al final se explica qué queda pendiente.

## Equivalencias de tipos

| Tipo PostgreSQL | Nombre equivalente habitual | Uso |
| --- | --- | --- |
| smallint | int2 | Rol e Id_Rol. |
| integer | int4 | IDs de usuario, cliente, sucursal y causa de falla. |
| bigint | int8 | Resto de IDs y pesos en gramos enteros. |
| varchar(n) | character varying(n) | Texto con longitud máxima n. |
| text | text | Texto sin límite declarado, hashes y rutas de fotos. |
| boolean | bool | true/false. |
| numeric(p,s) | decimal(p,s) | p dígitos totales y s decimales; usar la precisión indicada. |
| timestamptz | timestamp with time zone | Fecha y hora con zona; **no usar timetz**. |
| date | date | Fecha sin hora. |
| uuid | uuid | Identificador de operación/sesión. |
| jsonb | jsonb | Valores anteriores y nuevos de auditoría. |

**Unidades:** todos los pesos son gramos: 1000 g = 1 kg. `Capacidadgr` no incluye la tara.
`PrecioGrRefrigerante` es precio por gramo, no por kg. Dinero usa numeric, nunca float8.
`FotoPesaje`, `FotoBalanza` y `Documento` guardan rutas privadas de Storage, no imágenes bytea.
`Clave_Usuario` guarda exclusivamente un hash seguro generado por el backend.

## Resumen de claves primarias y orden de creación

| Orden | Tabla | PK | Tipo | Generación |
| --- | --- | --- | --- | --- |
| 1 | Rol | Id_Rol | smallint | GENERATED ALWAYS AS IDENTITY |
| 2 | Usuario | Id_Usuario | integer | GENERATED ALWAYS AS IDENTITY |
| 3 | Cliente | Id_Cliente | integer | GENERATED ALWAYS AS IDENTITY |
| 4 | Sucursales | Id_Sucursal | integer | GENERATED ALWAYS AS IDENTITY |
| 5 | Refrigerantes | Id_Refrigerante | bigint | GENERATED ALWAYS AS IDENTITY |
| 6 | Cilindros | Id_Cilindro | bigint | GENERATED ALWAYS AS IDENTITY |
| 7 | Equipos | Id_Equipo | bigint | GENERATED ALWAYS AS IDENTITY |
| 8 | CausasFalla | Id_CausaFalla | integer | GENERATED ALWAYS AS IDENTITY |
| 9 | OrdenTrabajo | Id_OrdenTrabajo | bigint | GENERATED ALWAYS AS IDENTITY |
| 10 | Movimientos | Id_Movimiento | bigint | GENERATED ALWAYS AS IDENTITY |
| 11 | Intervencion | Id_Intervencion | bigint | GENERATED ALWAYS AS IDENTITY |
| 12 | PreciosRefrigerantes | Id_Precio | bigint | GENERATED ALWAYS AS IDENTITY |
| 13 | Costos | Id_Costos | bigint | GENERATED ALWAYS AS IDENTITY |
| 14 | CertificadosDisposicion | Id_Certificado | bigint | GENERATED ALWAYS AS IDENTITY |
| 15 | CertificadosCilindros | Id_Certificado, Id_Cilindro | bigint, bigint | Compuesta; ambas columnas son FK, sin Identity |
| 16 | Auditoria | Id_Auditoria | bigint | GENERATED ALWAYS AS IDENTITY |

## 1. Rol

Tabla SQL: `afrisan."Rol"`.

| Columna | Tipo exacto | Clave | Nullable | Definición adicional del script |
| --- | --- | --- | --- | --- |
| `Id_Rol` | `smallint` | PK | No | `GENERATED ALWAYS AS IDENTITY` |
| `Nombre_Rol` | `varchar(40)` | UNIQUE | No | `UNIQUE CHECK ("Nombre_Rol" IN ('Administrador','Jefatura','Bodega','Tecnico'))` |

### Relaciones FK

No tiene claves foráneas.

## 2. Usuario

Tabla SQL: `afrisan."Usuario"`.

| Columna | Tipo exacto | Clave | Nullable | Definición adicional del script |
| --- | --- | --- | --- | --- |
| `Id_Usuario` | `integer` | PK | No | `GENERATED ALWAYS AS IDENTITY` |
| `Nombre` | `varchar(100)` | — | No | `CHECK (btrim("Nombre") <> '')` |
| `Apellido` | `varchar(100)` | — | No | `CHECK (btrim("Apellido") <> '')` |
| `Correo_Usuario` | `varchar(254)` | — | No | `CHECK (btrim("Correo_Usuario") <> '')` |
| `Clave_Usuario` | `text` | — | No | `CHECK (length("Clave_Usuario") >= 32)` |
| `Activo` | `boolean` | — | No | `DEFAULT true` |
| `Id_Rol` | `smallint` | FK | No | `REFERENCES afrisan."Rol" ON DELETE RESTRICT` |
| `FechaCreacion` | `timestamptz` | — | No | `DEFAULT now()` |
| `IntentosFallidos` | `integer` | — | No | `DEFAULT 0 CHECK ("IntentosFallidos" >= 0)` |
| `BloqueadoHasta` | `timestamptz` | — | Sí | — |
| `VersionSeguridad` | `uuid` | — | No | `DEFAULT gen_random_uuid()` |

### Relaciones FK

| Columnas locales, en orden | Tabla de destino | Columnas de destino, en orden | Relación |
| --- | --- | --- | --- |
| `Id_Rol` | `afrisan."Rol"` | `Id_Rol` | FK simple |

### Índices adicionales

```sql
CREATE UNIQUE INDEX "UX_Usuario_Correo" ON afrisan."Usuario" (lower(btrim("Correo_Usuario")));
```

## 3. Cliente

Tabla SQL: `afrisan."Cliente"`.

| Columna | Tipo exacto | Clave | Nullable | Definición adicional del script |
| --- | --- | --- | --- | --- |
| `Id_Cliente` | `integer` | PK | No | `GENERATED ALWAYS AS IDENTITY` |
| `RutCliente` | `varchar(20)` | UNIQUE | No | `UNIQUE CHECK (btrim("RutCliente") <> '')` |
| `NombreEmpresa` | `varchar(200)` | — | No | `CHECK (btrim("NombreEmpresa") <> '')` |
| `Activo` | `boolean` | — | No | `DEFAULT true` |

### Relaciones FK

No tiene claves foráneas.

## 4. Sucursales

Tabla SQL: `afrisan."Sucursales"`.

| Columna | Tipo exacto | Clave | Nullable | Definición adicional del script |
| --- | --- | --- | --- | --- |
| `Id_Sucursal` | `integer` | PK | No | `GENERATED ALWAYS AS IDENTITY` |
| `Id_Cliente` | `integer` | FK | No | `REFERENCES afrisan."Cliente" ON DELETE RESTRICT` |
| `NombreSucursal` | `varchar(200)` | — | No | `CHECK (btrim("NombreSucursal") <> '')` |
| `Direccion` | `varchar(400)` | — | Sí | — |
| `Activo` | `boolean` | — | No | `DEFAULT true` |

### Relaciones FK

| Columnas locales, en orden | Tabla de destino | Columnas de destino, en orden | Relación |
| --- | --- | --- | --- |
| `Id_Cliente` | `afrisan."Cliente"` | `Id_Cliente` | FK simple |

### Restricciones adicionales de la tabla

No son columnas nuevas. Las combinaciones UNIQUE deben existir antes de vincular las FK que las referencian.

```sql
UNIQUE ("Id_Cliente", "NombreSucursal")
```

## 5. Refrigerantes

Tabla SQL: `afrisan."Refrigerantes"`.

| Columna | Tipo exacto | Clave | Nullable | Definición adicional del script |
| --- | --- | --- | --- | --- |
| `Id_Refrigerante` | `bigint` | PK | No | `GENERATED ALWAYS AS IDENTITY` |
| `CodigoRefrigerante` | `varchar(40)` | — | No | `CHECK (btrim("CodigoRefrigerante") <> '')` |
| `NombreRefrigerante` | `varchar(150)` | — | Sí | — |
| `FactorGWP` | `numeric(12,3)` | — | Sí | `CHECK ("FactorGWP" >= 0)` |
| `FuenteGWP` | `text` | — | Sí | — |
| `MetodologiaGWP` | `varchar(150)` | — | Sí | — |
| `Activo` | `boolean` | — | No | `DEFAULT true` |

### Relaciones FK

No tiene claves foráneas.

### Restricciones adicionales de la tabla

No son columnas nuevas. Las combinaciones UNIQUE deben existir antes de vincular las FK que las referencian.

```sql
CHECK ("FactorGWP" IS NULL OR (coalesce(btrim("FuenteGWP"),'') <> '' AND coalesce(btrim("MetodologiaGWP"),'') <> ''))
```

### Índices adicionales

```sql
CREATE UNIQUE INDEX "UX_Refrigerantes_Codigo" ON afrisan."Refrigerantes" (upper(btrim("CodigoRefrigerante")));
```

## 6. Cilindros

Tabla SQL: `afrisan."Cilindros"`.

| Columna | Tipo exacto | Clave | Nullable | Definición adicional del script |
| --- | --- | --- | --- | --- |
| `Id_Cilindro` | `bigint` | PK | No | `GENERATED ALWAYS AS IDENTITY` |
| `Capacidadgr` | `bigint` | — | No | `CHECK ("Capacidadgr" > 0)` |
| `TaraGr` | `bigint` | — | No | `CHECK ("TaraGr" > 0)` |
| `Id_Refrigerante` | `bigint` | FK | No | `REFERENCES afrisan."Refrigerantes" ON DELETE RESTRICT` |
| `CilindroQR` | `varchar(120)` | UNIQUE | No | `UNIQUE CHECK (btrim("CilindroQR") <> '')` |
| `NumeroSerie` | `varchar(120)` | UNIQUE | Sí | `UNIQUE` |
| `Estado` | `varchar(40)` | — | No | `DEFAULT 'Disponible' CHECK ("Estado" IN ('Disponible','Despachado','RetornadoConDescalce','Chatarra','DisposicionFinalCertificada','Inactivo'))` |
| `FechaRegistro` | `timestamptz` | — | No | `DEFAULT now()` |

### Relaciones FK

| Columnas locales, en orden | Tabla de destino | Columnas de destino, en orden | Relación |
| --- | --- | --- | --- |
| `Id_Refrigerante` | `afrisan."Refrigerantes"` | `Id_Refrigerante` | FK simple |

### Restricciones adicionales de la tabla

No son columnas nuevas. Las combinaciones UNIQUE deben existir antes de vincular las FK que las referencian.

```sql
UNIQUE ("Id_Cilindro", "Id_Refrigerante")
```

## 7. Equipos

Tabla SQL: `afrisan."Equipos"`.

| Columna | Tipo exacto | Clave | Nullable | Definición adicional del script |
| --- | --- | --- | --- | --- |
| `Id_Equipo` | `bigint` | PK | No | `GENERATED ALWAYS AS IDENTITY` |
| `Id_Sucursal` | `integer` | FK | No | `REFERENCES afrisan."Sucursales" ON DELETE RESTRICT` |
| `CodigoEquipo` | `varchar(80)` | — | No | `CHECK (btrim("CodigoEquipo") <> '')` |
| `NombreEquipo` | `varchar(200)` | — | No | — |
| `CapacidadNominalGr` | `bigint` | — | No | `CHECK ("CapacidadNominalGr" > 0)` |
| `Id_Refrigerante` | `bigint` | FK | No | `REFERENCES afrisan."Refrigerantes" ON DELETE RESTRICT` |
| `Activo` | `boolean` | — | No | `DEFAULT true` |

### Relaciones FK

| Columnas locales, en orden | Tabla de destino | Columnas de destino, en orden | Relación |
| --- | --- | --- | --- |
| `Id_Sucursal` | `afrisan."Sucursales"` | `Id_Sucursal` | FK simple |
| `Id_Refrigerante` | `afrisan."Refrigerantes"` | `Id_Refrigerante` | FK simple |

### Restricciones adicionales de la tabla

No son columnas nuevas. Las combinaciones UNIQUE deben existir antes de vincular las FK que las referencian.

```sql
UNIQUE ("Id_Sucursal", "CodigoEquipo"),
UNIQUE ("Id_Equipo", "Id_Sucursal", "Id_Refrigerante")
```

## 8. CausasFalla

Tabla SQL: `afrisan."CausasFalla"`.

| Columna | Tipo exacto | Clave | Nullable | Definición adicional del script |
| --- | --- | --- | --- | --- |
| `Id_CausaFalla` | `integer` | PK | No | `GENERATED ALWAYS AS IDENTITY` |
| `Codigo` | `varchar(40)` | UNIQUE | No | `UNIQUE` |
| `Descripcion` | `varchar(250)` | — | No | — |
| `Activo` | `boolean` | — | No | `DEFAULT true` |

### Relaciones FK

No tiene claves foráneas.

## 9. OrdenTrabajo

Tabla SQL: `afrisan."OrdenTrabajo"`.

| Columna | Tipo exacto | Clave | Nullable | Definición adicional del script |
| --- | --- | --- | --- | --- |
| `Id_OrdenTrabajo` | `bigint` | PK | No | `GENERATED ALWAYS AS IDENTITY` |
| `CodigoOT` | `varchar(100)` | UNIQUE | No | `UNIQUE CHECK (btrim("CodigoOT") <> '')` |
| `Origen` | `varchar(30)` | — | No | `DEFAULT 'Local' CHECK ("Origen" IN ('Local','Defontana'))` |
| `CodigoExterno` | `varchar(100)` | — | Sí | — |
| `Id_Autorizador` | `integer` | FK | Sí | `REFERENCES afrisan."Usuario" ON DELETE RESTRICT` |
| `Id_Sucursal` | `integer` | FK | No | `REFERENCES afrisan."Sucursales" ON DELETE RESTRICT` |
| `Descripcion` | `text` | — | Sí | — |
| `Estado` | `varchar(20)` | — | No | `DEFAULT 'Abierta' CHECK ("Estado" IN ('Abierta','EnCurso','Cerrada','Anulada'))` |
| `FechaCreacion` | `timestamptz` | — | No | `DEFAULT now()` |
| `FechaSalida` | `timestamptz` | — | Sí | — |
| `FechaFinalizada` | `timestamptz` | — | Sí | — |

### Relaciones FK

| Columnas locales, en orden | Tabla de destino | Columnas de destino, en orden | Relación |
| --- | --- | --- | --- |
| `Id_Autorizador` | `afrisan."Usuario"` | `Id_Usuario` | FK simple |
| `Id_Sucursal` | `afrisan."Sucursales"` | `Id_Sucursal` | FK simple |

### Restricciones adicionales de la tabla

No son columnas nuevas. Las combinaciones UNIQUE deben existir antes de vincular las FK que las referencian.

```sql
CHECK ("Origen" <> 'Defontana' OR coalesce(btrim("CodigoExterno"),'') <> ''),
CHECK (("Estado" IN ('Cerrada','Anulada')) = ("FechaFinalizada" IS NOT NULL)),
CHECK ("FechaSalida" IS NULL OR "FechaSalida" >= "FechaCreacion"),
CHECK ("FechaFinalizada" IS NULL OR "FechaFinalizada" >= "FechaCreacion"),
UNIQUE ("Id_OrdenTrabajo", "Id_Sucursal")
```

### Índices adicionales

```sql
CREATE UNIQUE INDEX "UX_OT_Externa" ON afrisan."OrdenTrabajo" ("Origen","CodigoExterno") WHERE "CodigoExterno" IS NOT NULL;
```

## 10. Movimientos

Tabla SQL: `afrisan."Movimientos"`.

| Columna | Tipo exacto | Clave | Nullable | Definición adicional del script |
| --- | --- | --- | --- | --- |
| `Id_Movimiento` | `bigint` | PK | No | `GENERATED ALWAYS AS IDENTITY` |
| `Id_Operacion` | `uuid` | UNIQUE | No | `UNIQUE DEFAULT gen_random_uuid()` |
| `Id_OrdenTrabajo` | `bigint` | FK | No | `REFERENCES afrisan."OrdenTrabajo" ON DELETE RESTRICT` |
| `Id_Cilindro` | `bigint` | FK | No | `REFERENCES afrisan."Cilindros" ON DELETE RESTRICT` |
| `Id_Usuario` | `integer` | FK | No | `REFERENCES afrisan."Usuario" ON DELETE RESTRICT` |
| `Id_Tecnico` | `integer` | FK | No | `REFERENCES afrisan."Usuario" ON DELETE RESTRICT` |
| `Salida` | `boolean` | — | No | — |
| `PesoGrBruto` | `bigint` | — | No | `CHECK ("PesoGrBruto" >= 0)` |
| `FechaMovimiento` | `timestamptz` | — | No | `DEFAULT now()` |
| `FotoPesaje` | `text` | — | Sí | — |
| `Id_Despacho` | `bigint` | FK | Sí | — |
| `CustodiaAbierta` | `boolean` | — | No | `DEFAULT false` |
| `Observaciones` | `text` | — | Sí | — |

### Relaciones FK

| Columnas locales, en orden | Tabla de destino | Columnas de destino, en orden | Relación |
| --- | --- | --- | --- |
| `Id_OrdenTrabajo` | `afrisan."OrdenTrabajo"` | `Id_OrdenTrabajo` | FK simple |
| `Id_Cilindro` | `afrisan."Cilindros"` | `Id_Cilindro` | FK simple |
| `Id_Usuario` | `afrisan."Usuario"` | `Id_Usuario` | FK simple |
| `Id_Tecnico` | `afrisan."Usuario"` | `Id_Usuario` | FK simple |
| `Id_Despacho, Id_Cilindro, Id_OrdenTrabajo, Id_Tecnico` | `afrisan."Movimientos"` | `Id_Movimiento, Id_Cilindro, Id_OrdenTrabajo, Id_Tecnico` | Una sola FK compuesta |

### Restricciones adicionales de la tabla

No son columnas nuevas. Las combinaciones UNIQUE deben existir antes de vincular las FK que las referencian.

```sql
UNIQUE ("Id_Movimiento", "Id_Cilindro", "Id_OrdenTrabajo", "Id_Tecnico"),
FOREIGN KEY ("Id_Despacho", "Id_Cilindro", "Id_OrdenTrabajo", "Id_Tecnico") REFERENCES afrisan."Movimientos" ("Id_Movimiento", "Id_Cilindro", "Id_OrdenTrabajo", "Id_Tecnico") ON DELETE RESTRICT,
CHECK (("Salida" AND "Id_Despacho" IS NULL) OR (NOT "Salida" AND "Id_Despacho" IS NOT NULL)),
CHECK (NOT "CustodiaAbierta" OR "Salida"),
CHECK ("Id_Despacho" IS NULL OR "Id_Despacho" <> "Id_Movimiento")
```

### Índices adicionales

```sql
CREATE UNIQUE INDEX "UX_Movimiento_Retorno" ON afrisan."Movimientos" ("Id_Despacho") WHERE "Id_Despacho" IS NOT NULL;
CREATE UNIQUE INDEX "UX_Movimiento_Custodia" ON afrisan."Movimientos" ("Id_Cilindro") WHERE "CustodiaAbierta";
CREATE INDEX "IX_Movimiento_OT" ON afrisan."Movimientos" ("Id_OrdenTrabajo");
CREATE INDEX "IX_Movimiento_Historial" ON afrisan."Movimientos" ("Id_Cilindro", "FechaMovimiento");
```

## 11. Intervencion

Tabla SQL: `afrisan."Intervencion"`.

| Columna | Tipo exacto | Clave | Nullable | Definición adicional del script |
| --- | --- | --- | --- | --- |
| `Id_Intervencion` | `bigint` | PK | No | `GENERATED ALWAYS AS IDENTITY` |
| `Id_Operacion` | `uuid` | UNIQUE | No | `UNIQUE DEFAULT gen_random_uuid()` |
| `Id_OrdenTrabajo` | `bigint` | FK | No | — |
| `Id_Sucursal` | `integer` | FK | No | — |
| `Id_Equipo` | `bigint` | FK | No | — |
| `Id_Cilindro` | `bigint` | FK | No | — |
| `Id_Refrigerante` | `bigint` | FK | No | — |
| `Id_Despacho` | `bigint` | FK | No | — |
| `Id_Tecnico` | `integer` | FK | No | `REFERENCES afrisan."Usuario" ON DELETE RESTRICT` |
| `Id_CausaFalla` | `integer` | FK | No | `REFERENCES afrisan."CausasFalla" ON DELETE RESTRICT` |
| `FechaIntervencion` | `timestamptz` | — | No | — |
| `GrUtilizados` | `bigint` | — | No | `CHECK ("GrUtilizados" > 0)` |
| `PesoRestanteGrBruto` | `bigint` | — | Sí | `CHECK ("PesoRestanteGrBruto" >= 0)` |
| `FotoBalanza` | `text` | — | Sí | — |
| `CapacidadNominalGrAplicada` | `bigint` | — | No | `CHECK ("CapacidadNominalGrAplicada" > 0)` |
| `EstadoSemaforo` | `varchar(20)` | — | No | `DEFAULT 'Pendiente' CHECK ("EstadoSemaforo" IN ('Pendiente','Verde','Amarillo','Rojo'))` |
| `PlanCorreccion` | `text` | — | Sí | — |
| `Estado` | `varchar(20)` | — | No | `DEFAULT 'Registrada' CHECK ("Estado" IN ('Registrada','Revisada','Cerrada'))` |
| `Id_Revisor` | `integer` | FK | Sí | `REFERENCES afrisan."Usuario" ON DELETE RESTRICT` |
| `FechaRevision` | `timestamptz` | — | Sí | — |
| `FechaCierre` | `timestamptz` | — | Sí | — |
| `GrEmitidos` | `bigint` | — | Sí | `CHECK ("GrEmitidos" >= 0)` |
| `FactorGWPAplicado` | `numeric(12,3)` | — | Sí | `CHECK ("FactorGWPAplicado" >= 0)` |
| `FuenteGWPAplicada` | `text` | — | Sí | — |
| `MetodologiaGWPAplicada` | `varchar(150)` | — | Sí | — |
| `EmisionesCO2eTon` | `numeric(24,6)` | — | Sí | `GENERATED ALWAYS AS ("GrEmitidos"::numeric * "FactorGWPAplicado" / 1000000) STORED` |
| `Dispositivo` | `varchar(150)` | — | Sí | — |
| `FechaCreacionLocal` | `timestamptz` | — | Sí | — |
| `FechaRecepcion` | `timestamptz` | — | No | `DEFAULT now()` |
| `Observaciones` | `text` | — | Sí | — |

**EmisionesCO2eTon** es una columna calculada almacenada, no un dato que el usuario ingresa. **CapacidadNominalGrAplicada** la copia el trigger desde Equipos al insertar. Si creas solo columnas, esa copia no ocurrirá automáticamente.

### Relaciones FK

| Columnas locales, en orden | Tabla de destino | Columnas de destino, en orden | Relación |
| --- | --- | --- | --- |
| `Id_Tecnico` | `afrisan."Usuario"` | `Id_Usuario` | FK simple |
| `Id_CausaFalla` | `afrisan."CausasFalla"` | `Id_CausaFalla` | FK simple |
| `Id_Revisor` | `afrisan."Usuario"` | `Id_Usuario` | FK simple |
| `Id_OrdenTrabajo, Id_Sucursal` | `afrisan."OrdenTrabajo"` | `Id_OrdenTrabajo, Id_Sucursal` | Una sola FK compuesta |
| `Id_Equipo, Id_Sucursal, Id_Refrigerante` | `afrisan."Equipos"` | `Id_Equipo, Id_Sucursal, Id_Refrigerante` | Una sola FK compuesta |
| `Id_Cilindro, Id_Refrigerante` | `afrisan."Cilindros"` | `Id_Cilindro, Id_Refrigerante` | Una sola FK compuesta |
| `Id_Despacho, Id_Cilindro, Id_OrdenTrabajo, Id_Tecnico` | `afrisan."Movimientos"` | `Id_Movimiento, Id_Cilindro, Id_OrdenTrabajo, Id_Tecnico` | Una sola FK compuesta |

### Restricciones adicionales de la tabla

No son columnas nuevas. Las combinaciones UNIQUE deben existir antes de vincular las FK que las referencian.

```sql
FOREIGN KEY ("Id_OrdenTrabajo", "Id_Sucursal") REFERENCES afrisan."OrdenTrabajo" ("Id_OrdenTrabajo", "Id_Sucursal") ON DELETE RESTRICT,
FOREIGN KEY ("Id_Equipo", "Id_Sucursal", "Id_Refrigerante") REFERENCES afrisan."Equipos" ("Id_Equipo", "Id_Sucursal", "Id_Refrigerante") ON DELETE RESTRICT,
FOREIGN KEY ("Id_Cilindro", "Id_Refrigerante") REFERENCES afrisan."Cilindros" ("Id_Cilindro", "Id_Refrigerante") ON DELETE RESTRICT,
FOREIGN KEY ("Id_Despacho", "Id_Cilindro", "Id_OrdenTrabajo", "Id_Tecnico") REFERENCES afrisan."Movimientos" ("Id_Movimiento", "Id_Cilindro", "Id_OrdenTrabajo", "Id_Tecnico") ON DELETE RESTRICT,
CHECK ("Estado" <> 'Cerrada' OR ("FechaCierre" IS NOT NULL AND "PesoRestanteGrBruto" IS NOT NULL AND coalesce(btrim("FotoBalanza"),'') <> '')),
CHECK ("EstadoSemaforo" <> 'Rojo' OR coalesce(btrim("PlanCorreccion"),'') <> ''),
CHECK (("Id_Revisor" IS NULL) = ("FechaRevision" IS NULL)),
CHECK ("Estado" <> 'Revisada' OR "Id_Revisor" IS NOT NULL),
CHECK ("FechaCierre" IS NULL OR "FechaCierre" >= "FechaIntervencion"),
CHECK ("FactorGWPAplicado" IS NULL OR (coalesce(btrim("FuenteGWPAplicada"),'') <> '' AND coalesce(btrim("MetodologiaGWPAplicada"),'') <> '')),
UNIQUE ("Id_Intervencion", "Id_Cilindro", "Id_Refrigerante", "GrUtilizados")
```

### Índices adicionales

```sql
CREATE INDEX "IX_Intervencion_EquipoFecha" ON afrisan."Intervencion" ("Id_Equipo", "FechaIntervencion");
CREATE INDEX "IX_Intervencion_SucursalFecha" ON afrisan."Intervencion" ("Id_Sucursal", "FechaIntervencion");
CREATE INDEX "IX_Intervencion_Despacho" ON afrisan."Intervencion" ("Id_Despacho");
```

## 12. PreciosRefrigerantes

Tabla SQL: `afrisan."PreciosRefrigerantes"`.

| Columna | Tipo exacto | Clave | Nullable | Definición adicional del script |
| --- | --- | --- | --- | --- |
| `Id_Precio` | `bigint` | PK | No | `GENERATED ALWAYS AS IDENTITY` |
| `Id_Refrigerante` | `bigint` | FK | No | `REFERENCES afrisan."Refrigerantes" ON DELETE RESTRICT` |
| `PrecioGrRefrigerante` | `numeric(18,8)` | — | No | `CHECK ("PrecioGrRefrigerante" >= 0)` |
| `Moneda` | `varchar(3)` | — | No | `DEFAULT 'CLP' CHECK ("Moneda" ~ '^[A-Z]{3}$')` |
| `VigenteDesde` | `timestamptz` | — | No | — |

### Relaciones FK

| Columnas locales, en orden | Tabla de destino | Columnas de destino, en orden | Relación |
| --- | --- | --- | --- |
| `Id_Refrigerante` | `afrisan."Refrigerantes"` | `Id_Refrigerante` | FK simple |

### Restricciones adicionales de la tabla

No son columnas nuevas. Las combinaciones UNIQUE deben existir antes de vincular las FK que las referencian.

```sql
UNIQUE ("Id_Refrigerante", "Moneda", "VigenteDesde")
```

## 13. Costos

Tabla SQL: `afrisan."Costos"`.

| Columna | Tipo exacto | Clave | Nullable | Definición adicional del script |
| --- | --- | --- | --- | --- |
| `Id_Costos` | `bigint` | PK | No | `GENERATED ALWAYS AS IDENTITY` |
| `Id_Intervencion` | `bigint` | FK + UNIQUE | No | `UNIQUE` |
| `Id_Refrigerante` | `bigint` | FK | No | — |
| `Id_Cilindro` | `bigint` | FK | No | — |
| `GrUtilizados` | `bigint` | FK | No | `CHECK ("GrUtilizados" > 0)` |
| `PrecioGrRefrigerante` | `numeric(18,8)` | — | No | `CHECK ("PrecioGrRefrigerante" >= 0)` |
| `PrecioCilindro` | `numeric(18,2)` | — | No | `DEFAULT 0 CHECK ("PrecioCilindro" >= 0)` |
| `Moneda` | `varchar(3)` | — | No | `DEFAULT 'CLP' CHECK ("Moneda" ~ '^[A-Z]{3}$')` |
| `FechaValoracion` | `timestamptz` | — | No | `DEFAULT now()` |
| `PrecioTotal` | `numeric(24,4)` | — | Sí | `GENERATED ALWAYS AS ("GrUtilizados"::numeric * "PrecioGrRefrigerante" + "PrecioCilindro") STORED` |

**PrecioTotal** es una columna calculada almacenada, no un total ingresado libremente. `PrecioCilindro` es un cargo adicional aplicado, cero por defecto; no se repite el precio de compra del envase en cada trabajo.

### Relaciones FK

| Columnas locales, en orden | Tabla de destino | Columnas de destino, en orden | Relación |
| --- | --- | --- | --- |
| `Id_Intervencion, Id_Cilindro, Id_Refrigerante, GrUtilizados` | `afrisan."Intervencion"` | `Id_Intervencion, Id_Cilindro, Id_Refrigerante, GrUtilizados` | Una sola FK compuesta |

### Restricciones adicionales de la tabla

No son columnas nuevas. Las combinaciones UNIQUE deben existir antes de vincular las FK que las referencian.

```sql
FOREIGN KEY ("Id_Intervencion", "Id_Cilindro", "Id_Refrigerante", "GrUtilizados") REFERENCES afrisan."Intervencion" ("Id_Intervencion", "Id_Cilindro", "Id_Refrigerante", "GrUtilizados") ON DELETE RESTRICT
```

## 14. CertificadosDisposicion

Tabla SQL: `afrisan."CertificadosDisposicion"`.

| Columna | Tipo exacto | Clave | Nullable | Definición adicional del script |
| --- | --- | --- | --- | --- |
| `Id_Certificado` | `bigint` | PK | No | `GENERATED ALWAYS AS IDENTITY` |
| `Folio` | `varchar(120)` | — | No | — |
| `EmpresaEmisora` | `varchar(150)` | — | No | `DEFAULT 'Regener'` |
| `FechaEntrega` | `date` | — | No | — |
| `FechaEmision` | `date` | — | No | — |
| `Documento` | `text` | — | No | `CHECK (btrim("Documento") <> '')` |
| `Id_Usuario` | `integer` | FK | No | `REFERENCES afrisan."Usuario" ON DELETE RESTRICT` |
| `FechaRegistro` | `timestamptz` | — | No | `DEFAULT now()` |

### Relaciones FK

| Columnas locales, en orden | Tabla de destino | Columnas de destino, en orden | Relación |
| --- | --- | --- | --- |
| `Id_Usuario` | `afrisan."Usuario"` | `Id_Usuario` | FK simple |

### Restricciones adicionales de la tabla

No son columnas nuevas. Las combinaciones UNIQUE deben existir antes de vincular las FK que las referencian.

```sql
UNIQUE ("EmpresaEmisora", "Folio"),
CHECK ("FechaEmision" >= "FechaEntrega")
```

## 15. CertificadosCilindros

Tabla SQL: `afrisan."CertificadosCilindros"`.

| Columna | Tipo exacto | Clave | Nullable | Definición adicional del script |
| --- | --- | --- | --- | --- |
| `Id_Certificado` | `bigint` | PK + FK | No | `REFERENCES afrisan."CertificadosDisposicion" ON DELETE RESTRICT` |
| `Id_Cilindro` | `bigint` | PK + FK + UNIQUE | No | `UNIQUE REFERENCES afrisan."Cilindros" ON DELETE RESTRICT` |

### Relaciones FK

| Columnas locales, en orden | Tabla de destino | Columnas de destino, en orden | Relación |
| --- | --- | --- | --- |
| `Id_Certificado` | `afrisan."CertificadosDisposicion"` | `Id_Certificado` | FK simple |
| `Id_Cilindro` | `afrisan."Cilindros"` | `Id_Cilindro` | FK simple |

### Restricciones adicionales de la tabla

No son columnas nuevas. Las combinaciones UNIQUE deben existir antes de vincular las FK que las referencian.

```sql
PRIMARY KEY ("Id_Certificado", "Id_Cilindro")
```

## 16. Auditoria

Tabla SQL: `afrisan."Auditoria"`.

| Columna | Tipo exacto | Clave | Nullable | Definición adicional del script |
| --- | --- | --- | --- | --- |
| `Id_Auditoria` | `bigint` | PK | No | `GENERATED ALWAYS AS IDENTITY` |
| `Fecha` | `timestamptz` | — | No | `DEFAULT now()` |
| `Id_Usuario` | `integer` | FK | Sí | `REFERENCES afrisan."Usuario" ON DELETE RESTRICT` |
| `UsuarioBD` | `text` | — | No | `DEFAULT session_user` |
| `Tabla` | `text` | — | No | — |
| `Id_Registro` | `text` | — | No | — |
| `Accion` | `varchar(10)` | — | No | `CHECK ("Accion" IN ('INSERT','UPDATE','DELETE'))` |
| `ValoresAnteriores` | `jsonb` | — | Sí | — |
| `ValoresNuevos` | `jsonb` | — | Sí | — |

### Relaciones FK

| Columnas locales, en orden | Tabla de destino | Columnas de destino, en orden | Relación |
| --- | --- | --- | --- |
| `Id_Usuario` | `afrisan."Usuario"` | `Id_Usuario` | FK simple |

### Índices adicionales

```sql
CREATE INDEX "IX_Auditoria_Registro" ON afrisan."Auditoria" ("Tabla", "Id_Registro", "Fecha");
```

## Cómo vincular las FK compuestas sin el error 42830

Una FK compuesta es **una relación de varias columnas en conjunto**, no varias FK individuales.
Las columnas del destino deben formar una PK o un UNIQUE con esa misma combinación.
No conviertas cada columna del destino en UNIQUE por separado: eso impediría repetir, por ejemplo, el cilindro en diferentes movimientos.

Crea primero estos UNIQUE (además de las PK):

| Tabla de destino | Combinación UNIQUE necesaria |
| --- | --- |
| Cilindros | Id_Cilindro, Id_Refrigerante |
| Equipos | Id_Equipo, Id_Sucursal, Id_Refrigerante |
| OrdenTrabajo | Id_OrdenTrabajo, Id_Sucursal |
| Movimientos | Id_Movimiento, Id_Cilindro, Id_OrdenTrabajo, Id_Tecnico |
| Intervencion | Id_Intervencion, Id_Cilindro, Id_Refrigerante, GrUtilizados |

Cada relación compuesta se crea respetando el orden de ambas listas. Si la interfaz que usas no permite configurarlas, usa un ALTER TABLE específico en SQL Editor; no crees FK individuales para reemplazarlas.
Las FK simples que solo indican `REFERENCES tabla` en el script apuntan a la PK de esa tabla: los nombres exactos ya se muestran en las tablas de relaciones de este documento.

## Campos calculados

No configurar estas expresiones como DEFAULT. Son columnas **GENERATED ALWAYS AS (...) STORED**:

| Tabla | Columna | Tipo | Expresión |
| --- | --- | --- | --- |
| Intervencion | EmisionesCO2eTon | numeric(24,6) | GrEmitidos × FactorGWPAplicado / 1000000 |
| Costos | PrecioTotal | numeric(24,4) | GrUtilizados × PrecioGrRefrigerante + PrecioCilindro |

El divisor de CO2e convierte gramos de refrigerante a toneladas equivalentes.
Si no se conoce la masa emitida o el GWP, las emisiones permanecen NULL. No asumir que todo el gas utilizado se emitió.
Si tu editor no permite columnas generadas, ese paso requiere SQL específico antes de dar por terminada la estructura.

## Reglas que NO quedan instaladas solo por crear las tablas

El script 03 también define:

- Validación del despacho: OT abierta, técnico activo, registrador de bodega/administración, peso entre tara y capacidad y una custodia abierta por cilindro.
- Retorno vinculado al despacho exacto, sin duplicados y con intervenciones cerradas; estado del cilindro actualizado automáticamente.
- Intervención compatible con OT, sucursal, equipo, refrigerante y técnico custodio. Consumo no superior al contenido despachado.
- Semáforo: verde <5%, amarillo 5–15%, rojo >15% o carga anterior en menos de 30 días; rojo exige plan.
- Foto de balanza y peso restante para cerrar intervención.
- Conciliación: (salida − retorno) − declarado. Conforme solo si el valor absoluto es **menor a 200 g**.
- Bloqueo del cierre de OT con cilindros pendientes, descalces o intervenciones sin cerrar.
- Certificación de cilindros clasificados como chatarra, sin custodia pendiente.
- Historial automático de auditoría, protección de pesajes y trabajos cerrados.
- RLS activado, restricciones de acceso e índices.

Estos elementos deben instalarse por separado si haces la creación manual. No vuelvas a ejecutar todo el script 03 después de crear manualmente sus tablas: se detendrá porque ya existen.
La autorización de sesión, manejo de contraseñas, Storage y sincronización offline también requieren implementación en la API/aplicación.

## Vistas: no crearlas como tablas

| Esquema | Vista | Propósito |
| --- | --- | --- |
| afrisan | BalanceCilindro | Un ciclo salida/retorno con gramos declarados, descalce y resultado. |
| afrisan | EstadoCilindros | Último peso registrado y contenido estimado por cilindro. |
| afrisan_bi | ConsumoIntervenciones | Consumos, costos y emisiones por intervención para BI. |
| afrisan_bi | Conciliacion | Balance para BI sin datos de autenticación. |
| afrisan_bi | SucursalesCriticasMes | Conteo mensual de intervenciones críticas por sucursal. |

## Recordatorios para evitar diferencias con el diseño

- No agregar Id_Cilindro ni Id_Costos a OrdenTrabajo: una OT admite varios cilindros y trabajos.
- Sucursales.Id_Cliente apunta a Cliente.Id_Cliente; no al revés.
- Usuario.Id_Rol es int2 igual que Rol.Id_Rol. No hay tabla Permisos.
- CertificadosCilindros tiene PK de **dos columnas**, ambas sin Identity.
- UUID de operación no es PK: es una clave UNIQUE adicional para reintentos.
- Id_Usuario registra quién ingresó el pesaje; Id_Tecnico identifica al custodio.
- No usar el tamaño máximo de otra PK para decidir el tipo de FK: cada una debe coincidir con su propia columna de destino.
- Los DEFAULT solo definen valores de futuras filas; no pueblan tablas.
- El script 03 no carga siquiera los cuatro roles: todas las tablas comienzan vacías.
- Esquema personalizado y RLS deben mantenerse según la arquitectura de acceso por API; no otorgar acceso público a Usuario ni a hashes.
