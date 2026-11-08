-- AFRISAN | Modelo nuevo basado en el MER de Dante Moya y HU-01 a HU-10.
-- EJECUTAR COMPLETO en Supabase > SQL Editor con el propietario de la base.
-- No modifica public, auth, Storage ni las tablas creadas anteriormente.
-- Crea afrisan y afrisan_bi. Si ya existen, se detiene sin reemplazarlos.
-- NO es una migración del proyecto EF anterior: la aplicación nueva deberá
-- mapear estos nombres y usar conexión PostgreSQL desde su API.
-- Pesos: gramos enteros (1 kg = 1000 g). Dinero: NUMERIC, nunca FLOAT.
-- La API autentica, comprueba roles y escribe transacciones. No usar desde
-- el navegador claves de servicio ni conexión PostgreSQL.
BEGIN;
CREATE SCHEMA afrisan;
CREATE SCHEMA afrisan_bi;
REVOKE ALL ON SCHEMA afrisan, afrisan_bi FROM PUBLIC, anon, authenticated;

CREATE TABLE afrisan."Rol" (
  "Id_Rol" smallint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Nombre_Rol" varchar(40) NOT NULL UNIQUE
    CHECK ("Nombre_Rol" IN ('Administrador','Jefatura','Bodega','Tecnico'))
);
INSERT INTO afrisan."Rol" ("Nombre_Rol") VALUES
  ('Administrador'), ('Jefatura'), ('Bodega'), ('Tecnico');

CREATE TABLE afrisan."Usuario" (
  "Id_Usuario" integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Nombre" varchar(100) NOT NULL CHECK (btrim("Nombre") <> ''),
  "Apellido" varchar(100) NOT NULL CHECK (btrim("Apellido") <> ''),
  "Correo_Usuario" varchar(254) NOT NULL CHECK (btrim("Correo_Usuario") <> ''),
  "Clave_Usuario" text NOT NULL CHECK (length("Clave_Usuario") >= 32),
  "Activo" boolean NOT NULL DEFAULT true,
  "Id_Rol" smallint NOT NULL REFERENCES afrisan."Rol" ON DELETE RESTRICT,
  "FechaCreacion" timestamptz NOT NULL DEFAULT now(),
  "IntentosFallidos" integer NOT NULL DEFAULT 0 CHECK ("IntentosFallidos" >= 0),
  "BloqueadoHasta" timestamptz,
  "VersionSeguridad" uuid NOT NULL DEFAULT gen_random_uuid()
);
CREATE UNIQUE INDEX "UX_Usuario_Correo" ON afrisan."Usuario" (lower(btrim("Correo_Usuario")));
COMMENT ON COLUMN afrisan."Usuario"."Clave_Usuario" IS
  'SOLO hash seguro creado/verificado por la API (por ejemplo PasswordHasher). La longitud no valida el algoritmo. Nunca texto plano, SHA/MD5 simples ni contraseña en SQL.';
COMMENT ON COLUMN afrisan."Usuario"."VersionSeguridad" IS
  'La API debe renovarla al cambiar clave/rol o revocar sesiones, y validarla al autorizar tokens.';

CREATE TABLE afrisan."Cliente" (
  "Id_Cliente" integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "RutCliente" varchar(20) NOT NULL UNIQUE CHECK (btrim("RutCliente") <> ''),
  "NombreEmpresa" varchar(200) NOT NULL CHECK (btrim("NombreEmpresa") <> ''),
  "Activo" boolean NOT NULL DEFAULT true
);
CREATE TABLE afrisan."Sucursales" (
  "Id_Sucursal" integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_Cliente" integer NOT NULL REFERENCES afrisan."Cliente" ON DELETE RESTRICT,
  "NombreSucursal" varchar(200) NOT NULL CHECK (btrim("NombreSucursal") <> ''),
  "Direccion" varchar(400),
  "Activo" boolean NOT NULL DEFAULT true,
  UNIQUE ("Id_Cliente", "NombreSucursal")
);
CREATE TABLE afrisan."Refrigerantes" (
  "Id_Refrigerante" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "CodigoRefrigerante" varchar(40) NOT NULL CHECK (btrim("CodigoRefrigerante") <> ''),
  "NombreRefrigerante" varchar(150),
  "FactorGWP" numeric(12,3) CHECK ("FactorGWP" >= 0),
  "FuenteGWP" text,
  "MetodologiaGWP" varchar(150),
  "Activo" boolean NOT NULL DEFAULT true,
  CHECK ("FactorGWP" IS NULL OR
    (coalesce(btrim("FuenteGWP"),'') <> '' AND coalesce(btrim("MetodologiaGWP"),'') <> ''))
);
CREATE UNIQUE INDEX "UX_Refrigerantes_Codigo" ON afrisan."Refrigerantes" (upper(btrim("CodigoRefrigerante")));

CREATE TABLE afrisan."Cilindros" (
  "Id_Cilindro" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Capacidadgr" bigint NOT NULL CHECK ("Capacidadgr" > 0),
  "TaraGr" bigint NOT NULL CHECK ("TaraGr" > 0),
  "Id_Refrigerante" bigint NOT NULL REFERENCES afrisan."Refrigerantes" ON DELETE RESTRICT,
  "CilindroQR" varchar(120) NOT NULL UNIQUE CHECK (btrim("CilindroQR") <> ''),
  "NumeroSerie" varchar(120) UNIQUE,
  "Estado" varchar(40) NOT NULL DEFAULT 'Disponible'
    CHECK ("Estado" IN ('Disponible','Despachado','RetornadoConDescalce','Chatarra','DisposicionFinalCertificada','Inactivo')),
  "FechaRegistro" timestamptz NOT NULL DEFAULT now(),
  UNIQUE ("Id_Cilindro", "Id_Refrigerante")
);
COMMENT ON COLUMN afrisan."Cilindros"."Capacidadgr" IS 'Capacidad máxima de refrigerante, en gramos. No incluye tara.';
COMMENT ON COLUMN afrisan."Cilindros"."CilindroQR" IS 'Código único estable. La aplicación genera/descarga su imagen QR.';

