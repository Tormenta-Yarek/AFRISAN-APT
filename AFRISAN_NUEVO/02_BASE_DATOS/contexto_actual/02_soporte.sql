-- Módulos de soporte del contexto actual. Ejecutar después de 01_operacion.sql.
-- No inserta roles, usuarios, catálogos ni datos de ejemplo.
-- En el archivo canónico este módulo comparte la transacción de instalación.
BEGIN;

-- LÍMITES: el criterio de cómputo sigue requiriendo acuerdo de negocio.
-- Se guardan límites y mediciones declaradas, no un consumo/emisión inferidos.
CREATE TABLE afrisan."LimiteSucursal" (
  "Id_Limite" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_Sucursal" integer NOT NULL REFERENCES afrisan."Sucursales" ON DELETE RESTRICT,
  "Periodicidad" varchar(10) NOT NULL CHECK ("Periodicidad" IN ('Semana','Mes')),
  "EstadoDefinicion" varchar(15) NOT NULL DEFAULT 'Pendiente'
    CHECK ("EstadoDefinicion" IN ('Pendiente','Definido')),
  "GrLimite" bigint CHECK ("GrLimite" > 0),
  "CriterioComputo" text,
  "VigenteDesde" timestamptz NOT NULL DEFAULT now(),
  "BloqueadoBodega" boolean NOT NULL DEFAULT false,
  "Version" integer NOT NULL DEFAULT 1 CHECK ("Version" > 0),
  "Id_UltimoEditor" integer NOT NULL REFERENCES afrisan."Usuario" ON DELETE RESTRICT,
  "FechaActualizacion" timestamptz NOT NULL DEFAULT now(),
  UNIQUE ("Id_Sucursal", "Periodicidad"),
  CHECK ("EstadoDefinicion" <> 'Definido' OR
    ("GrLimite" IS NOT NULL AND coalesce(btrim("CriterioComputo"),'') <> ''))
);

CREATE TABLE afrisan."LimiteSucursalHistorial" (
  "Id_HistorialLimite" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_Limite" bigint NOT NULL REFERENCES afrisan."LimiteSucursal" ON DELETE RESTRICT,
  "Version" integer NOT NULL,
  "Id_Sucursal" integer NOT NULL REFERENCES afrisan."Sucursales" ON DELETE RESTRICT,
  "Periodicidad" varchar(10) NOT NULL,
  "EstadoDefinicion" varchar(15) NOT NULL,
  "GrLimite" bigint,
  "CriterioComputo" text,
  "VigenteDesde" timestamptz NOT NULL,
  "BloqueadoBodega" boolean NOT NULL,
  "Id_Actor" integer NOT NULL REFERENCES afrisan."Usuario" ON DELETE RESTRICT,
  "FechaRegistro" timestamptz NOT NULL,
  UNIQUE ("Id_Limite", "Version")
);