CREATE TABLE afrisan."Equipos" (
  "Id_Equipo" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_Sucursal" integer NOT NULL REFERENCES afrisan."Sucursales" ON DELETE RESTRICT,
  "CodigoEquipo" varchar(80) NOT NULL CHECK (btrim("CodigoEquipo") <> ''),
  "NombreEquipo" varchar(200) NOT NULL,
  "CapacidadNominalGr" bigint NOT NULL CHECK ("CapacidadNominalGr" > 0),
  "Id_Refrigerante" bigint NOT NULL REFERENCES afrisan."Refrigerantes" ON DELETE RESTRICT,
  "Activo" boolean NOT NULL DEFAULT true,
  UNIQUE ("Id_Sucursal", "CodigoEquipo"),
  UNIQUE ("Id_Equipo", "Id_Sucursal", "Id_Refrigerante")
);
CREATE TABLE afrisan."CausasFalla" (
  "Id_CausaFalla" integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Codigo" varchar(40) NOT NULL UNIQUE,
  "Descripcion" varchar(250) NOT NULL,
  "Activo" boolean NOT NULL DEFAULT true
);
-- No se inventa un catálogo de fallas: cargar el catálogo que apruebe el cliente.

CREATE TABLE afrisan."OrdenTrabajo" (
  "Id_OrdenTrabajo" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "CodigoOT" varchar(100) NOT NULL UNIQUE CHECK (btrim("CodigoOT") <> ''),
  "Origen" varchar(30) NOT NULL DEFAULT 'Local' CHECK ("Origen" IN ('Local','Defontana')),
  "CodigoExterno" varchar(100),
  "Id_Autorizador" integer REFERENCES afrisan."Usuario" ON DELETE RESTRICT,
  "Id_Sucursal" integer NOT NULL REFERENCES afrisan."Sucursales" ON DELETE RESTRICT,
  "Descripcion" text,
  "Estado" varchar(20) NOT NULL DEFAULT 'Abierta'
    CHECK ("Estado" IN ('Abierta','EnCurso','Cerrada','Anulada')),
  "FechaCreacion" timestamptz NOT NULL DEFAULT now(),
  "FechaSalida" timestamptz,
  "FechaFinalizada" timestamptz,
  CHECK ("Origen" <> 'Defontana' OR coalesce(btrim("CodigoExterno"),'') <> ''),
  CHECK (("Estado" IN ('Cerrada','Anulada')) = ("FechaFinalizada" IS NOT NULL)),
  CHECK ("FechaSalida" IS NULL OR "FechaSalida" >= "FechaCreacion"),
  CHECK ("FechaFinalizada" IS NULL OR "FechaFinalizada" >= "FechaCreacion"),
  UNIQUE ("Id_OrdenTrabajo", "Id_Sucursal")
);
CREATE UNIQUE INDEX "UX_OT_Externa" ON afrisan."OrdenTrabajo" ("Origen","CodigoExterno") WHERE "CodigoExterno" IS NOT NULL;
COMMENT ON TABLE afrisan."OrdenTrabajo" IS
  'Una OT de una sucursal agrupa múltiples cilindros mediante Movimientos y múltiples Intervenciones. No contiene un único Id_Cilindro ni Id_Costos.';

CREATE TABLE afrisan."Movimientos" (
  "Id_Movimiento" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_Operacion" uuid NOT NULL UNIQUE DEFAULT gen_random_uuid(),
  "Id_OrdenTrabajo" bigint NOT NULL REFERENCES afrisan."OrdenTrabajo" ON DELETE RESTRICT,
  "Id_Cilindro" bigint NOT NULL REFERENCES afrisan."Cilindros" ON DELETE RESTRICT,
  "Id_Usuario" integer NOT NULL REFERENCES afrisan."Usuario" ON DELETE RESTRICT,
  "Id_Tecnico" integer NOT NULL REFERENCES afrisan."Usuario" ON DELETE RESTRICT,
  "Salida" boolean NOT NULL,
  "PesoGrBruto" bigint NOT NULL CHECK ("PesoGrBruto" >= 0),
  "FechaMovimiento" timestamptz NOT NULL DEFAULT now(),
  "FotoPesaje" text,
  "Id_Despacho" bigint,
  "CustodiaAbierta" boolean NOT NULL DEFAULT false,
  "Observaciones" text,
  UNIQUE ("Id_Movimiento", "Id_Cilindro", "Id_OrdenTrabajo", "Id_Tecnico"),
  FOREIGN KEY ("Id_Despacho", "Id_Cilindro", "Id_OrdenTrabajo", "Id_Tecnico")
    REFERENCES afrisan."Movimientos" ("Id_Movimiento", "Id_Cilindro", "Id_OrdenTrabajo", "Id_Tecnico") ON DELETE RESTRICT,
  CHECK (("Salida" AND "Id_Despacho" IS NULL) OR (NOT "Salida" AND "Id_Despacho" IS NOT NULL)),
  CHECK (NOT "CustodiaAbierta" OR "Salida"),
  CHECK ("Id_Despacho" IS NULL OR "Id_Despacho" <> "Id_Movimiento")
);
CREATE UNIQUE INDEX "UX_Movimiento_Retorno" ON afrisan."Movimientos" ("Id_Despacho") WHERE "Id_Despacho" IS NOT NULL;
CREATE UNIQUE INDEX "UX_Movimiento_Custodia" ON afrisan."Movimientos" ("Id_Cilindro") WHERE "CustodiaAbierta";
CREATE INDEX "IX_Movimiento_OT" ON afrisan."Movimientos" ("Id_OrdenTrabajo");
CREATE INDEX "IX_Movimiento_Historial" ON afrisan."Movimientos" ("Id_Cilindro", "FechaMovimiento");
COMMENT ON COLUMN afrisan."Movimientos"."Id_Usuario" IS 'Usuario de bodega que registra el pesaje; la API lo obtiene de la sesión.';
COMMENT ON COLUMN afrisan."Movimientos"."Id_Tecnico" IS 'Técnico que retira/devuelve el cilindro y asume la custodia.';
COMMENT ON COLUMN afrisan."Movimientos"."FotoPesaje" IS 'Ruta privada en Storage de la foto de salida o retorno. No es la evidencia de terreno.';