CREATE FUNCTION afrisan.validar_limite_sucursal() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog, afrisan AS $$
DECLARE v_actor integer; v_rol text; v_cambio boolean;
BEGIN
  IF TG_OP = 'DELETE' THEN
    RAISE EXCEPTION 'El límite y su historial se conservan; no se eliminan';
  END IF;
  v_actor := afrisan.actor(ARRAY['Bodega','Jefatura']);
  SELECT r."Nombre_Rol" INTO v_rol FROM afrisan."Usuario" u
    JOIN afrisan."Rol" r ON r."Id_Rol" = u."Id_Rol"
    WHERE u."Id_Usuario" = v_actor;
  IF TG_OP = 'INSERT' THEN
    NEW."Version" := 1;
    NEW."BloqueadoBodega" := v_rol = 'Jefatura';
  ELSE
    IF (NEW."Id_Limite",NEW."Id_Sucursal",NEW."Periodicidad") IS DISTINCT FROM
       (OLD."Id_Limite",OLD."Id_Sucursal",OLD."Periodicidad") THEN
      RAISE EXCEPTION 'No se cambia la identidad, sucursal ni periodicidad del límite';
    END IF;
    IF v_rol = 'Bodega' AND OLD."BloqueadoBodega" THEN
      RAISE EXCEPTION 'Jefatura mantiene bloqueada la edición por Bodega';
    END IF;
    IF v_rol = 'Bodega' AND NEW."BloqueadoBodega" IS DISTINCT FROM OLD."BloqueadoBodega" THEN
      RAISE EXCEPTION 'Solo Jefatura puede bloquear o habilitar la edición por Bodega';
    END IF;
    v_cambio := (NEW."EstadoDefinicion",NEW."GrLimite",NEW."CriterioComputo",NEW."VigenteDesde")
      IS DISTINCT FROM (OLD."EstadoDefinicion",OLD."GrLimite",OLD."CriterioComputo",OLD."VigenteDesde");
    -- Si Jefatura modifica el valor/criterio/vigencia, vuelve a bloquear.
    -- Para habilitar, cambia solo BloqueadoBodega a false en otra operación.
    IF v_rol = 'Jefatura' AND v_cambio THEN NEW."BloqueadoBodega" := true; END IF;
    NEW."Version" := OLD."Version" + 1;
  END IF;
  NEW."Id_UltimoEditor" := v_actor;
  NEW."FechaActualizacion" := clock_timestamp();
  RETURN NEW;
END $$;
CREATE TRIGGER "TR_Limite_Validar" BEFORE INSERT OR UPDATE OR DELETE
ON afrisan."LimiteSucursal" FOR EACH ROW EXECUTE FUNCTION afrisan.validar_limite_sucursal();

CREATE FUNCTION afrisan.historiar_limite_sucursal() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog, afrisan AS $$
BEGIN
  INSERT INTO afrisan."LimiteSucursalHistorial"
    ("Id_Limite","Version","Id_Sucursal","Periodicidad","EstadoDefinicion","GrLimite",
     "CriterioComputo","VigenteDesde","BloqueadoBodega","Id_Actor","FechaRegistro")
  VALUES (NEW."Id_Limite",NEW."Version",NEW."Id_Sucursal",NEW."Periodicidad",NEW."EstadoDefinicion",
    NEW."GrLimite",NEW."CriterioComputo",NEW."VigenteDesde",NEW."BloqueadoBodega",
    NEW."Id_UltimoEditor",NEW."FechaActualizacion");
  RETURN NEW;
END $$;
CREATE TRIGGER "TR_Limite_Historial" AFTER INSERT OR UPDATE ON afrisan."LimiteSucursal"
FOR EACH ROW EXECUTE FUNCTION afrisan.historiar_limite_sucursal();

CREATE FUNCTION afrisan.proteger_historial_limite() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog, afrisan AS $$
BEGIN
  IF TG_OP <> 'INSERT' OR pg_trigger_depth() < 2 THEN
    RAISE EXCEPTION 'El historial de límites solo se genera al registrar una versión del límite';
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER "TR_LimiteHistorial_Inmutable" BEFORE INSERT OR UPDATE OR DELETE
ON afrisan."LimiteSucursalHistorial" FOR EACH ROW EXECUTE FUNCTION afrisan.proteger_historial_limite();

CREATE TABLE afrisan."MedicionLimite" (
  "Id_Medicion" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_HistorialLimite" bigint NOT NULL REFERENCES afrisan."LimiteSucursalHistorial" ON DELETE RESTRICT,
  "PeriodoInicio" date NOT NULL,
  "PeriodoFin" date NOT NULL,
  "GrComputados" bigint NOT NULL CHECK ("GrComputados" >= 0),
  "MetodologiaComputo" text NOT NULL CHECK (btrim("MetodologiaComputo") <> ''),
  "Observaciones" text,
  "Id_Registrador" integer NOT NULL REFERENCES afrisan."Usuario" ON DELETE RESTRICT,
  "FechaRegistro" timestamptz NOT NULL DEFAULT now(),
  CHECK ("PeriodoFin" >= "PeriodoInicio")
);
COMMENT ON TABLE afrisan."MedicionLimite" IS
  'Cómputo declarado con metodología explícita y límite histórico. No calcula automáticamente consumo ni el criterio de semana/mes; los períodos se indican expresamente hasta acordar calendario, masa y desglose.';