CREATE TABLE afrisan."Intervencion" (
  "Id_Intervencion" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_Operacion" uuid NOT NULL UNIQUE DEFAULT gen_random_uuid(),
  "Id_OrdenTrabajo" bigint NOT NULL,
  "Id_Sucursal" integer NOT NULL,
  "Id_Equipo" bigint NOT NULL,
  "Id_Cilindro" bigint NOT NULL,
  "Id_Refrigerante" bigint NOT NULL,
  "Id_Despacho" bigint NOT NULL,
  "Id_Tecnico" integer NOT NULL REFERENCES afrisan."Usuario" ON DELETE RESTRICT,
  "Id_CausaFalla" integer NOT NULL REFERENCES afrisan."CausasFalla" ON DELETE RESTRICT,
  "FechaIntervencion" timestamptz NOT NULL,
  "GrUtilizados" bigint NOT NULL CHECK ("GrUtilizados" > 0),
  "PesoRestanteGrBruto" bigint CHECK ("PesoRestanteGrBruto" >= 0),
  "FotoBalanza" text,
  "CapacidadNominalGrAplicada" bigint NOT NULL CHECK ("CapacidadNominalGrAplicada" > 0),
  "EstadoSemaforo" varchar(20) NOT NULL DEFAULT 'Pendiente'
    CHECK ("EstadoSemaforo" IN ('Pendiente','Verde','Amarillo','Rojo')),
  "PlanCorreccion" text,
  "Estado" varchar(20) NOT NULL DEFAULT 'Registrada' CHECK ("Estado" IN ('Registrada','Revisada','Cerrada')),
  "Id_Revisor" integer REFERENCES afrisan."Usuario" ON DELETE RESTRICT,
  "FechaRevision" timestamptz,
  "FechaCierre" timestamptz,
  "GrEmitidos" bigint CHECK ("GrEmitidos" >= 0),
  "FactorGWPAplicado" numeric(12,3) CHECK ("FactorGWPAplicado" >= 0),
  "FuenteGWPAplicada" text,
  "MetodologiaGWPAplicada" varchar(150),
  "EmisionesCO2eTon" numeric(24,6) GENERATED ALWAYS AS
    ("GrEmitidos"::numeric * "FactorGWPAplicado" / 1000000) STORED,
  "Dispositivo" varchar(150),
  "FechaCreacionLocal" timestamptz,
  "FechaRecepcion" timestamptz NOT NULL DEFAULT now(),
  "Observaciones" text,
  FOREIGN KEY ("Id_OrdenTrabajo", "Id_Sucursal") REFERENCES afrisan."OrdenTrabajo" ("Id_OrdenTrabajo", "Id_Sucursal") ON DELETE RESTRICT,
  FOREIGN KEY ("Id_Equipo", "Id_Sucursal", "Id_Refrigerante") REFERENCES afrisan."Equipos" ("Id_Equipo", "Id_Sucursal", "Id_Refrigerante") ON DELETE RESTRICT,
  FOREIGN KEY ("Id_Cilindro", "Id_Refrigerante") REFERENCES afrisan."Cilindros" ("Id_Cilindro", "Id_Refrigerante") ON DELETE RESTRICT,
  FOREIGN KEY ("Id_Despacho", "Id_Cilindro", "Id_OrdenTrabajo", "Id_Tecnico")
    REFERENCES afrisan."Movimientos" ("Id_Movimiento", "Id_Cilindro", "Id_OrdenTrabajo", "Id_Tecnico") ON DELETE RESTRICT,
  CHECK ("Estado" <> 'Cerrada' OR ("FechaCierre" IS NOT NULL AND "PesoRestanteGrBruto" IS NOT NULL AND coalesce(btrim("FotoBalanza"),'') <> '')),
  CHECK ("EstadoSemaforo" <> 'Rojo' OR coalesce(btrim("PlanCorreccion"),'') <> ''),
  CHECK (("Id_Revisor" IS NULL) = ("FechaRevision" IS NULL)),
  CHECK ("Estado" <> 'Revisada' OR "Id_Revisor" IS NOT NULL),
  CHECK ("FechaCierre" IS NULL OR "FechaCierre" >= "FechaIntervencion"),
  CHECK ("FactorGWPAplicado" IS NULL OR (coalesce(btrim("FuenteGWPAplicada"),'') <> '' AND coalesce(btrim("MetodologiaGWPAplicada"),'') <> '')),
  UNIQUE ("Id_Intervencion", "Id_Cilindro", "Id_Refrigerante", "GrUtilizados")
);
CREATE INDEX "IX_Intervencion_EquipoFecha" ON afrisan."Intervencion" ("Id_Equipo", "FechaIntervencion");
CREATE INDEX "IX_Intervencion_SucursalFecha" ON afrisan."Intervencion" ("Id_Sucursal", "FechaIntervencion");
CREATE INDEX "IX_Intervencion_Despacho" ON afrisan."Intervencion" ("Id_Despacho");
COMMENT ON COLUMN afrisan."Intervencion"."GrEmitidos" IS
  'Masa emitida estimada/declarada según metodología acordada. No asumir que todo lo utilizado se emitió. NULL significa desconocido.';
COMMENT ON COLUMN afrisan."Intervencion"."Id_Operacion" IS
  'El dispositivo conserva el UUID al reintentar sincronización. El servidor compara el contenido en conflictos; UNIQUE no sincroniza por sí solo.';