CREATE TABLE afrisan."MedicionLimiteOperaciones" (
  "Id_Medicion" bigint NOT NULL REFERENCES afrisan."MedicionLimite" ON DELETE RESTRICT,
  "Id_Operacion" uuid NOT NULL REFERENCES afrisan."OperacionTerreno" ON DELETE RESTRICT,
  PRIMARY KEY ("Id_Medicion", "Id_Operacion")
);
CREATE INDEX "IX_MedicionLimite_Periodo" ON afrisan."MedicionLimite" ("Id_HistorialLimite","PeriodoInicio","PeriodoFin");
CREATE INDEX "IX_MedicionLimite_Operacion" ON afrisan."MedicionLimiteOperaciones" ("Id_Operacion");
CREATE TABLE afrisan."RevisionExceso" (
  "Id_RevisionExceso" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_Medicion" bigint NOT NULL UNIQUE REFERENCES afrisan."MedicionLimite" ON DELETE RESTRICT,
  "Conclusion" text NOT NULL CHECK (length(btrim("Conclusion")) >= 5),
  "Id_Revisor" integer NOT NULL REFERENCES afrisan."Usuario" ON DELETE RESTRICT,
  "FechaRevision" timestamptz NOT NULL DEFAULT now()
);
COMMENT ON TABLE afrisan."RevisionExceso" IS
  'Conclusión expresa de Jefatura sobre un exceso declarado. Superar un límite no atribuye culpa ni demuestra una pérdida física.';

-- VALORACIÓN: cantidades y conceptos explícitos; no se derivan del reemplazo.
CREATE TABLE afrisan."PreciosRefrigerantes" (
  "Id_Precio" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_Refrigerante" bigint NOT NULL REFERENCES afrisan."Refrigerantes" ON DELETE RESTRICT,
  "PrecioGrRefrigerante" numeric(18,8) NOT NULL CHECK ("PrecioGrRefrigerante" >= 0),
  "Moneda" varchar(3) NOT NULL CHECK ("Moneda" ~ '^[A-Z]{3}$'),
  "VigenteDesde" timestamptz NOT NULL,
  "Id_Registrador" integer NOT NULL REFERENCES afrisan."Usuario" ON DELETE RESTRICT,
  "FechaRegistro" timestamptz NOT NULL DEFAULT now(),
  UNIQUE ("Id_Refrigerante","Moneda","VigenteDesde")
);
CREATE TABLE afrisan."Costos" (
  "Id_Costos" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_Operacion" uuid NOT NULL REFERENCES afrisan."OperacionTerreno" ON DELETE RESTRICT,
  "NumeroLinea" integer NOT NULL CHECK ("NumeroLinea" > 0),
  "Concepto" varchar(20) NOT NULL CHECK ("Concepto" IN ('Refrigerante','Envase','ManoObra','Repuesto','Traslado','Otro')),
  "Descripcion" text NOT NULL CHECK (btrim("Descripcion") <> ''),
  "Cantidad" numeric(18,6) NOT NULL CHECK ("Cantidad" > 0),
  "Unidad" varchar(40) NOT NULL CHECK (btrim("Unidad") <> ''),
  "TarifaUnitaria" numeric(18,8) NOT NULL CHECK ("TarifaUnitaria" >= 0),
  "Id_Precio" bigint REFERENCES afrisan."PreciosRefrigerantes" ON DELETE RESTRICT,
  "Moneda" varchar(3) NOT NULL CHECK ("Moneda" ~ '^[A-Z]{3}$'),
  "PrecioTotal" numeric(30,8) GENERATED ALWAYS AS ("Cantidad" * "TarifaUnitaria") STORED,
  "Id_Registrador" integer NOT NULL REFERENCES afrisan."Usuario" ON DELETE RESTRICT,
  "FechaValoracion" timestamptz NOT NULL DEFAULT now(),
  UNIQUE ("Id_Operacion","NumeroLinea"),
  CHECK ("Id_Precio" IS NULL OR ("Concepto" = 'Refrigerante' AND "Unidad" = 'g'))
);
COMMENT ON TABLE afrisan."Costos" IS
  'Valoración por líneas de una operación física. Cantidad y tarifa son explícitas; instalar/reemplazar un cilindro no significa consumir ni facturar todo su contenido. La API determina la base de cobro acordada y conserva esta tarifa aplicada.';