CREATE TABLE afrisan."PreciosRefrigerantes" (
  "Id_Precio" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_Refrigerante" bigint NOT NULL REFERENCES afrisan."Refrigerantes" ON DELETE RESTRICT,
  "PrecioGrRefrigerante" numeric(18,8) NOT NULL CHECK ("PrecioGrRefrigerante" >= 0),
  "Moneda" varchar(3) NOT NULL DEFAULT 'CLP' CHECK ("Moneda" ~ '^[A-Z]{3}$'),
  "VigenteDesde" timestamptz NOT NULL,
  UNIQUE ("Id_Refrigerante", "Moneda", "VigenteDesde")
);
CREATE TABLE afrisan."Costos" (
  "Id_Costos" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_Intervencion" bigint NOT NULL UNIQUE,
  "Id_Refrigerante" bigint NOT NULL,
  "Id_Cilindro" bigint NOT NULL,
  "GrUtilizados" bigint NOT NULL CHECK ("GrUtilizados" > 0),
  "PrecioGrRefrigerante" numeric(18,8) NOT NULL CHECK ("PrecioGrRefrigerante" >= 0),
  "PrecioCilindro" numeric(18,2) NOT NULL DEFAULT 0 CHECK ("PrecioCilindro" >= 0),
  "Moneda" varchar(3) NOT NULL DEFAULT 'CLP' CHECK ("Moneda" ~ '^[A-Z]{3}$'),
  "FechaValoracion" timestamptz NOT NULL DEFAULT now(),
  "PrecioTotal" numeric(24,4) GENERATED ALWAYS AS
    ("GrUtilizados"::numeric * "PrecioGrRefrigerante" + "PrecioCilindro") STORED,
  FOREIGN KEY ("Id_Intervencion", "Id_Cilindro", "Id_Refrigerante", "GrUtilizados")
    REFERENCES afrisan."Intervencion" ("Id_Intervencion", "Id_Cilindro", "Id_Refrigerante", "GrUtilizados") ON DELETE RESTRICT
);
COMMENT ON TABLE afrisan."Costos" IS
  'Tarifa aplicada por intervención: se conserva aunque cambie el precio del catálogo. La API selecciona la última tarifa vigente a la fecha del trabajo y moneda.';
COMMENT ON COLUMN afrisan."Costos"."PrecioCilindro" IS
  'Cargo adicional de envase aplicado SOLO si corresponde y fue acordado; por defecto cero. No repetir automáticamente el precio de compra del cilindro en cada carga.';

CREATE TABLE afrisan."CertificadosDisposicion" (
  "Id_Certificado" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Folio" varchar(120) NOT NULL,
  "EmpresaEmisora" varchar(150) NOT NULL DEFAULT 'Regener',
  "FechaEntrega" date NOT NULL,
  "FechaEmision" date NOT NULL,
  "Documento" text NOT NULL CHECK (btrim("Documento") <> ''),
  "Id_Usuario" integer NOT NULL REFERENCES afrisan."Usuario" ON DELETE RESTRICT,
  "FechaRegistro" timestamptz NOT NULL DEFAULT now(),
  UNIQUE ("EmpresaEmisora", "Folio"),
  CHECK ("FechaEmision" >= "FechaEntrega")
);
CREATE TABLE afrisan."CertificadosCilindros" (
  "Id_Certificado" bigint NOT NULL REFERENCES afrisan."CertificadosDisposicion" ON DELETE RESTRICT,
  "Id_Cilindro" bigint NOT NULL UNIQUE REFERENCES afrisan."Cilindros" ON DELETE RESTRICT,
  PRIMARY KEY ("Id_Certificado", "Id_Cilindro")
);
CREATE TABLE afrisan."Auditoria" (
  "Id_Auditoria" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Fecha" timestamptz NOT NULL DEFAULT now(),
  "Id_Usuario" integer REFERENCES afrisan."Usuario" ON DELETE RESTRICT,
  "UsuarioBD" text NOT NULL DEFAULT session_user,
  "Tabla" text NOT NULL,
  "Id_Registro" text NOT NULL,
  "Accion" varchar(10) NOT NULL CHECK ("Accion" IN ('INSERT','UPDATE','DELETE')),
  "ValoresAnteriores" jsonb,
  "ValoresNuevos" jsonb
);
CREATE INDEX "IX_Auditoria_Registro" ON afrisan."Auditoria" ("Tabla", "Id_Registro", "Fecha");
CREATE FUNCTION afrisan.proteger_auditoria() RETURNS trigger LANGUAGE plpgsql
SET search_path = pg_catalog, afrisan AS $$
BEGIN
  RAISE EXCEPTION 'El historial de auditoría no se modifica ni elimina';
END $$;
CREATE TRIGGER "TR_Auditoria_Inmutable" BEFORE UPDATE OR DELETE ON afrisan."Auditoria"
FOR EACH ROW EXECUTE FUNCTION afrisan.proteger_auditoria();