-- DISPOSICIÓN: lote entregado primero; certificado recibido después.
CREATE TABLE afrisan."CertificadosDisposicion" (
  "Id_Certificado" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Estado" varchar(25) NOT NULL DEFAULT 'PendienteCertificado'
    CHECK ("Estado" IN ('PendienteCertificado','Certificado')),
  "EmpresaEmisora" varchar(150) NOT NULL CHECK (btrim("EmpresaEmisora") <> ''),
  "Folio" varchar(120),
  "FechaEntrega" date NOT NULL,
  "FechaEmision" date,
  "Documento" text,
  "Id_UsuarioRegistro" integer NOT NULL REFERENCES afrisan."Usuario" ON DELETE RESTRICT,
  "FechaRegistro" timestamptz NOT NULL DEFAULT now(),
  "Id_UsuarioCertifica" integer REFERENCES afrisan."Usuario" ON DELETE RESTRICT,
  "FechaRecepcionCertificado" timestamptz,
  UNIQUE ("EmpresaEmisora","Folio"),
  CHECK ("FechaEmision" IS NULL OR "FechaEmision" >= "FechaEntrega"),
  CHECK (("Estado" = 'Certificado') =
    (coalesce(btrim("Folio"),'') <> '' AND "FechaEmision" IS NOT NULL AND
     coalesce(btrim("Documento"),'') <> '' AND "Id_UsuarioCertifica" IS NOT NULL AND "FechaRecepcionCertificado" IS NOT NULL))
);
CREATE TABLE afrisan."CertificadosCilindros" (
  "Id_Certificado" bigint NOT NULL REFERENCES afrisan."CertificadosDisposicion" ON DELETE RESTRICT,
  "Id_Cilindro" bigint NOT NULL UNIQUE REFERENCES afrisan."Cilindros" ON DELETE RESTRICT,
  PRIMARY KEY ("Id_Certificado","Id_Cilindro")
);
COMMENT ON TABLE afrisan."CertificadosCilindros" IS
  'Cilindros en estado Chatarra de un lote pendiente de certificado. No exige intervenciones ni cargas de gas. El certificado y sus enlaces se conservan; no se asignan emisiones automáticamente.';

-- Estos módulos de soporte no amplían permisos operativos sin acuerdo.
-- Administrador registra tarifas/valoraciones/certificados por ahora.
-- La API podrá conceder las tareas a otros roles solo al definir esa política.
CREATE FUNCTION afrisan.validar_soporte() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog, afrisan AS $$
DECLARE v_actor integer; v_limite afrisan."LimiteSucursalHistorial"%ROWTYPE;
  v_medicion afrisan."MedicionLimite"%ROWTYPE; v_precio afrisan."PreciosRefrigerantes"%ROWTYPE;
  v_gas bigint; v_sucursal integer; v_cert afrisan."CertificadosDisposicion"%ROWTYPE; v_estado text;