-- VALIDACIONES ENTRE TABLAS. Estas funciones se ejecutan con los permisos del
-- usuario PostgreSQL de la API; no son RPC públicas ni sustituyen autorización.
CREATE FUNCTION afrisan.validar_movimiento() RETURNS trigger LANGUAGE plpgsql
SET search_path = pg_catalog, afrisan AS $$
DECLARE ot afrisan."OrdenTrabajo"; c afrisan."Cilindros"; d afrisan."Movimientos";
BEGIN
  SELECT * INTO ot FROM afrisan."OrdenTrabajo" WHERE "Id_OrdenTrabajo" = NEW."Id_OrdenTrabajo" FOR UPDATE;
  IF NOT FOUND OR ot."Estado" NOT IN ('Abierta','EnCurso') THEN RAISE EXCEPTION 'La OT no está abierta'; END IF;
  SELECT * INTO c FROM afrisan."Cilindros" WHERE "Id_Cilindro" = NEW."Id_Cilindro" FOR UPDATE;
  IF NOT FOUND THEN RAISE EXCEPTION 'Cilindro inexistente'; END IF;
  IF NEW."FechaMovimiento" < ot."FechaCreacion" OR EXISTS (
    SELECT 1 FROM afrisan."Movimientos" WHERE "Id_Cilindro"=NEW."Id_Cilindro"
      AND "FechaMovimiento">NEW."FechaMovimiento") THEN
    RAISE EXCEPTION 'El pesaje precede a la OT o a otro movimiento ya registrado del cilindro'; END IF;
  IF NOT EXISTS (SELECT 1 FROM afrisan."Usuario" u JOIN afrisan."Rol" r USING ("Id_Rol")
    WHERE u."Id_Usuario"=NEW."Id_Usuario" AND u."Activo" AND r."Nombre_Rol" IN ('Administrador','Bodega'))
    THEN RAISE EXCEPTION 'El registrador debe ser de bodega o administrador activo'; END IF;
  IF NOT EXISTS (SELECT 1 FROM afrisan."Usuario" u JOIN afrisan."Rol" r USING ("Id_Rol")
    WHERE u."Id_Usuario"=NEW."Id_Tecnico" AND u."Activo" AND r."Nombre_Rol"='Tecnico')
    THEN RAISE EXCEPTION 'Se requiere un técnico activo para la custodia'; END IF;
  IF NEW."PesoGrBruto" < c."TaraGr" OR NEW."PesoGrBruto"::numeric > c."TaraGr"::numeric+c."Capacidadgr" THEN
    RAISE EXCEPTION 'Peso fuera del rango tara / tara más capacidad'; END IF;
  IF NEW."Salida" THEN
    IF c."Estado" <> 'Disponible' OR NEW."PesoGrBruto" <= c."TaraGr" THEN
      RAISE EXCEPTION 'El cilindro no está disponible o no contiene refrigerante'; END IF;
    IF EXISTS (SELECT 1 FROM afrisan."Movimientos" WHERE "Id_Cilindro"=NEW."Id_Cilindro" AND "CustodiaAbierta") THEN
      RAISE EXCEPTION 'El cilindro ya está en custodia'; END IF;
    NEW."CustodiaAbierta" := true;
  ELSE
    SELECT * INTO d FROM afrisan."Movimientos" WHERE "Id_Movimiento"=NEW."Id_Despacho" FOR UPDATE;
    IF NOT FOUND OR NOT d."Salida" OR NOT d."CustodiaAbierta" THEN RAISE EXCEPTION 'Despacho no abierto'; END IF;
    IF NEW."FechaMovimiento" < d."FechaMovimiento" OR NEW."PesoGrBruto" > d."PesoGrBruto" THEN
      RAISE EXCEPTION 'El retorno precede al despacho o supera su peso'; END IF;
    IF EXISTS (SELECT 1 FROM afrisan."Intervencion" i WHERE i."Id_Despacho"=d."Id_Movimiento"
      AND (i."Estado" <> 'Cerrada' OR i."FechaIntervencion" > NEW."FechaMovimiento")) THEN
      RAISE EXCEPTION 'Hay intervenciones sin cerrar o posteriores al retorno'; END IF;
    NEW."CustodiaAbierta" := false;
    UPDATE afrisan."Movimientos" SET "CustodiaAbierta"=false WHERE "Id_Movimiento"=d."Id_Movimiento";
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER "TR_Movimiento_Validar" BEFORE INSERT ON afrisan."Movimientos"
FOR EACH ROW EXECUTE FUNCTION afrisan.validar_movimiento();

CREATE FUNCTION afrisan.validar_intervencion() RETURNS trigger LANGUAGE plpgsql
SET search_path = pg_catalog, afrisan AS $$
DECLARE d afrisan."Movimientos"; e afrisan."Equipos"; ot afrisan."OrdenTrabajo"; usado numeric; tara bigint;
BEGIN
  SELECT * INTO ot FROM afrisan."OrdenTrabajo" WHERE "Id_OrdenTrabajo"=NEW."Id_OrdenTrabajo" FOR UPDATE;
  IF NOT FOUND OR ot."Estado" NOT IN ('Abierta','EnCurso') THEN RAISE EXCEPTION 'OT no abierta'; END IF;
  SELECT "TaraGr" INTO tara FROM afrisan."Cilindros" WHERE "Id_Cilindro"=NEW."Id_Cilindro" FOR UPDATE;
  SELECT * INTO d FROM afrisan."Movimientos" WHERE "Id_Movimiento"=NEW."Id_Despacho" FOR UPDATE;
  IF NOT FOUND OR NOT d."Salida" OR NOT d."CustodiaAbierta" THEN RAISE EXCEPTION 'Se requiere un despacho abierto'; END IF;
  IF NEW."FechaIntervencion" < d."FechaMovimiento" THEN RAISE EXCEPTION 'La intervención precede al despacho'; END IF;
  SELECT * INTO e FROM afrisan."Equipos" WHERE "Id_Equipo"=NEW."Id_Equipo" FOR UPDATE;
  IF NOT FOUND OR NOT e."Activo" THEN RAISE EXCEPTION 'Equipo inexistente o inactivo'; END IF;
  IF NOT EXISTS (SELECT 1 FROM afrisan."CausasFalla" WHERE "Id_CausaFalla"=NEW."Id_CausaFalla" AND "Activo") THEN
    RAISE EXCEPTION 'Causa de falla no activa'; END IF;
  IF TG_OP='INSERT' THEN
    NEW."CapacidadNominalGrAplicada" := e."CapacidadNominalGr";
  ELSE
    -- La capacidad del equipo puede cambiar después; conservar la aplicada.
    IF NEW."Id_Equipo" <> OLD."Id_Equipo" OR NEW."Id_Despacho" <> OLD."Id_Despacho"
       OR NEW."Id_Cilindro" <> OLD."Id_Cilindro" OR NEW."Id_OrdenTrabajo" <> OLD."Id_OrdenTrabajo"
       OR NEW."Id_Tecnico" <> OLD."Id_Tecnico" OR NEW."FechaIntervencion" <> OLD."FechaIntervencion" THEN
      RAISE EXCEPTION 'No se cambia la identidad/fecha del trabajo registrado; requiere rectificación';
    END IF;
    NEW."CapacidadNominalGrAplicada" := OLD."CapacidadNominalGrAplicada";
  END IF;
  SELECT coalesce(sum("GrUtilizados"),0) INTO usado FROM afrisan."Intervencion"
    WHERE "Id_Despacho"=NEW."Id_Despacho" AND "Id_Intervencion"<>NEW."Id_Intervencion";
  IF usado+NEW."GrUtilizados" > d."PesoGrBruto"-tara THEN RAISE EXCEPTION 'Consumo declarado superior al contenido despachado'; END IF;
  IF NEW."PesoRestanteGrBruto" IS NOT NULL AND
    (NEW."PesoRestanteGrBruto" < tara OR NEW."PesoRestanteGrBruto" > d."PesoGrBruto") THEN
    RAISE EXCEPTION 'Peso restante fuera del rango del despacho'; END IF;
  -- RN-03: otra carga anterior en el mismo equipo, hace menos de 30 días.
  -- La sincronización offline debe reconciliar también registros posteriores
  -- ya existentes cuando llega un trabajo antiguo (responsabilidad del servicio).
  IF NEW."GrUtilizados"::numeric / NEW."CapacidadNominalGrAplicada" > 0.15 OR EXISTS (
    SELECT 1 FROM afrisan."Intervencion" WHERE "Id_Equipo"=NEW."Id_Equipo"
      AND "Id_Intervencion"<>NEW."Id_Intervencion"
      AND "FechaIntervencion" <= NEW."FechaIntervencion"
      AND "FechaIntervencion" > NEW."FechaIntervencion" - interval '30 days') THEN
    NEW."EstadoSemaforo" := 'Rojo';
  ELSIF NEW."GrUtilizados"::numeric / NEW."CapacidadNominalGrAplicada" >= 0.05 THEN NEW."EstadoSemaforo" := 'Amarillo';
  ELSE NEW."EstadoSemaforo" := 'Verde'; END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER "TR_Intervencion_Validar" BEFORE INSERT OR UPDATE ON afrisan."Intervencion"
FOR EACH ROW EXECUTE FUNCTION afrisan.validar_intervencion();

-- Los pesajes son hechos históricos. Solo el trigger de retorno cierra
-- CustodiaAbierta internamente; una corrección requiere un proceso explícito.
CREATE FUNCTION afrisan.proteger_movimiento() RETURNS trigger LANGUAGE plpgsql
SET search_path = pg_catalog, afrisan AS $$
BEGIN
  IF TG_OP='UPDATE' AND pg_trigger_depth()>1 THEN
    IF OLD."Salida" AND OLD."CustodiaAbierta" AND NOT NEW."CustodiaAbierta"
       AND (to_jsonb(NEW)-'CustodiaAbierta')=(to_jsonb(OLD)-'CustodiaAbierta') THEN RETURN NEW; END IF;
  END IF;
  RAISE EXCEPTION 'Los pesajes registrados no se editan ni eliminan; requieren rectificación auditada';
END $$;
CREATE TRIGGER "TR_Movimiento_Historico" BEFORE UPDATE OR DELETE ON afrisan."Movimientos"
FOR EACH ROW EXECUTE FUNCTION afrisan.proteger_movimiento();

CREATE FUNCTION afrisan.proteger_intervencion() RETURNS trigger LANGUAGE plpgsql
SET search_path = pg_catalog, afrisan AS $$
BEGIN
  IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Las intervenciones no se eliminan; requieren rectificación auditada'; END IF;
  IF OLD."Estado"='Cerrada' AND NEW IS DISTINCT FROM OLD THEN
    RAISE EXCEPTION 'Una intervención cerrada requiere un proceso de rectificación'; END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER "TR_Intervencion_Historica" BEFORE UPDATE OR DELETE ON afrisan."Intervencion"
FOR EACH ROW EXECUTE FUNCTION afrisan.proteger_intervencion();

CREATE FUNCTION afrisan.proteger_cilindro() RETURNS trigger LANGUAGE plpgsql
SET search_path = pg_catalog, afrisan AS $$
BEGIN
  IF (NEW."TaraGr",NEW."Capacidadgr",NEW."Id_Refrigerante",NEW."CilindroQR") IS DISTINCT FROM
     (OLD."TaraGr",OLD."Capacidadgr",OLD."Id_Refrigerante",OLD."CilindroQR")
     AND EXISTS (SELECT 1 FROM afrisan."Movimientos" WHERE "Id_Cilindro"=OLD."Id_Cilindro") THEN
    RAISE EXCEPTION 'No se cambia tara, capacidad, refrigerante ni QR de cilindros con historial'; END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER "TR_Cilindro_Historico" BEFORE UPDATE ON afrisan."Cilindros"
FOR EACH ROW EXECUTE FUNCTION afrisan.proteger_cilindro();

-- Una fila por ciclo salida/retorno; compara gramos físicos con declarados.
CREATE VIEW afrisan."BalanceCilindro" AS
SELECT d."Id_Movimiento" AS "Id_Despacho", d."Id_OrdenTrabajo", d."Id_Cilindro", d."Id_Tecnico",
  d."PesoGrBruto" AS "PesoSalidaGr", r."PesoGrBruto" AS "PesoRetornoGr",
  coalesce(i.declarado,0) AS "GrDeclarados",
  d."PesoGrBruto" - r."PesoGrBruto" - coalesce(i.declarado,0) AS "DescalceGr",
  CASE WHEN r."Id_Movimiento" IS NULL THEN 'PendienteRetorno'
    WHEN abs(d."PesoGrBruto" - r."PesoGrBruto" - coalesce(i.declarado,0)) < 200 THEN 'RetornadoConforme'
    ELSE 'Descalce' END AS "Resultado"
FROM afrisan."Movimientos" d
LEFT JOIN afrisan."Movimientos" r ON r."Id_Despacho"=d."Id_Movimiento"
LEFT JOIN (SELECT "Id_Despacho",sum("GrUtilizados") declarado FROM afrisan."Intervencion" GROUP BY "Id_Despacho") i
  ON i."Id_Despacho"=d."Id_Movimiento"
WHERE d."Salida";