BEGIN
  IF TG_OP <> 'INSERT' THEN RAISE EXCEPTION 'Este registro histórico no se modifica ni elimina'; END IF;
  IF TG_TABLE_NAME = 'MedicionLimite' THEN
    v_actor := afrisan.actor(ARRAY['Bodega','Autorizador','Jefatura']);
    SELECT * INTO STRICT v_limite FROM afrisan."LimiteSucursalHistorial"
      WHERE "Id_HistorialLimite" = NEW."Id_HistorialLimite";
    IF v_limite."EstadoDefinicion" <> 'Definido' THEN RAISE EXCEPTION 'No se evalúa un límite cuyo criterio sigue pendiente'; END IF;
    NEW."Id_Registrador" := v_actor; NEW."FechaRegistro" := clock_timestamp();
  ELSIF TG_TABLE_NAME = 'MedicionLimiteOperaciones' THEN
    PERFORM afrisan.actor(ARRAY['Bodega','Autorizador','Jefatura']);
    SELECT * INTO STRICT v_medicion FROM afrisan."MedicionLimite" WHERE "Id_Medicion" = NEW."Id_Medicion";
    SELECT "Id_Sucursal" INTO STRICT v_sucursal FROM afrisan."LimiteSucursalHistorial"
      WHERE "Id_HistorialLimite" = v_medicion."Id_HistorialLimite";
    IF NOT EXISTS (SELECT 1 FROM afrisan."OperacionTerreno" op
      JOIN afrisan."OrdenTrabajo" ot ON ot."Id_OrdenTrabajo" = op."Id_OrdenTrabajo"
      WHERE op."Id_Operacion" = NEW."Id_Operacion" AND op."Estado" = 'Registrada' AND ot."Id_Sucursal" = v_sucursal) THEN
      RAISE EXCEPTION 'La operación debe estar registrada y pertenecer a la sucursal del cómputo';
    END IF;
  ELSIF TG_TABLE_NAME = 'RevisionExceso' THEN
    v_actor := afrisan.actor(ARRAY['Jefatura']);
    SELECT * INTO STRICT v_medicion FROM afrisan."MedicionLimite" WHERE "Id_Medicion" = NEW."Id_Medicion";
    SELECT * INTO STRICT v_limite FROM afrisan."LimiteSucursalHistorial"
      WHERE "Id_HistorialLimite" = v_medicion."Id_HistorialLimite";
    IF v_medicion."GrComputados" <= v_limite."GrLimite" THEN RAISE EXCEPTION 'La medición no registra un exceso sobre el límite aplicado'; END IF;
    NEW."Id_Revisor" := v_actor; NEW."FechaRevision" := clock_timestamp();
  ELSIF TG_TABLE_NAME = 'PreciosRefrigerantes' THEN
    NEW."Id_Registrador" := afrisan.actor(ARRAY['Administrador']); NEW."FechaRegistro" := clock_timestamp();
  ELSIF TG_TABLE_NAME = 'Costos' THEN
    v_actor := afrisan.actor(ARRAY['Administrador']);
    SELECT c."Id_Refrigerante" INTO v_gas FROM afrisan."OperacionTerreno" op
      JOIN afrisan."Cilindros" c ON c."Id_Cilindro" = op."Id_Instalado"
      WHERE op."Id_Operacion" = NEW."Id_Operacion" AND op."Estado" = 'Registrada';
    IF v_gas IS NULL THEN RAISE EXCEPTION 'Solo se valora una operación física registrada'; END IF;
    NEW."Id_Registrador" := v_actor;
    NEW."FechaValoracion" := clock_timestamp();
    IF NEW."Id_Precio" IS NOT NULL THEN
      SELECT * INTO STRICT v_precio FROM afrisan."PreciosRefrigerantes" WHERE "Id_Precio" = NEW."Id_Precio";
      IF v_precio."Id_Refrigerante" <> v_gas OR NEW."TarifaUnitaria" <> v_precio."PrecioGrRefrigerante"
        OR NEW."Moneda" <> v_precio."Moneda" THEN RAISE EXCEPTION 'La tarifa aplicada no coincide con el refrigerante, valor o moneda del precio referenciado'; END IF;
    END IF;
  ELSIF TG_TABLE_NAME = 'CertificadosCilindros' THEN
    PERFORM afrisan.actor(ARRAY['Administrador']);
    SELECT * INTO STRICT v_cert FROM afrisan."CertificadosDisposicion" WHERE "Id_Certificado" = NEW."Id_Certificado" FOR UPDATE;
    IF v_cert."Estado" <> 'PendienteCertificado' THEN RAISE EXCEPTION 'El lote certificado ya está cerrado'; END IF;
    SELECT "Estado" INTO STRICT v_estado FROM afrisan."Cilindros" WHERE "Id_Cilindro" = NEW."Id_Cilindro" FOR UPDATE;
    IF v_estado <> 'Chatarra' THEN RAISE EXCEPTION 'Solo se incluye un cilindro clasificado como Chatarra'; END IF;
  ELSE RAISE EXCEPTION 'Tabla de soporte no reconocida';
  END IF;
  RETURN NEW;