CREATE VIEW afrisan."EstadoCilindros" AS
SELECT c."Id_Cilindro",c."CilindroQR",c."Id_Refrigerante",c."Estado",c."TaraGr",c."Capacidadgr",
  m."PesoGrBruto" AS "PesoRegistradoGr",m."FechaMovimiento" AS "FechaUltimoPesaje",
  CASE WHEN m."Id_Movimiento" IS NULL THEN NULL
    WHEN m."CustodiaAbierta" THEN m."PesoGrBruto" - c."TaraGr" - coalesce(i.utilizados,0)
    ELSE m."PesoGrBruto" - c."TaraGr" END AS "ContenidoEstimadoGr"
FROM afrisan."Cilindros" c
LEFT JOIN LATERAL (SELECT * FROM afrisan."Movimientos" WHERE "Id_Cilindro"=c."Id_Cilindro"
  ORDER BY "FechaMovimiento" DESC,"Id_Movimiento" DESC LIMIT 1) m ON true
LEFT JOIN LATERAL (SELECT sum("GrUtilizados") utilizados FROM afrisan."Intervencion"
  WHERE "Id_Despacho"=m."Id_Movimiento") i ON true;
COMMENT ON VIEW afrisan."EstadoCilindros" IS
  'Último peso realmente registrado, separado del contenido estimado tras descontar cargas. NULL sin pesajes. No reemplaza una lectura de balanza.';

CREATE FUNCTION afrisan.aplicar_movimiento() RETURNS trigger LANGUAGE plpgsql
SET search_path = pg_catalog, afrisan AS $$
BEGIN
  UPDATE afrisan."Cilindros" SET "Estado"=CASE WHEN NEW."Salida" THEN 'Despachado'
    WHEN (SELECT "Resultado" FROM afrisan."BalanceCilindro" WHERE "Id_Despacho"=NEW."Id_Despacho")='RetornadoConforme' THEN 'Disponible'
    ELSE 'RetornadoConDescalce' END WHERE "Id_Cilindro"=NEW."Id_Cilindro";
  IF NEW."Salida" THEN
    UPDATE afrisan."OrdenTrabajo" SET "Estado"='EnCurso',"FechaSalida"=coalesce("FechaSalida",NEW."FechaMovimiento")
      WHERE "Id_OrdenTrabajo"=NEW."Id_OrdenTrabajo";
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER "TR_Movimiento_Aplicar" AFTER INSERT ON afrisan."Movimientos"
FOR EACH ROW EXECUTE FUNCTION afrisan.aplicar_movimiento();

CREATE FUNCTION afrisan.validar_cierre_ot() RETURNS trigger LANGUAGE plpgsql
SET search_path = pg_catalog, afrisan AS $$
BEGIN
  IF TG_OP='UPDATE' THEN
    IF OLD."Estado" IN ('Cerrada','Anulada') AND NEW IS DISTINCT FROM OLD THEN
      RAISE EXCEPTION 'Una OT finalizada no se modifica: requiere un procedimiento de rectificación'; END IF;
  END IF;
  IF NEW."Estado"='Anulada' AND EXISTS (SELECT 1 FROM afrisan."Movimientos" WHERE "Id_OrdenTrabajo"=NEW."Id_OrdenTrabajo") THEN
    RAISE EXCEPTION 'No se anula una OT con movimientos'; END IF;
  IF NEW."Estado"='Cerrada' THEN
    IF EXISTS (SELECT 1 FROM afrisan."BalanceCilindro" WHERE "Id_OrdenTrabajo"=NEW."Id_OrdenTrabajo" AND "Resultado"<>'RetornadoConforme') THEN
      RAISE EXCEPTION 'No se cierra la OT: hay cilindros pendientes o descalces'; END IF;
    IF EXISTS (SELECT 1 FROM afrisan."Intervencion" WHERE "Id_OrdenTrabajo"=NEW."Id_OrdenTrabajo" AND "Estado"<>'Cerrada') THEN
      RAISE EXCEPTION 'No se cierra la OT: hay intervenciones sin evidencia/cierre'; END IF;
    NEW."FechaFinalizada" := coalesce(NEW."FechaFinalizada",now());
  ELSIF NEW."Estado"='Anulada' THEN NEW."FechaFinalizada" := coalesce(NEW."FechaFinalizada",now());
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER "TR_OT_Cierre" BEFORE INSERT OR UPDATE ON afrisan."OrdenTrabajo"
FOR EACH ROW EXECUTE FUNCTION afrisan.validar_cierre_ot();

CREATE FUNCTION afrisan.certificar_cilindro() RETURNS trigger LANGUAGE plpgsql
SET search_path = pg_catalog, afrisan AS $$
DECLARE estado text;
BEGIN
  SELECT "Estado" INTO estado FROM afrisan."Cilindros" WHERE "Id_Cilindro"=NEW."Id_Cilindro" FOR UPDATE;
  IF estado IS DISTINCT FROM 'Chatarra' OR EXISTS (SELECT 1 FROM afrisan."Movimientos" WHERE "Id_Cilindro"=NEW."Id_Cilindro" AND "CustodiaAbierta") THEN
    RAISE EXCEPTION 'Solo se certifican cilindros clasificados como chatarra, sin custodia abierta'; END IF;
  UPDATE afrisan."Cilindros" SET "Estado"='DisposicionFinalCertificada' WHERE "Id_Cilindro"=NEW."Id_Cilindro";
  RETURN NEW;
END $$;
CREATE TRIGGER "TR_Certificado_Cilindro" BEFORE INSERT ON afrisan."CertificadosCilindros"
FOR EACH ROW EXECUTE FUNCTION afrisan.certificar_cilindro();

-- AUDITORÍA propuesta de implementación para conservar todos los cambios.
-- La API ejecuta dentro de la MISMA transacción:
-- SELECT set_config('afrisan.id_usuario', :id_usuario_autenticado::text, true);
-- El valor lo establece el backend de confianza, nunca un campo libre del cliente.
CREATE FUNCTION afrisan.auditar() RETURNS trigger LANGUAGE plpgsql
SET search_path = pg_catalog, afrisan AS $$
DECLARE anterior jsonb; nuevo jsonb; actor integer; registro text;
BEGIN
  IF TG_OP <> 'INSERT' THEN anterior := to_jsonb(OLD); END IF;
  IF TG_OP <> 'DELETE' THEN nuevo := to_jsonb(NEW); END IF;
  -- Excluir secretos y estado técnico de autenticación del historial.
  IF TG_TABLE_NAME='Usuario' THEN
    anterior := anterior - ARRAY['Clave_Usuario','VersionSeguridad'];
    nuevo := nuevo - ARRAY['Clave_Usuario','VersionSeguridad'];
  END IF;
  actor := nullif(current_setting('afrisan.id_usuario',true),'')::integer;
  registro := coalesce(nuevo ->> TG_ARGV[0], anterior ->> TG_ARGV[0]);
  INSERT INTO afrisan."Auditoria" ("Id_Usuario","Tabla","Id_Registro","Accion","ValoresAnteriores","ValoresNuevos")
  VALUES (actor,TG_TABLE_NAME,registro,TG_OP,anterior,nuevo);
  RETURN NULL;
END $$;
DO $$
DECLARE t text; clave text;
BEGIN
  FOR t,clave IN VALUES
    ('Rol','Id_Rol'), ('Usuario','Id_Usuario'), ('Cliente','Id_Cliente'), ('Sucursales','Id_Sucursal'),
    ('Refrigerantes','Id_Refrigerante'), ('Cilindros','Id_Cilindro'), ('Equipos','Id_Equipo'),
    ('CausasFalla','Id_CausaFalla'), ('OrdenTrabajo','Id_OrdenTrabajo'), ('Movimientos','Id_Movimiento'),
    ('Intervencion','Id_Intervencion'), ('PreciosRefrigerantes','Id_Precio'), ('Costos','Id_Costos'),
    ('CertificadosDisposicion','Id_Certificado'), ('CertificadosCilindros','Id_Cilindro')
  LOOP
    EXECUTE format('CREATE TRIGGER %I AFTER INSERT OR UPDATE OR DELETE ON afrisan.%I FOR EACH ROW EXECUTE FUNCTION afrisan.auditar(%L)', 'TR_Auditoria_'||t,t,clave);
  END LOOP;
END $$;

-- REPORTERÍA: sin usuarios, claves, imágenes ni rutas privadas. No se publican
-- esquemas en Data API ni se otorgan accesos de Power BI automáticamente.
CREATE VIEW afrisan_bi."ConsumoIntervenciones" AS
SELECT i."Id_Intervencion",i."Id_OrdenTrabajo",s."Id_Cliente",i."Id_Sucursal",i."Id_Equipo",
  i."Id_Cilindro",i."Id_Refrigerante",i."Id_CausaFalla",i."FechaIntervencion",i."EstadoSemaforo",
  i."GrUtilizados"::numeric/1000 AS "KgUtilizados",i."GrEmitidos"::numeric/1000 AS "KgEmitidos",
  i."EmisionesCO2eTon",c."PrecioTotal",c."Moneda",
  c."Id_Costos" IS NOT NULL AS "TieneValoracion",
  i."EmisionesCO2eTon" IS NOT NULL AS "TieneEstimacionEmisiones"
FROM afrisan."Intervencion" i
JOIN afrisan."Sucursales" s USING ("Id_Sucursal")
LEFT JOIN afrisan."Costos" c USING ("Id_Intervencion");
CREATE VIEW afrisan_bi."Conciliacion" AS
SELECT "Id_Despacho","Id_OrdenTrabajo","Id_Cilindro","PesoSalidaGr","PesoRetornoGr","GrDeclarados","DescalceGr","Resultado"
FROM afrisan."BalanceCilindro";
CREATE VIEW afrisan_bi."SucursalesCriticasMes" AS
SELECT "Id_Sucursal",date_trunc('month',"FechaIntervencion" AT TIME ZONE 'America/Santiago') AS "Mes",
  count(*) AS "Intervenciones",count(*) FILTER (WHERE "EstadoSemaforo"='Rojo') AS "IntervencionesCriticas",
  count(*) FILTER (WHERE "EstadoSemaforo"='Rojo') > 3 AS "SuperaTresCriticas"
FROM afrisan."Intervencion" GROUP BY "Id_Sucursal",date_trunc('month',"FechaIntervencion" AT TIME ZONE 'America/Santiago');
COMMENT ON VIEW afrisan_bi."SucursalesCriticasMes" IS
  'Parte verificable de HU-07: más de tres críticas al mes. La definición adicional de sobreconsumo recurrente de sucursal debe acordarse; no se inventa un umbral.';

-- RLS sin políticas: anon/authenticated no acceden directamente.
-- Las operaciones se hacen desde una API con usuario PostgreSQL de backend.
-- El propietario/roles BYPASSRLS pueden acceder; jamás compartir esas claves.
DO $$ DECLARE t record; BEGIN
  FOR t IN SELECT tablename FROM pg_tables WHERE schemaname='afrisan' LOOP
    EXECUTE format('ALTER TABLE afrisan.%I ENABLE ROW LEVEL SECURITY',t.tablename);
  END LOOP;
END $$;
REVOKE ALL ON ALL TABLES IN SCHEMA afrisan, afrisan_bi FROM PUBLIC, anon, authenticated;
REVOKE ALL ON ALL SEQUENCES IN SCHEMA afrisan FROM PUBLIC, anon, authenticated;
REVOKE EXECUTE ON ALL FUNCTIONS IN SCHEMA afrisan FROM PUBLIC, anon, authenticated;
ALTER DEFAULT PRIVILEGES IN SCHEMA afrisan REVOKE EXECUTE ON FUNCTIONS FROM PUBLIC;

-- No se crean usuarios ficticios ni contraseñas. El primer administrador debe
-- aprovisionarse desde la API con un hash generado por el mecanismo elegido.
-- Storage privado, conexión Power BI y permisos del usuario técnico se
-- configuran después. Consultar README antes de otorgar privilegios.
COMMIT;