END $$;

CREATE FUNCTION afrisan.validar_certificado() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog, afrisan AS $$
DECLARE v_actor integer;
BEGIN
  IF TG_OP = 'DELETE' THEN RAISE EXCEPTION 'No se elimina el historial de disposición'; END IF;
  v_actor := afrisan.actor(ARRAY['Administrador']);
  IF TG_OP = 'INSERT' THEN
    IF NEW."Estado" <> 'PendienteCertificado' THEN RAISE EXCEPTION 'Registra primero la entrega del lote y después su certificado'; END IF;
    NEW."Id_UsuarioRegistro" := v_actor; NEW."FechaRegistro" := clock_timestamp();
    NEW."Id_UsuarioCertifica" := NULL; NEW."FechaRecepcionCertificado" := NULL;
  ELSE
    IF OLD."Estado" <> 'PendienteCertificado' OR NEW."Estado" <> 'Certificado' THEN
      RAISE EXCEPTION 'Solo se permite completar un certificado pendiente; el documento confirmado es inmutable';
    END IF;
    IF (NEW."Id_Certificado",NEW."EmpresaEmisora",NEW."FechaEntrega",NEW."Id_UsuarioRegistro",NEW."FechaRegistro")
      IS DISTINCT FROM (OLD."Id_Certificado",OLD."EmpresaEmisora",OLD."FechaEntrega",OLD."Id_UsuarioRegistro",OLD."FechaRegistro") THEN
      RAISE EXCEPTION 'No se reemplazan los datos históricos de entrega del lote';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM afrisan."CertificadosCilindros" WHERE "Id_Certificado" = OLD."Id_Certificado") THEN
      RAISE EXCEPTION 'El certificado debe identificar al menos un cilindro entregado';
    END IF;
    PERFORM c."Id_Cilindro" FROM afrisan."Cilindros" c
      JOIN afrisan."CertificadosCilindros" l ON l."Id_Cilindro" = c."Id_Cilindro"
      WHERE l."Id_Certificado" = OLD."Id_Certificado" FOR UPDATE OF c;
    IF EXISTS (SELECT 1 FROM afrisan."Cilindros" c
      JOIN afrisan."CertificadosCilindros" l ON l."Id_Cilindro" = c."Id_Cilindro"
      WHERE l."Id_Certificado" = OLD."Id_Certificado" AND c."Estado" <> 'Chatarra') THEN
      RAISE EXCEPTION 'El lote ya no corresponde a cilindros clasificados como Chatarra';
    END IF;
    NEW."Id_UsuarioCertifica" := v_actor; NEW."FechaRecepcionCertificado" := clock_timestamp();
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER "TR_Certificado_Validar" BEFORE INSERT OR UPDATE OR DELETE
ON afrisan."CertificadosDisposicion" FOR EACH ROW EXECUTE FUNCTION afrisan.validar_certificado();

DO $$ DECLARE t text; BEGIN
  FOREACH t IN ARRAY ARRAY['MedicionLimite','MedicionLimiteOperaciones','RevisionExceso',
    'PreciosRefrigerantes','Costos','CertificadosCilindros'] LOOP
    EXECUTE format('CREATE TRIGGER %I BEFORE INSERT OR UPDATE OR DELETE ON afrisan.%I FOR EACH ROW EXECUTE FUNCTION afrisan.validar_soporte()', 'TR_' || t || '_Validar', t);
  END LOOP;
  FOREACH t IN ARRAY ARRAY['LimiteSucursal','LimiteSucursalHistorial','MedicionLimite','MedicionLimiteOperaciones',
    'RevisionExceso','PreciosRefrigerantes','Costos','CertificadosDisposicion','CertificadosCilindros'] LOOP
    EXECUTE format('ALTER TABLE afrisan.%I ENABLE ROW LEVEL SECURITY', t);
    EXECUTE format('REVOKE ALL ON afrisan.%I FROM PUBLIC', t);
  END LOOP;
END $$;

-- Vistas para un lector BI de servidor confiable, con acceso expresamente otorgado
-- por el administrador. No crear GRANT a anon/authenticated ni publicar Data API.
CREATE VIEW afrisan."LimitesVigentes" AS
SELECT DISTINCT ON ("Id_Limite") "Id_Limite","Id_Sucursal","Periodicidad","EstadoDefinicion","GrLimite","CriterioComputo",
  "VigenteDesde","BloqueadoBodega","Version","FechaRegistro" AS "FechaActualizacion"
FROM afrisan."LimiteSucursalHistorial"
WHERE "VigenteDesde" <= current_timestamp
ORDER BY "Id_Limite","VigenteDesde" DESC,"Version" DESC;
CREATE VIEW afrisan."ExcesosRegistrados" AS
SELECT m."Id_Medicion",h."Id_Sucursal",h."Periodicidad",m."PeriodoInicio",m."PeriodoFin",
  h."Version" AS "VersionLimiteAplicada",h."GrLimite",m."GrComputados",
  greatest(m."GrComputados" - h."GrLimite",0) AS "GrExcesoDeclarado",
  h."CriterioComputo",m."MetodologiaComputo",m."FechaRegistro",
  r."Conclusion",r."FechaRevision"
FROM afrisan."MedicionLimite" m
JOIN afrisan."LimiteSucursalHistorial" h ON h."Id_HistorialLimite" = m."Id_HistorialLimite"
LEFT JOIN afrisan."RevisionExceso" r ON r."Id_Medicion" = m."Id_Medicion";
CREATE VIEW afrisan."CostosOperaciones" AS
SELECT c."Id_Costos",c."Id_Operacion",op."Id_OrdenTrabajo",ot."Id_Sucursal",c."NumeroLinea",c."Concepto",
  c."Descripcion",c."Cantidad",c."Unidad",c."TarifaUnitaria",c."Moneda",c."PrecioTotal",c."FechaValoracion"
FROM afrisan."Costos" c JOIN afrisan."OperacionTerreno" op ON op."Id_Operacion" = c."Id_Operacion"
JOIN afrisan."OrdenTrabajo" ot ON ot."Id_OrdenTrabajo" = op."Id_OrdenTrabajo";
CREATE VIEW afrisan."DisposicionesCertificadas" AS
SELECT d."Id_Certificado",d."Estado",d."EmpresaEmisora",d."Folio",d."FechaEntrega",d."FechaEmision",c."Id_Cilindro"
FROM afrisan."CertificadosDisposicion" d
JOIN afrisan."CertificadosCilindros" c ON c."Id_Certificado" = d."Id_Certificado";

REVOKE ALL ON ALL TABLES IN SCHEMA afrisan FROM PUBLIC;
REVOKE EXECUTE ON FUNCTION afrisan.validar_limite_sucursal(),
  afrisan.historiar_limite_sucursal(), afrisan.proteger_historial_limite(),
  afrisan.validar_soporte(), afrisan.validar_certificado() FROM PUBLIC;
COMMIT;
