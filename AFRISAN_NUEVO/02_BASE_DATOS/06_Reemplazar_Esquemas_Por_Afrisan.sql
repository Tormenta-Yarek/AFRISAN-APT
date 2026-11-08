-- AFRISAN | REEMPLAZO COMPLETO AUTORIZADO | 02/11/2026
-- ATENCIÓN: borra TODOS los datos y objetos de los cuatro esquemas indicados.
-- Ejecutar este archivo completo, UNA SOLA VEZ, en SQL Editor de Supabase.
-- No ejecutar además el archivo 05. No ejecutarlo contra datos a conservar.
-- Los cambios se confirman juntos: eliminación y creación en una transacción.
-- public, auth y storage no son destinos de eliminación.
BEGIN;
SET LOCAL lock_timeout = '10s';
DROP SCHEMA IF EXISTS afrisan_bi_actual CASCADE;
DROP SCHEMA IF EXISTS afrisan_bi CASCADE;
DROP SCHEMA IF EXISTS afrisan_actual CASCADE;
DROP SCHEMA IF EXISTS afrisan CASCADE;
-- COMPONENTE: 01_operacion.sql
-- Componente de la instalación 05. No ejecutar junto con el paquete completo.
-- Flujo confirmado el 25/10/2026. Pesos en gramos enteros; fotos en Storage privado.
CREATE SCHEMA afrisan;
REVOKE ALL ON SCHEMA afrisan FROM PUBLIC;

CREATE TABLE afrisan."Rol" (
  "Id_Rol" smallint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Nombre_Rol" varchar(40) NOT NULL UNIQUE CHECK ("Nombre_Rol" IN ('Administrador','Jefatura','Autorizador','Bodega','Tecnico'))
);
CREATE TABLE afrisan."Usuario" (
  "Id_Usuario" integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Nombre" varchar(100) NOT NULL CHECK (btrim("Nombre")<>''),
  "Apellido" varchar(100) NOT NULL CHECK (btrim("Apellido")<>''),
  "Correo_Usuario" varchar(254) NOT NULL CHECK (btrim("Correo_Usuario")<>''),
  "Clave_Usuario" text NOT NULL CHECK (length("Clave_Usuario")>=32),
  "Id_Rol" smallint NOT NULL REFERENCES afrisan."Rol",
  "Activo" boolean NOT NULL DEFAULT true,
  "IntentosFallidos" integer NOT NULL DEFAULT 0 CHECK ("IntentosFallidos">=0),
  "BloqueadoHasta" timestamptz,
  "VersionSeguridad" uuid NOT NULL DEFAULT gen_random_uuid(),
  "FechaCreacion" timestamptz NOT NULL DEFAULT clock_timestamp()
);
CREATE UNIQUE INDEX "UX_Usuario_Correo" ON afrisan."Usuario" (lower(btrim("Correo_Usuario")));
COMMENT ON COLUMN afrisan."Usuario"."Clave_Usuario" IS 'Solo hash seguro generado por la API. La longitud no verifica algoritmo. Nunca contraseña en texto plano.';

-- El backend de confianza establece el actor validado por JWT EN CADA TRANSACCIÓN.
-- SELECT set_config('afrisan.id_usuario', :actor_autenticado::text, true);
-- Este contexto no es autenticación por sí mismo ni debe aceptarse desde el navegador.
CREATE FUNCTION afrisan.actor(roles text[]) RETURNS integer LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
DECLARE identificador integer; nombre text;
BEGIN
  identificador := nullif(current_setting('afrisan.id_usuario',true),'')::integer;
  SELECT r."Nombre_Rol" INTO nombre FROM afrisan."Usuario" u
    JOIN afrisan."Rol" r USING ("Id_Rol") WHERE u."Id_Usuario"=identificador AND u."Activo";
  IF nombre IS NULL OR NOT(nombre=ANY(roles)) THEN RAISE EXCEPTION 'Actor ausente, inactivo o sin rol para esta acción' USING ERRCODE='42501'; END IF;
  RETURN identificador;
END $$;
CREATE FUNCTION afrisan.inmutable() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN RAISE EXCEPTION 'El registro confirmado/histórico no se modifica ni elimina: %',TG_TABLE_NAME; END $$;

CREATE TABLE afrisan."Cliente" (
  "Id_Cliente" integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "RutCliente" varchar(20) NOT NULL UNIQUE CHECK (btrim("RutCliente")<>''),
  "NombreEmpresa" varchar(200) NOT NULL CHECK (btrim("NombreEmpresa")<>''), "Activo" boolean NOT NULL DEFAULT true
);
CREATE TABLE afrisan."Sucursales" (
  "Id_Sucursal" integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_Cliente" integer NOT NULL REFERENCES afrisan."Cliente",
  "NombreSucursal" varchar(200) NOT NULL CHECK (btrim("NombreSucursal")<>''),
  "Direccion" varchar(400) NOT NULL CHECK (btrim("Direccion")<>''), "Activo" boolean NOT NULL DEFAULT true,
  UNIQUE ("Id_Cliente","NombreSucursal")
);
CREATE TABLE afrisan."Refrigerantes" (
  "Id_Refrigerante" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "CodigoRefrigerante" varchar(40) NOT NULL CHECK (btrim("CodigoRefrigerante")<>''),
  "NombreRefrigerante" varchar(150) NOT NULL CHECK (btrim("NombreRefrigerante")<>''),
  "Activo" boolean NOT NULL DEFAULT true
);
CREATE UNIQUE INDEX "UX_Refrigerante_Codigo" ON afrisan."Refrigerantes" (upper(btrim("CodigoRefrigerante")));
CREATE TABLE afrisan."Cilindros" (
  "Id_Cilindro" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "CilindroQR" varchar(120) NOT NULL UNIQUE CHECK (btrim("CilindroQR")<>''), "NumeroSerie" varchar(120),
  "Id_Refrigerante" bigint NOT NULL REFERENCES afrisan."Refrigerantes",
  "Capacidadgr" bigint NOT NULL CHECK ("Capacidadgr">0), "TaraGr" bigint NOT NULL CHECK ("TaraGr">0),
  "Ubicacion" varchar(30) NOT NULL DEFAULT 'Bodega' CHECK ("Ubicacion" IN ('Bodega','Sucursal','Tecnico','FueraServicio')),
  "Id_Sucursal" integer REFERENCES afrisan."Sucursales", "Id_Tecnico" integer REFERENCES afrisan."Usuario",
  "Estado" varchar(40) NOT NULL DEFAULT 'PendientePesaje' CHECK ("Estado" IN ('PendientePesaje','Disponible','Vacio','EnProcesoDespacho','Instalado','RetiradoPendienteRecepcion','Chatarra','DisposicionFinalCertificada','Inactivo')),
  "Id_UltimoPesaje" uuid, "FechaRegistro" timestamptz NOT NULL DEFAULT clock_timestamp(),
  "Id_Registrador" integer NOT NULL REFERENCES afrisan."Usuario",
  CHECK (("Ubicacion"='Sucursal')=("Id_Sucursal" IS NOT NULL)),
  CHECK (("Ubicacion"='Tecnico')=("Id_Tecnico" IS NOT NULL)),
  CHECK (("Ubicacion"='Bodega' AND "Estado" IN ('PendientePesaje','Disponible','Vacio')) OR
         ("Ubicacion"='Sucursal' AND "Estado"='Instalado') OR
         ("Ubicacion"='Tecnico' AND "Estado" IN ('EnProcesoDespacho','RetiradoPendienteRecepcion')) OR
         ("Ubicacion"='FueraServicio' AND "Estado" IN ('Chatarra','DisposicionFinalCertificada','Inactivo')))
);
CREATE UNIQUE INDEX "UX_Cilindro_Serie" ON afrisan."Cilindros" (upper(btrim("NumeroSerie"))) WHERE "NumeroSerie" IS NOT NULL;
CREATE TABLE afrisan."OrdenTrabajo" (
  "Id_OrdenTrabajo" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_Publico" uuid NOT NULL UNIQUE DEFAULT gen_random_uuid(),
  "CodigoOT" varchar(100) NOT NULL UNIQUE CHECK (btrim("CodigoOT")<>''),
  "Origen" varchar(20) NOT NULL DEFAULT 'Local' CHECK ("Origen" IN ('Local','Defontana')),
  "CodigoExterno" varchar(100), "Id_Sucursal" integer NOT NULL REFERENCES afrisan."Sucursales",
  "Id_Tecnico" integer NOT NULL REFERENCES afrisan."Usuario", "Id_Autorizador" integer NOT NULL REFERENCES afrisan."Usuario",
  "Descripcion" varchar(2000) NOT NULL CHECK (btrim("Descripcion")<>''),
  "Estado" varchar(40) NOT NULL DEFAULT 'Asignada' CHECK ("Estado" IN ('Asignada','EnProcesoDespacho','EnTerreno','PendienteBodega','PendienteRevision','Cerrada')),
  "FechaCreacion" timestamptz NOT NULL DEFAULT clock_timestamp(), "FechaTerminoTecnico" timestamptz,
  "ObservacionTermino" text, "FechaCierre" timestamptz,
  CHECK ("Origen"<>'Defontana' OR coalesce(btrim("CodigoExterno"),'')<>''),
  CHECK (("Estado"='Cerrada')=("FechaCierre" IS NOT NULL)),
  CHECK ("Estado" NOT IN ('PendienteBodega','PendienteRevision','Cerrada') OR "FechaTerminoTecnico" IS NOT NULL)
);
CREATE UNIQUE INDEX "UX_OT_Externa" ON afrisan."OrdenTrabajo" ("Origen","CodigoExterno") WHERE "CodigoExterno" IS NOT NULL;
CREATE TABLE afrisan."DetalleOT" (
  "Id_OrdenTrabajo" bigint NOT NULL REFERENCES afrisan."OrdenTrabajo",
  "Id_Refrigerante" bigint NOT NULL REFERENCES afrisan."Refrigerantes",
  "GrSolicitados" bigint NOT NULL CHECK ("GrSolicitados">0), "GrAutorizados" bigint NOT NULL CHECK ("GrAutorizados">0),
  PRIMARY KEY ("Id_OrdenTrabajo","Id_Refrigerante")
);
CREATE TABLE afrisan."RetiroPrevisto" (
  "Id_OrdenTrabajo" bigint NOT NULL REFERENCES afrisan."OrdenTrabajo",
  "Id_Cilindro" bigint NOT NULL REFERENCES afrisan."Cilindros",
  "Estado" varchar(20) NOT NULL DEFAULT 'Reservado' CHECK ("Estado" IN ('Reservado','Ejecutado','NoEjecutado')),
  PRIMARY KEY ("Id_OrdenTrabajo","Id_Cilindro")
);
CREATE UNIQUE INDEX "UX_Retiro_Reserva" ON afrisan."RetiroPrevisto" ("Id_Cilindro") WHERE "Estado"='Reservado';

CREATE TABLE afrisan."Pesaje" (
  "Id_Pesaje" uuid PRIMARY KEY DEFAULT gen_random_uuid(), "Id_Cilindro" bigint NOT NULL REFERENCES afrisan."Cilindros",
  "Id_OrdenTrabajo" bigint REFERENCES afrisan."OrdenTrabajo",
  "Tipo" varchar(20) NOT NULL CHECK ("Tipo" IN ('Inicial','Despacho','Retirado','Instalado','Recepcion')),
  "PesoBrutoGr" bigint NOT NULL CHECK ("PesoBrutoGr">=0),
  "Id_Usuario" integer NOT NULL REFERENCES afrisan."Usuario", "Fecha" timestamptz NOT NULL DEFAULT clock_timestamp(),
  "Observacion" text,
  CHECK (("Tipo"='Inicial')=("Id_OrdenTrabajo" IS NULL)), UNIQUE ("Id_Pesaje","Id_Cilindro")
);
CREATE UNIQUE INDEX "UX_Pesaje_Inicial" ON afrisan."Pesaje" ("Id_Cilindro") WHERE "Tipo"='Inicial';
ALTER TABLE afrisan."Cilindros" ADD FOREIGN KEY ("Id_UltimoPesaje","Id_Cilindro") REFERENCES afrisan."Pesaje" ("Id_Pesaje","Id_Cilindro");
CREATE TABLE afrisan."EvidenciaPesaje" (
  "Id_Evidencia" uuid PRIMARY KEY DEFAULT gen_random_uuid(), "Id_Pesaje" uuid NOT NULL REFERENCES afrisan."Pesaje",
  "BucketPrivado" varchar(100) NOT NULL CHECK (btrim("BucketPrivado")<>''),
  "RutaObjeto" text NOT NULL CHECK (btrim("RutaObjeto")<>'' AND "RutaObjeto" !~* '^https?://' AND "RutaObjeto" !~ '(^|/)\.\.(/|$)'),
  "NombreArchivo" varchar(150) NOT NULL CHECK (btrim("NombreArchivo")<>''),
  "ContentType" varchar(30) NOT NULL CHECK ("ContentType" IN ('image/jpeg','image/png','image/webp')),
  "Bytes" bigint NOT NULL CHECK ("Bytes" BETWEEN 12 AND 4194304),
  "SHA256" varchar(64) NOT NULL CHECK ("SHA256" ~ '^[0-9a-f]{64}$'),
  "Id_Usuario" integer NOT NULL REFERENCES afrisan."Usuario", "Fecha" timestamptz NOT NULL DEFAULT clock_timestamp(),
  UNIQUE ("Id_Pesaje","SHA256"), UNIQUE ("BucketPrivado","RutaObjeto")
);
COMMENT ON TABLE afrisan."EvidenciaPesaje" IS 'Metadatos de objeto privado. La API verifica archivo real, firma, hash y acceso Storage antes de confirmar; la ruta no acredita una foto.';
CREATE TABLE afrisan."Despacho" (
  "Id_Despacho" uuid PRIMARY KEY, "Id_OrdenTrabajo" bigint NOT NULL REFERENCES afrisan."OrdenTrabajo",
  "Id_Cilindro" bigint NOT NULL REFERENCES afrisan."Cilindros", "Id_Pesaje" uuid NOT NULL UNIQUE,
  "Id_Tecnico" integer NOT NULL REFERENCES afrisan."Usuario", "Fecha" timestamptz NOT NULL DEFAULT clock_timestamp(),
  "Estado" varchar(25) NOT NULL DEFAULT 'EnProcesoDespacho' CHECK ("Estado" IN ('EnProcesoDespacho','Instalado','Devuelto')),
  "HuellaSolicitud" text NOT NULL,
  FOREIGN KEY ("Id_Pesaje","Id_Cilindro") REFERENCES afrisan."Pesaje" ("Id_Pesaje","Id_Cilindro"),
  UNIQUE ("Id_Despacho","Id_OrdenTrabajo","Id_Cilindro")
);
CREATE UNIQUE INDEX "UX_Despacho_Activo" ON afrisan."Despacho" ("Id_Cilindro") WHERE "Estado"='EnProcesoDespacho';
CREATE TABLE afrisan."OperacionTerreno" (
  "Id_Operacion" uuid PRIMARY KEY, "Id_OrdenTrabajo" bigint NOT NULL REFERENCES afrisan."OrdenTrabajo",
  "Tipo" varchar(20) NOT NULL CHECK ("Tipo" IN ('Reemplazo','Agregado')),
  "Id_Instalado" bigint REFERENCES afrisan."Cilindros", "Id_Retirado" bigint REFERENCES afrisan."Cilindros",
  "Id_Despacho" uuid, "Id_PesajeInstalado" uuid UNIQUE, "Id_PesajeRetirado" uuid UNIQUE,
  "Estado" varchar(20) NOT NULL DEFAULT 'Borrador' CHECK ("Estado" IN ('Borrador','Registrada','Descartada')),
  "Version" integer NOT NULL DEFAULT 1 CHECK ("Version">0),
  "Id_Tecnico" integer NOT NULL REFERENCES afrisan."Usuario", "Observacion" text,
  "FechaCreacionLocal" timestamptz, "Dispositivo" varchar(150),
  "FechaRecepcion" timestamptz NOT NULL DEFAULT clock_timestamp(), "FechaConfirmacion" timestamptz,
  "FechaEntregaInformada" timestamptz, "Id_EntregaInformadaPor" integer REFERENCES afrisan."Usuario",
  FOREIGN KEY ("Id_Despacho","Id_OrdenTrabajo","Id_Instalado") REFERENCES afrisan."Despacho" ("Id_Despacho","Id_OrdenTrabajo","Id_Cilindro"),
  FOREIGN KEY ("Id_PesajeInstalado","Id_Instalado") REFERENCES afrisan."Pesaje" ("Id_Pesaje","Id_Cilindro"),
  FOREIGN KEY ("Id_PesajeRetirado","Id_Retirado") REFERENCES afrisan."Pesaje" ("Id_Pesaje","Id_Cilindro"),
  CHECK ("Tipo"<>'Agregado' OR ("Id_Retirado" IS NULL AND "Id_PesajeRetirado" IS NULL AND "FechaEntregaInformada" IS NULL)),
  CHECK ("Id_Retirado" IS NULL OR "Id_Retirado"<>"Id_Instalado"),
  CHECK ("Estado"<>'Registrada' OR ("Id_Instalado" IS NOT NULL AND "Id_Despacho" IS NOT NULL AND "Id_PesajeInstalado" IS NOT NULL AND "FechaConfirmacion" IS NOT NULL)),
  CHECK ("Estado"<>'Registrada' OR "Tipo"<>'Reemplazo' OR ("Id_Retirado" IS NOT NULL AND "Id_PesajeRetirado" IS NOT NULL)),
  CHECK (("FechaEntregaInformada" IS NULL)=("Id_EntregaInformadaPor" IS NULL))
);
CREATE UNIQUE INDEX "UX_Operacion_Instalado" ON afrisan."OperacionTerreno" ("Id_OrdenTrabajo","Id_Instalado") WHERE "Estado"='Registrada';
CREATE UNIQUE INDEX "UX_Operacion_Retirado" ON afrisan."OperacionTerreno" ("Id_OrdenTrabajo","Id_Retirado") WHERE "Estado"='Registrada';
CREATE UNIQUE INDEX "UX_Operacion_Despacho" ON afrisan."OperacionTerreno" ("Id_Despacho") WHERE "Estado"='Registrada';
CREATE TABLE afrisan."CustodiaActual" (
  "Id_Cilindro" bigint PRIMARY KEY REFERENCES afrisan."Cilindros",
  "Id_OrdenTrabajo" bigint NOT NULL REFERENCES afrisan."OrdenTrabajo", "Id_Tecnico" integer NOT NULL REFERENCES afrisan."Usuario",
  "Id_Despacho" uuid REFERENCES afrisan."Despacho", "Id_Operacion" uuid REFERENCES afrisan."OperacionTerreno",
  CHECK (("Id_Despacho" IS NULL)<>("Id_Operacion" IS NULL))
);
CREATE TABLE afrisan."RecepcionBodega" (
  "Id_Recepcion" uuid PRIMARY KEY DEFAULT gen_random_uuid(), "Id_OrdenTrabajo" bigint NOT NULL REFERENCES afrisan."OrdenTrabajo",
  "Id_Cilindro" bigint NOT NULL REFERENCES afrisan."Cilindros", "Id_Operacion" uuid UNIQUE REFERENCES afrisan."OperacionTerreno",
  "Id_DespachoSinUso" uuid UNIQUE REFERENCES afrisan."Despacho", "Id_Pesaje" uuid NOT NULL UNIQUE,
  "Id_Bodeguero" integer NOT NULL REFERENCES afrisan."Usuario", "Fecha" timestamptz NOT NULL DEFAULT clock_timestamp(), "Observacion" text,
  CHECK (("Id_Operacion" IS NULL)<>("Id_DespachoSinUso" IS NULL)),
  FOREIGN KEY ("Id_Pesaje","Id_Cilindro") REFERENCES afrisan."Pesaje" ("Id_Pesaje","Id_Cilindro")
);
CREATE TABLE afrisan."RevisionOT" (
  "Id_Revision" uuid PRIMARY KEY DEFAULT gen_random_uuid(), "Id_OrdenTrabajo" bigint NOT NULL REFERENCES afrisan."OrdenTrabajo",
  "Etapa" varchar(20) NOT NULL CHECK ("Etapa" IN ('Bodega','Autorizador')),
  "Id_Usuario" integer NOT NULL REFERENCES afrisan."Usuario", "Fecha" timestamptz NOT NULL DEFAULT clock_timestamp(),
  "ConfirmoRevision" boolean NOT NULL CHECK ("ConfirmoRevision"), "Observacion" text,
  UNIQUE ("Id_OrdenTrabajo","Etapa")
);
CREATE TABLE afrisan."Incidencia" (
  "Id_Incidencia" uuid PRIMARY KEY DEFAULT gen_random_uuid(), "Id_OrdenTrabajo" bigint NOT NULL REFERENCES afrisan."OrdenTrabajo",
  "Id_Operacion" uuid REFERENCES afrisan."OperacionTerreno",
  "Tipo" varchar(20) NOT NULL CHECK ("Tipo" IN ('Incidencia','Rectificacion')), "Motivo" text NOT NULL CHECK (length(btrim("Motivo"))>=5),
  "Id_Usuario" integer NOT NULL REFERENCES afrisan."Usuario", "Fecha" timestamptz NOT NULL DEFAULT clock_timestamp(),
  "Respuesta" text, "Id_Revisor" integer REFERENCES afrisan."Usuario", "FechaRevision" timestamptz,
  CHECK (("Respuesta" IS NULL AND "Id_Revisor" IS NULL AND "FechaRevision" IS NULL) OR
         ("Respuesta" IS NOT NULL AND length(btrim("Respuesta"))>=5 AND "Id_Revisor" IS NOT NULL AND "FechaRevision" IS NOT NULL))
);
CREATE TABLE afrisan."HistorialCilindro" (
  "Id_Historial" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY, "Id_Cilindro" bigint NOT NULL REFERENCES afrisan."Cilindros",
  "Ubicacion" text NOT NULL, "Estado" text NOT NULL, "Id_Sucursal" integer REFERENCES afrisan."Sucursales",
  "Id_Tecnico" integer REFERENCES afrisan."Usuario", "Id_Pesaje" uuid REFERENCES afrisan."Pesaje",
  "Id_Usuario" integer NOT NULL REFERENCES afrisan."Usuario", "Fecha" timestamptz NOT NULL DEFAULT clock_timestamp()
);
CREATE TABLE afrisan."EventoOT" (
  "Id_Evento" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY, "Id_OrdenTrabajo" bigint NOT NULL REFERENCES afrisan."OrdenTrabajo",
  "Accion" text NOT NULL, "Id_Usuario" integer NOT NULL REFERENCES afrisan."Usuario", "Fecha" timestamptz NOT NULL DEFAULT clock_timestamp(), "Detalle" text
);

CREATE FUNCTION afrisan.ot_actor(id_ot bigint, roles text[], editable boolean DEFAULT false) RETURNS afrisan."OrdenTrabajo"
LANGUAGE plpgsql SET search_path=pg_catalog,afrisan AS $$
DECLARE o afrisan."OrdenTrabajo"; a integer;
BEGIN
  a:=afrisan.actor(roles);
  SELECT * INTO STRICT o FROM afrisan."OrdenTrabajo" WHERE "Id_OrdenTrabajo"=id_ot FOR UPDATE;
  IF 'Tecnico'=ANY(roles) AND a<>o."Id_Tecnico" AND EXISTS(SELECT 1 FROM afrisan."Usuario" u JOIN afrisan."Rol" r USING("Id_Rol") WHERE u."Id_Usuario"=a AND r."Nombre_Rol"='Tecnico') THEN RAISE EXCEPTION 'OT de otro técnico'; END IF;
  IF editable AND o."Estado" NOT IN ('Asignada','EnProcesoDespacho','EnTerreno') THEN RAISE EXCEPTION 'La OT ya se envió a revisión'; END IF;
  RETURN o;
END $$;
CREATE FUNCTION afrisan.peso_valido(id_c bigint,peso bigint) RETURNS void LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
DECLARE c afrisan."Cilindros";
BEGIN SELECT * INTO STRICT c FROM afrisan."Cilindros" WHERE "Id_Cilindro"=id_c;
  IF peso<c."TaraGr" OR peso::numeric>c."TaraGr"::numeric+c."Capacidadgr" THEN RAISE EXCEPTION 'Peso fuera de tara/capacidad'; END IF;
END $$;
CREATE FUNCTION afrisan.cilindro_guardar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
BEGIN
  IF TG_OP='INSERT' THEN
    NEW."Id_Registrador":=afrisan.actor(ARRAY['Bodega','Administrador']); NEW."FechaRegistro":=clock_timestamp();
    IF NEW."Ubicacion" NOT IN ('Bodega','Sucursal') OR NEW."Id_UltimoPesaje" IS NOT NULL THEN RAISE EXCEPTION 'Alta solo en Bodega/Sucursal y sin pesaje ficticio'; END IF;
    NEW."Estado":=CASE WHEN NEW."Ubicacion"='Sucursal' THEN 'Instalado' ELSE 'PendientePesaje' END;
  ELSE
    IF (NEW."Id_Refrigerante",NEW."TaraGr",NEW."Capacidadgr",NEW."CilindroQR",NEW."Id_Registrador",NEW."FechaRegistro") IS DISTINCT FROM
       (OLD."Id_Refrigerante",OLD."TaraGr",OLD."Capacidadgr",OLD."CilindroQR",OLD."Id_Registrador",OLD."FechaRegistro") THEN RAISE EXCEPTION 'Identidad/tara/capacidad no se reescriben'; END IF;
    IF pg_trigger_depth()<2 THEN RAISE EXCEPTION 'Ubicación/peso se cambian mediante los registros operativos'; END IF;
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER "Cilindro_Validar" BEFORE INSERT OR UPDATE ON afrisan."Cilindros" FOR EACH ROW EXECUTE FUNCTION afrisan.cilindro_guardar();
CREATE FUNCTION afrisan.cilindro_historial() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
BEGIN
  INSERT INTO afrisan."HistorialCilindro"("Id_Cilindro","Ubicacion","Estado","Id_Sucursal","Id_Tecnico","Id_Pesaje","Id_Usuario")
  VALUES(NEW."Id_Cilindro",NEW."Ubicacion",NEW."Estado",NEW."Id_Sucursal",NEW."Id_Tecnico",NEW."Id_UltimoPesaje",afrisan.actor(ARRAY['Bodega','Tecnico','Administrador']));
  RETURN NULL;
END $$;
CREATE TRIGGER "Cilindro_Historial" AFTER INSERT OR UPDATE ON afrisan."Cilindros" FOR EACH ROW EXECUTE FUNCTION afrisan.cilindro_historial();

CREATE FUNCTION afrisan.pesaje_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
DECLARE c afrisan."Cilindros"; o afrisan."OrdenTrabajo";
BEGIN
  IF NEW."Tipo"='Inicial' THEN NEW."Id_Usuario":=afrisan.actor(ARRAY['Bodega','Administrador']);
  ELSIF NEW."Tipo"='Recepcion' THEN o:=afrisan.ot_actor(NEW."Id_OrdenTrabajo",ARRAY['Bodega']); NEW."Id_Usuario":=afrisan.actor(ARRAY['Bodega']);
  ELSE o:=afrisan.ot_actor(NEW."Id_OrdenTrabajo",ARRAY['Tecnico'],true); NEW."Id_Usuario":=o."Id_Tecnico"; END IF;
  SELECT * INTO STRICT c FROM afrisan."Cilindros" WHERE "Id_Cilindro"=NEW."Id_Cilindro" FOR UPDATE;
  PERFORM afrisan.peso_valido(c."Id_Cilindro",NEW."PesoBrutoGr");
  IF NEW."Tipo"='Inicial' AND (c."Id_UltimoPesaje" IS NOT NULL OR EXISTS(SELECT 1 FROM afrisan."Pesaje" WHERE "Id_Cilindro"=c."Id_Cilindro")) THEN RAISE EXCEPTION 'Ya existe pesaje inicial u operativo'; END IF;
  IF NEW."Tipo"='Recepcion' AND (o."Estado"='Cerrada' OR EXISTS(SELECT 1 FROM afrisan."RevisionOT" WHERE "Id_OrdenTrabajo"=o."Id_OrdenTrabajo")) THEN RAISE EXCEPTION 'Recepción ya revisada/cerrada'; END IF;
  NEW."Fecha":=clock_timestamp(); RETURN NEW;
END $$;
CREATE TRIGGER "Pesaje_Validar" BEFORE INSERT ON afrisan."Pesaje" FOR EACH ROW EXECUTE FUNCTION afrisan.pesaje_validar();
CREATE FUNCTION afrisan.pesaje_inicial_aplicar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
BEGIN
  IF NEW."Tipo"='Inicial' THEN
    UPDATE afrisan."Cilindros" SET "Id_UltimoPesaje"=NEW."Id_Pesaje", "Estado"=CASE WHEN "Ubicacion"='Sucursal' THEN 'Instalado' WHEN NEW."PesoBrutoGr"="TaraGr" THEN 'Vacio' ELSE 'Disponible' END WHERE "Id_Cilindro"=NEW."Id_Cilindro";
  END IF; RETURN NULL;
END $$;
CREATE TRIGGER "Pesaje_Inicial_Aplicar" AFTER INSERT ON afrisan."Pesaje" FOR EACH ROW EXECUTE FUNCTION afrisan.pesaje_inicial_aplicar();
CREATE FUNCTION afrisan.evidencia_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
DECLARE p afrisan."Pesaje"; o afrisan."OrdenTrabajo";
BEGIN
  SELECT * INTO STRICT p FROM afrisan."Pesaje" WHERE "Id_Pesaje"=NEW."Id_Pesaje";
  IF p."Id_OrdenTrabajo" IS NOT NULL THEN
    o:=afrisan.ot_actor(p."Id_OrdenTrabajo",CASE WHEN p."Tipo"='Recepcion' THEN ARRAY['Bodega'] ELSE ARRAY['Tecnico'] END,p."Tipo"<>'Recepcion');
    IF EXISTS(SELECT 1 FROM afrisan."RevisionOT" WHERE "Id_OrdenTrabajo"=p."Id_OrdenTrabajo") THEN RAISE EXCEPTION 'No se agregan fotos después de la revisión'; END IF;
  END IF;
  SELECT * INTO STRICT p FROM afrisan."Pesaje" WHERE "Id_Pesaje"=NEW."Id_Pesaje" FOR UPDATE;
  NEW."Id_Usuario":=afrisan.actor(CASE WHEN p."Tipo" IN ('Inicial','Recepcion') THEN ARRAY['Bodega','Administrador'] ELSE ARRAY['Tecnico'] END);
  IF p."Id_Usuario"<>NEW."Id_Usuario" THEN RAISE EXCEPTION 'La evidencia pertenece a otro registrador'; END IF;
  IF (SELECT count(*) FROM afrisan."EvidenciaPesaje" WHERE "Id_Pesaje"=p."Id_Pesaje")>=8 THEN RAISE EXCEPTION 'Máximo ocho fotos por pesaje'; END IF;
  IF EXISTS(SELECT 1 FROM afrisan."Despacho" WHERE "Id_Pesaje"=p."Id_Pesaje") OR EXISTS(SELECT 1 FROM afrisan."RecepcionBodega" WHERE "Id_Pesaje"=p."Id_Pesaje") OR
     EXISTS(SELECT 1 FROM afrisan."OperacionTerreno" WHERE "Estado"='Registrada' AND p."Id_Pesaje" IN ("Id_PesajeRetirado","Id_PesajeInstalado")) THEN RAISE EXCEPTION 'No se agregan fotos a pesajes confirmados'; END IF;
  NEW."Fecha":=clock_timestamp(); RETURN NEW;
END $$;
CREATE TRIGGER "Evidencia_Validar" BEFORE INSERT ON afrisan."EvidenciaPesaje" FOR EACH ROW EXECUTE FUNCTION afrisan.evidencia_validar();

CREATE FUNCTION afrisan.pedido_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
DECLARE o afrisan."OrdenTrabajo"; c afrisan."Cilindros";
BEGIN
  IF TG_OP='DELETE' THEN RAISE EXCEPTION 'No borrar el pedido; requiere procedimiento de anulación'; END IF;
  IF TG_TABLE_NAME='RetiroPrevisto' AND TG_OP='UPDATE' AND pg_trigger_depth()>1 THEN
    IF OLD."Estado"<>'Reservado' OR NEW."Estado" NOT IN ('Ejecutado','NoEjecutado') OR
       (NEW."Id_OrdenTrabajo",NEW."Id_Cilindro") IS DISTINCT FROM (OLD."Id_OrdenTrabajo",OLD."Id_Cilindro") THEN RAISE EXCEPTION 'Transición interna de reserva inválida'; END IF;
    IF NEW."Estado"='NoEjecutado' AND NOT EXISTS(SELECT 1 FROM afrisan."OrdenTrabajo" WHERE "Id_OrdenTrabajo"=NEW."Id_OrdenTrabajo" AND "Estado"='Cerrada') THEN RAISE EXCEPTION 'Solo liberar un retiro no ejecutado al cerrar'; END IF;
    RETURN NEW;
  END IF;
  o:=afrisan.ot_actor(NEW."Id_OrdenTrabajo",ARRAY['Autorizador']);
  IF o."Estado"<>'Asignada' OR TG_OP='UPDATE' THEN RAISE EXCEPTION 'Pedido autorizado inmutable; agregar detalle solo antes del despacho'; END IF;
  IF TG_TABLE_NAME='RetiroPrevisto' THEN
    SELECT * INTO STRICT c FROM afrisan."Cilindros" WHERE "Id_Cilindro"=NEW."Id_Cilindro" FOR UPDATE;
    IF c."Ubicacion"<>'Sucursal' OR c."Id_Sucursal"<>o."Id_Sucursal" OR NOT EXISTS(SELECT 1 FROM afrisan."DetalleOT" WHERE "Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND "Id_Refrigerante"=c."Id_Refrigerante") THEN RAISE EXCEPTION 'Retiro no corresponde a sucursal/gas encargado'; END IF;
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER "Detalle_Validar" BEFORE INSERT OR UPDATE OR DELETE ON afrisan."DetalleOT" FOR EACH ROW EXECUTE FUNCTION afrisan.pedido_validar();
CREATE TRIGGER "RetiroPrevisto_Validar" BEFORE INSERT OR UPDATE OR DELETE ON afrisan."RetiroPrevisto" FOR EACH ROW EXECUTE FUNCTION afrisan.pedido_validar();

CREATE FUNCTION afrisan.despacho_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
DECLARE o afrisan."OrdenTrabajo"; c afrisan."Cilindros"; p afrisan."Pesaje";
BEGIN
  IF TG_OP<>'INSERT' THEN
    IF TG_OP='DELETE' OR pg_trigger_depth()<2 OR (to_jsonb(NEW)-'Estado')<>(to_jsonb(OLD)-'Estado') OR OLD."Estado"<>'EnProcesoDespacho' THEN RAISE EXCEPTION 'Despacho confirmado no se modifica'; END IF;
    RETURN NEW;
  END IF;
  o:=afrisan.ot_actor(NEW."Id_OrdenTrabajo",ARRAY['Tecnico'],true);
  SELECT * INTO STRICT c FROM afrisan."Cilindros" WHERE "Id_Cilindro"=NEW."Id_Cilindro" FOR UPDATE;
  SELECT * INTO STRICT p FROM afrisan."Pesaje" WHERE "Id_Pesaje"=NEW."Id_Pesaje";
  IF c."Ubicacion"<>'Bodega' OR c."Estado"<>'Disponible' OR c."Id_UltimoPesaje" IS NULL OR p."PesoBrutoGr"<=c."TaraGr" THEN RAISE EXCEPTION 'Cilindro no disponible/pesado/con contenido en Bodega'; END IF;
  IF NOT EXISTS(SELECT 1 FROM afrisan."DetalleOT" WHERE "Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND "Id_Refrigerante"=c."Id_Refrigerante") THEN RAISE EXCEPTION 'Gas fuera del pedido'; END IF;
  IF p."Tipo"<>'Despacho' OR p."Id_OrdenTrabajo"<>o."Id_OrdenTrabajo" OR p."Id_Cilindro"<>c."Id_Cilindro" OR p."Id_Usuario"<>o."Id_Tecnico" THEN RAISE EXCEPTION 'Pesaje de salida ajeno'; END IF;
  IF NOT EXISTS(SELECT 1 FROM afrisan."EvidenciaPesaje" WHERE "Id_Pesaje"=p."Id_Pesaje") THEN RAISE EXCEPTION 'Falta foto del peso de salida'; END IF;
  NEW."Id_Tecnico":=o."Id_Tecnico"; NEW."Fecha":=clock_timestamp(); NEW."Estado":='EnProcesoDespacho'; RETURN NEW;
END $$;
CREATE TRIGGER "Despacho_Validar" BEFORE INSERT OR UPDATE OR DELETE ON afrisan."Despacho" FOR EACH ROW EXECUTE FUNCTION afrisan.despacho_validar();
CREATE FUNCTION afrisan.despacho_aplicar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
BEGIN
  INSERT INTO afrisan."CustodiaActual"("Id_Cilindro","Id_OrdenTrabajo","Id_Tecnico","Id_Despacho") VALUES(NEW."Id_Cilindro",NEW."Id_OrdenTrabajo",NEW."Id_Tecnico",NEW."Id_Despacho");
  UPDATE afrisan."Cilindros" SET "Ubicacion"='Tecnico',"Id_Tecnico"=NEW."Id_Tecnico","Id_Sucursal"=NULL,"Estado"='EnProcesoDespacho',"Id_UltimoPesaje"=NEW."Id_Pesaje" WHERE "Id_Cilindro"=NEW."Id_Cilindro";
  UPDATE afrisan."OrdenTrabajo" SET "Estado"='EnProcesoDespacho' WHERE "Id_OrdenTrabajo"=NEW."Id_OrdenTrabajo" AND "Estado"='Asignada';
  INSERT INTO afrisan."EventoOT"("Id_OrdenTrabajo","Accion","Id_Usuario","Detalle") VALUES(NEW."Id_OrdenTrabajo",'Retiro de Bodega por Técnico',NEW."Id_Tecnico",NEW."Id_Cilindro"::text);
  RETURN NULL;
END $$;
CREATE TRIGGER "Despacho_Aplicar" AFTER INSERT ON afrisan."Despacho" FOR EACH ROW EXECUTE FUNCTION afrisan.despacho_aplicar();

CREATE FUNCTION afrisan.operacion_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
DECLARE o afrisan."OrdenTrabajo"; n afrisan."Cilindros"; v afrisan."Cilindros"; p afrisan."Pesaje";
BEGIN
  IF TG_OP='DELETE' THEN RAISE EXCEPTION 'No borrar operaciones; descartar borrador o solicitar rectificación'; END IF;
  o:=afrisan.ot_actor(NEW."Id_OrdenTrabajo",ARRAY['Tecnico']);
  IF TG_OP='INSERT' THEN
    IF o."Estado" NOT IN ('Asignada','EnProcesoDespacho','EnTerreno') OR NEW."Estado"<>'Borrador' OR NEW."FechaEntregaInformada" IS NOT NULL THEN RAISE EXCEPTION 'Crear primero borrador editable'; END IF;
    NEW."Id_Tecnico":=o."Id_Tecnico"; NEW."Version":=1; NEW."FechaRecepcion":=clock_timestamp(); NEW."FechaConfirmacion":=NULL; RETURN NEW;
  END IF;
  IF NEW."Id_OrdenTrabajo"<>OLD."Id_OrdenTrabajo" OR NEW."Id_Operacion"<>OLD."Id_Operacion" OR NEW."Id_Tecnico"<>OLD."Id_Tecnico" OR NEW."Version"<>OLD."Version"+1 THEN RAISE EXCEPTION 'Identidad o versión de operación inválida'; END IF;
  IF NEW."FechaRecepcion" IS DISTINCT FROM OLD."FechaRecepcion" THEN RAISE EXCEPTION 'No sobrescribir la recepción del borrador'; END IF;
  IF OLD."Estado"='Registrada' THEN
    IF OLD."Tipo"<>'Reemplazo' OR OLD."FechaEntregaInformada" IS NOT NULL OR NEW."FechaEntregaInformada" IS NULL OR o."Estado" NOT IN ('EnTerreno','PendienteBodega') OR
       (to_jsonb(NEW)-ARRAY['FechaEntregaInformada','Id_EntregaInformadaPor','Version'])<>(to_jsonb(OLD)-ARRAY['FechaEntregaInformada','Id_EntregaInformadaPor','Version']) THEN RAISE EXCEPTION 'Trabajo confirmado no se sobrescribe'; END IF;
    NEW."Id_EntregaInformadaPor":=o."Id_Tecnico"; NEW."FechaEntregaInformada":=clock_timestamp(); RETURN NEW;
  END IF;
  IF OLD."Estado"='Descartada' OR o."Estado" NOT IN ('Asignada','EnProcesoDespacho','EnTerreno') THEN RAISE EXCEPTION 'Operación no editable'; END IF;
  IF NEW."FechaEntregaInformada" IS NOT NULL OR NEW."Id_EntregaInformadaPor" IS NOT NULL THEN RAISE EXCEPTION 'La devolución se informa después de confirmar el cambio'; END IF;
  IF NEW."Estado"<>'Registrada' THEN RETURN NEW; END IF;
  IF NEW."Id_Instalado" IS NULL OR NEW."Id_Despacho" IS NULL OR NEW."Id_PesajeInstalado" IS NULL THEN RAISE EXCEPTION 'Falta cilindro/entrega/pesaje instalado'; END IF;
  PERFORM 1 FROM afrisan."Cilindros" WHERE "Id_Cilindro" IN (NEW."Id_Instalado",NEW."Id_Retirado") ORDER BY "Id_Cilindro" FOR UPDATE;
  SELECT * INTO STRICT n FROM afrisan."Cilindros" WHERE "Id_Cilindro"=NEW."Id_Instalado";
  IF NOT EXISTS(SELECT 1 FROM afrisan."CustodiaActual" WHERE "Id_Cilindro"=n."Id_Cilindro" AND "Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND "Id_Despacho"=NEW."Id_Despacho") THEN RAISE EXCEPTION 'Instalado fuera de custodia de esta OT'; END IF;
  SELECT * INTO STRICT p FROM afrisan."Pesaje" WHERE "Id_Pesaje"=NEW."Id_PesajeInstalado";
  IF p."Tipo"<>'Instalado' OR p."Id_OrdenTrabajo"<>o."Id_OrdenTrabajo" OR p."Id_Cilindro"<>n."Id_Cilindro" THEN RAISE EXCEPTION 'Pesaje instalado incompatible'; END IF;
  IF NOT EXISTS(SELECT 1 FROM afrisan."EvidenciaPesaje" WHERE "Id_Pesaje"=p."Id_Pesaje") THEN RAISE EXCEPTION 'Falta foto del instalado'; END IF;
  IF NEW."Tipo"='Reemplazo' THEN
    IF NEW."Id_Retirado" IS NULL OR NEW."Id_PesajeRetirado" IS NULL THEN RAISE EXCEPTION 'Falta cilindro/pesaje retirado'; END IF;
    SELECT * INTO STRICT v FROM afrisan."Cilindros" WHERE "Id_Cilindro"=NEW."Id_Retirado";
    IF v."Ubicacion"<>'Sucursal' OR v."Id_Sucursal"<>o."Id_Sucursal" OR v."Id_Refrigerante"<>n."Id_Refrigerante" OR NOT EXISTS(SELECT 1 FROM afrisan."RetiroPrevisto" WHERE "Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND "Id_Cilindro"=v."Id_Cilindro" AND "Estado"='Reservado') THEN RAISE EXCEPTION 'Retirado de otra sucursal, gas o pedido'; END IF;
    SELECT * INTO STRICT p FROM afrisan."Pesaje" WHERE "Id_Pesaje"=NEW."Id_PesajeRetirado";
    IF p."Tipo"<>'Retirado' OR p."Id_OrdenTrabajo"<>o."Id_OrdenTrabajo" OR p."Id_Cilindro"<>v."Id_Cilindro" THEN RAISE EXCEPTION 'Pesaje retirado incompatible'; END IF;
    IF NOT EXISTS(SELECT 1 FROM afrisan."EvidenciaPesaje" WHERE "Id_Pesaje"=p."Id_Pesaje") THEN RAISE EXCEPTION 'Falta foto del retirado'; END IF;
    IF EXISTS(SELECT 1 FROM afrisan."EvidenciaPesaje" a JOIN afrisan."EvidenciaPesaje" b USING("SHA256") WHERE a."Id_Pesaje"=NEW."Id_PesajeRetirado" AND b."Id_Pesaje"=NEW."Id_PesajeInstalado") THEN RAISE EXCEPTION 'No reutilizar la misma foto para ambos cilindros'; END IF;
  END IF;
  NEW."FechaConfirmacion":=clock_timestamp(); RETURN NEW;
END $$;
CREATE TRIGGER "Operacion_Validar" BEFORE INSERT OR UPDATE OR DELETE ON afrisan."OperacionTerreno" FOR EACH ROW EXECUTE FUNCTION afrisan.operacion_validar();
CREATE FUNCTION afrisan.operacion_aplicar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
DECLARE o afrisan."OrdenTrabajo";
BEGIN
  IF NEW."Estado"<>'Registrada' OR OLD."Estado"='Registrada' THEN RETURN NULL; END IF;
  SELECT * INTO STRICT o FROM afrisan."OrdenTrabajo" WHERE "Id_OrdenTrabajo"=NEW."Id_OrdenTrabajo";
  DELETE FROM afrisan."CustodiaActual" WHERE "Id_Cilindro"=NEW."Id_Instalado";
  UPDATE afrisan."Despacho" SET "Estado"='Instalado' WHERE "Id_Despacho"=NEW."Id_Despacho";
  UPDATE afrisan."Cilindros" SET "Ubicacion"='Sucursal',"Id_Sucursal"=o."Id_Sucursal","Id_Tecnico"=NULL,"Estado"='Instalado',"Id_UltimoPesaje"=NEW."Id_PesajeInstalado" WHERE "Id_Cilindro"=NEW."Id_Instalado";
  IF NEW."Tipo"='Reemplazo' THEN
    INSERT INTO afrisan."CustodiaActual"("Id_Cilindro","Id_OrdenTrabajo","Id_Tecnico","Id_Operacion") VALUES(NEW."Id_Retirado",NEW."Id_OrdenTrabajo",NEW."Id_Tecnico",NEW."Id_Operacion");
    UPDATE afrisan."Cilindros" SET "Ubicacion"='Tecnico',"Id_Sucursal"=NULL,"Id_Tecnico"=NEW."Id_Tecnico","Estado"='RetiradoPendienteRecepcion',"Id_UltimoPesaje"=NEW."Id_PesajeRetirado" WHERE "Id_Cilindro"=NEW."Id_Retirado";
    UPDATE afrisan."RetiroPrevisto" SET "Estado"='Ejecutado' WHERE "Id_OrdenTrabajo"=NEW."Id_OrdenTrabajo" AND "Id_Cilindro"=NEW."Id_Retirado";
  END IF;
  UPDATE afrisan."OrdenTrabajo" SET "Estado"='EnTerreno' WHERE "Id_OrdenTrabajo"=NEW."Id_OrdenTrabajo" AND "Estado"<>'EnTerreno';
  INSERT INTO afrisan."EventoOT"("Id_OrdenTrabajo","Accion","Id_Usuario","Detalle") VALUES(NEW."Id_OrdenTrabajo",'Cambio físico confirmado',NEW."Id_Tecnico",NEW."Id_Operacion"::text);
  RETURN NULL;
END $$;
CREATE TRIGGER "Operacion_Aplicar" AFTER UPDATE ON afrisan."OperacionTerreno" FOR EACH ROW EXECUTE FUNCTION afrisan.operacion_aplicar();

CREATE FUNCTION afrisan.recepcion_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
DECLARE o afrisan."OrdenTrabajo"; p afrisan."Pesaje";
BEGIN
  o:=afrisan.ot_actor(NEW."Id_OrdenTrabajo",ARRAY['Bodega']);
  IF o."Estado"='Cerrada' OR EXISTS(SELECT 1 FROM afrisan."RevisionOT" WHERE "Id_OrdenTrabajo"=o."Id_OrdenTrabajo") THEN RAISE EXCEPTION 'OT ya aprobada'; END IF;
  PERFORM 1 FROM afrisan."Cilindros" WHERE "Id_Cilindro"=NEW."Id_Cilindro" FOR UPDATE;
  IF NOT EXISTS(SELECT 1 FROM afrisan."CustodiaActual" WHERE "Id_Cilindro"=NEW."Id_Cilindro" AND "Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND "Id_Tecnico"=o."Id_Tecnico") THEN RAISE EXCEPTION 'Cilindro fuera de custodia de esta OT'; END IF;
  SELECT * INTO STRICT p FROM afrisan."Pesaje" WHERE "Id_Pesaje"=NEW."Id_Pesaje";
  IF p."Tipo"<>'Recepcion' OR p."Id_OrdenTrabajo"<>o."Id_OrdenTrabajo" OR p."Id_Cilindro"<>NEW."Id_Cilindro" OR p."Id_Usuario"<>afrisan.actor(ARRAY['Bodega']) THEN RAISE EXCEPTION 'Pesaje de recepción incompatible'; END IF;
  IF NEW."Id_Operacion" IS NOT NULL THEN
    IF NOT EXISTS(SELECT 1 FROM afrisan."OperacionTerreno" WHERE "Id_Operacion"=NEW."Id_Operacion" AND "Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND "Estado"='Registrada' AND "Id_Retirado"=NEW."Id_Cilindro" AND "FechaEntregaInformada" IS NOT NULL) THEN RAISE EXCEPTION 'Falta devolución informada del retirado'; END IF;
  ELSE
    IF NOT EXISTS(SELECT 1 FROM afrisan."Despacho" WHERE "Id_Despacho"=NEW."Id_DespachoSinUso" AND "Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND "Id_Cilindro"=NEW."Id_Cilindro" AND "Estado"='EnProcesoDespacho') THEN RAISE EXCEPTION 'No corresponde a devolución sin uso'; END IF;
  END IF;
  NEW."Id_Bodeguero":=afrisan.actor(ARRAY['Bodega']); NEW."Fecha":=clock_timestamp(); RETURN NEW;
END $$;
CREATE TRIGGER "Recepcion_Validar" BEFORE INSERT ON afrisan."RecepcionBodega" FOR EACH ROW EXECUTE FUNCTION afrisan.recepcion_validar();
CREATE FUNCTION afrisan.recepcion_aplicar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
BEGIN
  DELETE FROM afrisan."CustodiaActual" WHERE "Id_Cilindro"=NEW."Id_Cilindro";
  UPDATE afrisan."Cilindros" c SET "Ubicacion"='Bodega',"Id_Sucursal"=NULL,"Id_Tecnico"=NULL,"Id_UltimoPesaje"=NEW."Id_Pesaje",
    "Estado"=CASE WHEN p."PesoBrutoGr"=c."TaraGr" THEN 'Vacio' ELSE 'Disponible' END FROM afrisan."Pesaje" p WHERE c."Id_Cilindro"=NEW."Id_Cilindro" AND p."Id_Pesaje"=NEW."Id_Pesaje";
  IF NEW."Id_DespachoSinUso" IS NOT NULL THEN UPDATE afrisan."Despacho" SET "Estado"='Devuelto' WHERE "Id_Despacho"=NEW."Id_DespachoSinUso"; END IF;
  INSERT INTO afrisan."EventoOT"("Id_OrdenTrabajo","Accion","Id_Usuario","Detalle") VALUES(NEW."Id_OrdenTrabajo",'Recepción oficial Bodega',NEW."Id_Bodeguero",NEW."Id_Cilindro"::text);
  RETURN NULL;
END $$;
CREATE TRIGGER "Recepcion_Aplicar" AFTER INSERT ON afrisan."RecepcionBodega" FOR EACH ROW EXECUTE FUNCTION afrisan.recepcion_aplicar();

CREATE FUNCTION afrisan.sin_pendientes(id_ot bigint) RETURNS void LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
BEGIN
  IF NOT EXISTS(SELECT 1 FROM afrisan."OperacionTerreno" WHERE "Id_OrdenTrabajo"=id_ot AND "Estado"='Registrada') OR
     EXISTS(SELECT 1 FROM afrisan."OperacionTerreno" WHERE "Id_OrdenTrabajo"=id_ot AND "Estado"='Borrador') THEN RAISE EXCEPTION 'Falta trabajo confirmado o hay borradores pendientes'; END IF;
  IF EXISTS(SELECT 1 FROM afrisan."Despacho" WHERE "Id_OrdenTrabajo"=id_ot AND "Estado"='EnProcesoDespacho') THEN RAISE EXCEPTION 'Hay cilindros sin instalar ni devolver'; END IF;
  IF EXISTS(SELECT 1 FROM afrisan."OperacionTerreno" op WHERE op."Id_OrdenTrabajo"=id_ot AND op."Estado"='Registrada' AND op."Tipo"='Reemplazo' AND NOT EXISTS(SELECT 1 FROM afrisan."RecepcionBodega" r WHERE r."Id_Operacion"=op."Id_Operacion")) THEN RAISE EXCEPTION 'Hay retirados sin recepción de Bodega'; END IF;
END $$;
CREATE FUNCTION afrisan.revision_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
DECLARE o afrisan."OrdenTrabajo";
BEGIN
  o:=afrisan.ot_actor(NEW."Id_OrdenTrabajo",ARRAY[NEW."Etapa"]);
  PERFORM afrisan.sin_pendientes(o."Id_OrdenTrabajo");
  IF NEW."Etapa"='Bodega' AND o."Estado"<>'PendienteBodega' THEN RAISE EXCEPTION 'Primero término técnico'; END IF;
  IF NEW."Etapa"='Autorizador' THEN
    IF o."Estado"<>'PendienteRevision' OR NOT EXISTS(SELECT 1 FROM afrisan."RevisionOT" WHERE "Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND "Etapa"='Bodega') THEN RAISE EXCEPTION 'Primero aprobación de Bodega'; END IF;
    IF EXISTS(SELECT 1 FROM afrisan."Incidencia" WHERE "Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND "Respuesta" IS NULL) THEN RAISE EXCEPTION 'Incidencias sin revisar'; END IF;
  END IF;
  NEW."Id_Usuario":=afrisan.actor(ARRAY[NEW."Etapa"]); NEW."Fecha":=clock_timestamp(); RETURN NEW;
END $$;
CREATE TRIGGER "Revision_Validar" BEFORE INSERT ON afrisan."RevisionOT" FOR EACH ROW EXECUTE FUNCTION afrisan.revision_validar();
CREATE FUNCTION afrisan.revision_aplicar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
BEGIN
  IF NEW."Etapa"='Bodega' THEN UPDATE afrisan."OrdenTrabajo" SET "Estado"='PendienteRevision' WHERE "Id_OrdenTrabajo"=NEW."Id_OrdenTrabajo"; END IF;
  INSERT INTO afrisan."EventoOT"("Id_OrdenTrabajo","Accion","Id_Usuario","Detalle") VALUES(NEW."Id_OrdenTrabajo",'Revisión '||NEW."Etapa",NEW."Id_Usuario",NEW."Observacion");
  RETURN NULL;
END $$;
CREATE TRIGGER "Revision_Aplicar" AFTER INSERT ON afrisan."RevisionOT" FOR EACH ROW EXECUTE FUNCTION afrisan.revision_aplicar();

CREATE FUNCTION afrisan.ot_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
DECLARE a integer;
BEGIN
  IF TG_OP='DELETE' THEN RAISE EXCEPTION 'No borrar OT'; END IF;
  IF TG_OP='INSERT' THEN
    a:=afrisan.actor(ARRAY['Autorizador']); NEW."Id_Autorizador":=a; NEW."FechaCreacion":=clock_timestamp();
    IF NEW."Estado"<>'Asignada' OR NEW."FechaTerminoTecnico" IS NOT NULL THEN RAISE EXCEPTION 'Crear OT asignada, no cerrada'; END IF;
    IF NOT EXISTS(SELECT 1 FROM afrisan."Usuario" u JOIN afrisan."Rol" r USING("Id_Rol") WHERE u."Id_Usuario"=NEW."Id_Tecnico" AND u."Activo" AND r."Nombre_Rol"='Tecnico') THEN RAISE EXCEPTION 'Técnico asignado inválido'; END IF;
    RETURN NEW;
  END IF;
  IF (to_jsonb(NEW)-ARRAY['Estado','FechaTerminoTecnico','ObservacionTermino','FechaCierre'])<>(to_jsonb(OLD)-ARRAY['Estado','FechaTerminoTecnico','ObservacionTermino','FechaCierre']) THEN RAISE EXCEPTION 'No reescribir OT autorizada'; END IF;
  IF NEW."Estado"=OLD."Estado" THEN RAISE EXCEPTION 'Sin transición válida'; END IF;
  IF NEW."Estado"='EnProcesoDespacho' AND OLD."Estado"='Asignada' THEN
    a:=afrisan.actor(ARRAY['Tecnico']);
    IF NOT EXISTS(SELECT 1 FROM afrisan."Despacho" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo") THEN RAISE EXCEPTION 'Falta despacho'; END IF;
  ELSIF NEW."Estado"='EnTerreno' AND OLD."Estado" IN ('EnProcesoDespacho','Asignada') THEN
    a:=afrisan.actor(ARRAY['Tecnico']);
    IF NOT EXISTS(SELECT 1 FROM afrisan."OperacionTerreno" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo" AND "Estado"='Registrada') THEN RAISE EXCEPTION 'Falta cambio confirmado'; END IF;
  ELSIF NEW."Estado"='PendienteBodega' AND OLD."Estado"='EnTerreno' THEN
    a:=afrisan.actor(ARRAY['Tecnico']);
    IF EXISTS(SELECT 1 FROM afrisan."OperacionTerreno" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo" AND "Estado"='Borrador') THEN RAISE EXCEPTION 'Resolver borradores antes de terminar'; END IF;
    IF EXISTS(SELECT 1 FROM afrisan."RetiroPrevisto" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo" AND "Estado"='Reservado') AND coalesce(btrim(NEW."ObservacionTermino"),'')='' THEN RAISE EXCEPTION 'Explicar ejecución parcial'; END IF;
    NEW."FechaTerminoTecnico":=clock_timestamp();
  ELSIF NEW."Estado"='PendienteRevision' AND OLD."Estado"='PendienteBodega' THEN
    a:=afrisan.actor(ARRAY['Bodega']);
    IF NOT EXISTS(SELECT 1 FROM afrisan."RevisionOT" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo" AND "Etapa"='Bodega') THEN RAISE EXCEPTION 'Falta aprobación Bodega'; END IF;
  ELSIF NEW."Estado"='Cerrada' AND OLD."Estado"='PendienteRevision' THEN
    a:=afrisan.actor(ARRAY['Autorizador']); PERFORM afrisan.sin_pendientes(OLD."Id_OrdenTrabajo");
    IF (SELECT count(*) FROM afrisan."RevisionOT" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo")<>2 OR EXISTS(SELECT 1 FROM afrisan."Incidencia" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo" AND "Respuesta" IS NULL) THEN RAISE EXCEPTION 'Faltan revisiones/gestión de incidencias'; END IF;
    NEW."FechaCierre":=clock_timestamp();
  ELSE RAISE EXCEPTION 'Transición de OT no permitida'; END IF;
  IF NEW."Estado" IN ('EnProcesoDespacho','EnTerreno','PendienteBodega') AND a<>OLD."Id_Tecnico" THEN RAISE EXCEPTION 'OT de otro técnico'; END IF;
  IF NEW."Estado"<>'PendienteBodega' AND (NEW."FechaTerminoTecnico",NEW."ObservacionTermino") IS DISTINCT FROM (OLD."FechaTerminoTecnico",OLD."ObservacionTermino") THEN RAISE EXCEPTION 'No sobrescribir informe técnico'; END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER "OT_Validar" BEFORE INSERT OR UPDATE OR DELETE ON afrisan."OrdenTrabajo" FOR EACH ROW EXECUTE FUNCTION afrisan.ot_validar();
CREATE FUNCTION afrisan.ot_cierre_aplicar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
BEGIN
  IF NEW."Estado"='Cerrada' AND OLD."Estado"<>'Cerrada' THEN
    UPDATE afrisan."RetiroPrevisto" SET "Estado"='NoEjecutado' WHERE "Id_OrdenTrabajo"=NEW."Id_OrdenTrabajo" AND "Estado"='Reservado';
    INSERT INTO afrisan."EventoOT"("Id_OrdenTrabajo","Accion","Id_Usuario") VALUES(NEW."Id_OrdenTrabajo",'Cierre administrativo',afrisan.actor(ARRAY['Autorizador']));
  END IF; RETURN NULL;
END $$;
CREATE TRIGGER "OT_Cierre_Aplicar" AFTER UPDATE ON afrisan."OrdenTrabajo" FOR EACH ROW EXECUTE FUNCTION afrisan.ot_cierre_aplicar();

CREATE FUNCTION afrisan.incidencia_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
DECLARE o afrisan."OrdenTrabajo";
BEGIN
  IF TG_OP='DELETE' THEN RAISE EXCEPTION 'No borrar incidencias'; END IF;
  IF TG_OP='INSERT' THEN
    o:=afrisan.ot_actor(NEW."Id_OrdenTrabajo",ARRAY['Tecnico','Bodega']);
    IF o."Estado"='Cerrada' OR EXISTS(SELECT 1 FROM afrisan."RevisionOT" WHERE "Id_OrdenTrabajo"=o."Id_OrdenTrabajo") OR NEW."Respuesta" IS NOT NULL OR NEW."Id_Revisor" IS NOT NULL OR NEW."FechaRevision" IS NOT NULL THEN RAISE EXCEPTION 'Incidencia fuera de etapa'; END IF;
    IF NEW."Id_Operacion" IS NOT NULL AND NOT EXISTS(SELECT 1 FROM afrisan."OperacionTerreno" WHERE "Id_Operacion"=NEW."Id_Operacion" AND "Id_OrdenTrabajo"=o."Id_OrdenTrabajo") THEN RAISE EXCEPTION 'Operación de otra OT'; END IF;
    NEW."Id_Usuario":=afrisan.actor(ARRAY['Tecnico','Bodega']); NEW."Fecha":=clock_timestamp();
  ELSE
    o:=afrisan.ot_actor(OLD."Id_OrdenTrabajo",ARRAY['Autorizador']);
    IF o."Estado"<>'PendienteRevision' OR OLD."Respuesta" IS NOT NULL OR NEW."Respuesta" IS NULL OR
      (to_jsonb(NEW)-ARRAY['Respuesta','Id_Revisor','FechaRevision'])<>(to_jsonb(OLD)-ARRAY['Respuesta','Id_Revisor','FechaRevision']) THEN RAISE EXCEPTION 'Solo revisar una incidencia pendiente después de Bodega'; END IF;
    NEW."Id_Revisor":=afrisan.actor(ARRAY['Autorizador']); NEW."FechaRevision":=clock_timestamp();
  END IF; RETURN NEW;
END $$;
CREATE TRIGGER "Incidencia_Validar" BEFORE INSERT OR UPDATE OR DELETE ON afrisan."Incidencia" FOR EACH ROW EXECUTE FUNCTION afrisan.incidencia_validar();

-- Función de retiro atómica e idempotente. fotos es array JSON de metadatos, no binarios.
CREATE FUNCTION afrisan.retirar_cilindro(id uuid,ot bigint,cilindro bigint,peso bigint,fotos jsonb) RETURNS afrisan."Despacho"
LANGUAGE plpgsql SET search_path=pg_catalog,afrisan AS $$
DECLARE d afrisan."Despacho"; firma text; a integer; f jsonb;
BEGIN
  a:=afrisan.actor(ARRAY['Tecnico']);
  PERFORM afrisan.ot_actor(ot,ARRAY['Tecnico']);
  firma:=encode(sha256(convert_to(jsonb_build_array(ot,cilindro,peso,fotos)::text,'UTF8')),'hex');
  SELECT * INTO d FROM afrisan."Despacho" WHERE "Id_Despacho"=id;
  IF FOUND THEN
    IF d."HuellaSolicitud"<>firma OR d."Id_Tecnico"<>a THEN RAISE EXCEPTION 'UUID usado con otra solicitud'; END IF;
    RETURN d;
  END IF;
  IF jsonb_typeof(fotos) IS DISTINCT FROM 'array' OR jsonb_array_length(fotos) NOT BETWEEN 1 AND 8 THEN RAISE EXCEPTION 'Adjuntar entre una y ocho fotos del peso'; END IF;
  INSERT INTO afrisan."Pesaje"("Id_Pesaje","Id_Cilindro","Id_OrdenTrabajo","Tipo","PesoBrutoGr","Id_Usuario") VALUES(id,cilindro,ot,'Despacho',peso,a);
  FOR f IN SELECT * FROM jsonb_array_elements(fotos) LOOP
    INSERT INTO afrisan."EvidenciaPesaje"("Id_Evidencia","Id_Pesaje","BucketPrivado","RutaObjeto","NombreArchivo","ContentType","Bytes","SHA256","Id_Usuario")
    VALUES((f->>'id')::uuid,id,f->>'bucket',f->>'ruta',f->>'nombre',f->>'mime',(f->>'bytes')::bigint,f->>'sha256',a);
  END LOOP;
  INSERT INTO afrisan."Despacho"("Id_Despacho","Id_OrdenTrabajo","Id_Cilindro","Id_Pesaje","Id_Tecnico","HuellaSolicitud") VALUES(id,ot,cilindro,id,a,firma) RETURNING * INTO d;
  RETURN d;
END $$;

-- Registros de hechos inmutables. Custodia es una proyección operativa, no historial.
DO $$ DECLARE t text; BEGIN
  FOREACH t IN ARRAY ARRAY['Pesaje','EvidenciaPesaje','RecepcionBodega','RevisionOT','HistorialCilindro','EventoOT'] LOOP
    EXECUTE format('CREATE TRIGGER %I BEFORE UPDATE OR DELETE ON afrisan.%I FOR EACH ROW EXECUTE FUNCTION afrisan.inmutable()','Inmutable_'||t,t);
  END LOOP;
END $$;
CREATE FUNCTION afrisan.custodia_proteger() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF pg_trigger_depth()<2 THEN RAISE EXCEPTION 'Custodia cambia solo al despachar, confirmar cambio o recibir'; END IF;
  IF TG_OP='DELETE' THEN RETURN OLD; END IF; RETURN NEW;
END $$;
CREATE TRIGGER "Custodia_Proteger" BEFORE INSERT OR UPDATE OR DELETE ON afrisan."CustodiaActual" FOR EACH ROW EXECUTE FUNCTION afrisan.custodia_proteger();

CREATE VIEW afrisan."Inventario" AS
SELECT c."Id_Cilindro",c."CilindroQR",c."Id_Refrigerante",c."Ubicacion",c."Estado",c."Id_Sucursal",c."Id_Tecnico",
 c."TaraGr",c."Capacidadgr",p."PesoBrutoGr",p."PesoBrutoGr"-c."TaraGr" AS "ContenidoSegunPesajeGr",p."Fecha" AS "FechaUltimoPesaje"
FROM afrisan."Cilindros" c LEFT JOIN afrisan."Pesaje" p ON p."Id_Pesaje"=c."Id_UltimoPesaje";
CREATE VIEW afrisan."ResumenOT" AS
SELECT o."Id_OrdenTrabajo",o."CodigoOT",o."Id_Sucursal",o."Id_Tecnico",o."Estado",o."FechaCreacion",o."FechaCierre",
 (SELECT count(*) FROM afrisan."Despacho" d WHERE d."Id_OrdenTrabajo"=o."Id_OrdenTrabajo") AS "CilindrosDespachados",
 (SELECT count(*) FROM afrisan."OperacionTerreno" op WHERE op."Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND op."Estado"='Registrada') AS "OperacionesConfirmadas"
FROM afrisan."OrdenTrabajo" o;
CREATE VIEW afrisan."PesajesRecepcion" AS
SELECT r."Id_Recepcion",r."Id_OrdenTrabajo",r."Id_Cilindro",r."Id_Operacion",r."Id_DespachoSinUso",r."Fecha",
 pr."PesoBrutoGr" AS "PesoRecepcionGr",pt."PesoBrutoGr" AS "PesoTerrenoGr",pd."PesoBrutoGr" AS "PesoSalidaSinUsoGr",
 pr."PesoBrutoGr"-coalesce(pt."PesoBrutoGr",pd."PesoBrutoGr") AS "DiferenciaGr"
FROM afrisan."RecepcionBodega" r JOIN afrisan."Pesaje" pr ON pr."Id_Pesaje"=r."Id_Pesaje"
LEFT JOIN afrisan."OperacionTerreno" op ON op."Id_Operacion"=r."Id_Operacion"
LEFT JOIN afrisan."Pesaje" pt ON pt."Id_Pesaje"=op."Id_PesajeRetirado"
LEFT JOIN afrisan."Despacho" d ON d."Id_Despacho"=r."Id_DespachoSinUso"
LEFT JOIN afrisan."Pesaje" pd ON pd."Id_Pesaje"=d."Id_Pesaje";

-- COMPONENTE: 02_soporte.sql
-- Módulos de soporte del contexto actual. Ejecutar después de 01_operacion.sql.
-- No inserta roles, usuarios, catálogos ni datos de ejemplo.
-- En el archivo canónico este módulo comparte la transacción de instalación.

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

-- COMPONENTE: 03_auditoria_y_acceso.sql
CREATE TABLE afrisan."Auditoria" (
  "Id_Auditoria" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Fecha" timestamptz NOT NULL DEFAULT clock_timestamp(), "Id_Usuario" integer REFERENCES afrisan."Usuario",
  "UsuarioBD" text NOT NULL DEFAULT session_user, "Tabla" text NOT NULL,
  "Accion" varchar(10) NOT NULL CHECK ("Accion" IN ('INSERT','UPDATE','DELETE')),
  "ValoresAnteriores" jsonb,"ValoresNuevos" jsonb
);
CREATE FUNCTION afrisan.auditar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
DECLARE anterior jsonb; nuevo jsonb;
BEGIN
  IF TG_OP<>'INSERT' THEN anterior:=to_jsonb(OLD); END IF;
  IF TG_OP<>'DELETE' THEN nuevo:=to_jsonb(NEW); END IF;
  IF TG_TABLE_NAME='Usuario' THEN
    anterior:=anterior-ARRAY['Clave_Usuario','VersionSeguridad']; nuevo:=nuevo-ARRAY['Clave_Usuario','VersionSeguridad'];
  END IF;
  INSERT INTO afrisan."Auditoria"("Id_Usuario","Tabla","Accion","ValoresAnteriores","ValoresNuevos")
  VALUES(nullif(current_setting('afrisan.id_usuario',true),'')::integer,TG_TABLE_NAME,TG_OP,anterior,nuevo);
  RETURN NULL;
END $$;
CREATE TRIGGER "Auditoria_Inmutable" BEFORE UPDATE OR DELETE ON afrisan."Auditoria" FOR EACH ROW EXECUTE FUNCTION afrisan.inmutable();

-- Baja administrativa con motivo. No supone que exista certificado todavía.
-- Es una herramienta de administración; el responsable de negocio debe ratificarse.
CREATE TABLE afrisan."BajaCilindro" (
  "Id_Baja" uuid PRIMARY KEY DEFAULT gen_random_uuid(),"Id_Cilindro" bigint NOT NULL UNIQUE REFERENCES afrisan."Cilindros",
  "Motivo" text NOT NULL CHECK(length(btrim("Motivo"))>=5),
  "EstadoDestino" varchar(20) NOT NULL CHECK("EstadoDestino" IN ('Chatarra','Inactivo')),
  "Id_Usuario" integer NOT NULL REFERENCES afrisan."Usuario","Fecha" timestamptz NOT NULL DEFAULT clock_timestamp()
);
CREATE FUNCTION afrisan.baja_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
DECLARE c afrisan."Cilindros";
BEGIN
  NEW."Id_Usuario":=afrisan.actor(ARRAY['Administrador']);
  SELECT * INTO STRICT c FROM afrisan."Cilindros" WHERE "Id_Cilindro"=NEW."Id_Cilindro" FOR UPDATE;
  IF c."Ubicacion"<>'Bodega' OR EXISTS(SELECT 1 FROM afrisan."CustodiaActual" WHERE "Id_Cilindro"=c."Id_Cilindro") OR EXISTS(SELECT 1 FROM afrisan."RetiroPrevisto" WHERE "Id_Cilindro"=c."Id_Cilindro" AND "Estado"='Reservado') THEN RAISE EXCEPTION 'No retirar de servicio un envase instalado, reservado o en custodia'; END IF;
  NEW."Fecha":=clock_timestamp(); RETURN NEW;
END $$;
CREATE TRIGGER "Baja_Validar" BEFORE INSERT ON afrisan."BajaCilindro" FOR EACH ROW EXECUTE FUNCTION afrisan.baja_validar();
CREATE TRIGGER "Baja_Inmutable" BEFORE UPDATE OR DELETE ON afrisan."BajaCilindro" FOR EACH ROW EXECUTE FUNCTION afrisan.inmutable();
CREATE FUNCTION afrisan.baja_aplicar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
BEGIN
  UPDATE afrisan."Cilindros" SET "Ubicacion"='FueraServicio',"Estado"=NEW."EstadoDestino","Id_Sucursal"=NULL,"Id_Tecnico"=NULL WHERE "Id_Cilindro"=NEW."Id_Cilindro";
  RETURN NULL;
END $$;
CREATE TRIGGER "Baja_Aplicar" AFTER INSERT ON afrisan."BajaCilindro" FOR EACH ROW EXECUTE FUNCTION afrisan.baja_aplicar();
CREATE FUNCTION afrisan.certificado_aplicar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan AS $$
BEGIN
  IF NEW."Estado"='Certificado' AND OLD."Estado"='PendienteCertificado' THEN
    UPDATE afrisan."Cilindros" c SET "Estado"='DisposicionFinalCertificada'
    FROM afrisan."CertificadosCilindros" l WHERE l."Id_Certificado"=NEW."Id_Certificado" AND l."Id_Cilindro"=c."Id_Cilindro";
  END IF; RETURN NULL;
END $$;
CREATE TRIGGER "Certificado_Aplicar" AFTER UPDATE ON afrisan."CertificadosDisposicion" FOR EACH ROW EXECUTE FUNCTION afrisan.certificado_aplicar();

CREATE VIEW afrisan."PedidoYEjecucion" AS
SELECT d."Id_OrdenTrabajo",o."Id_Sucursal",d."Id_Refrigerante",d."GrSolicitados",d."GrAutorizados",
 (SELECT sum(p."PesoBrutoGr"-c."TaraGr") FROM afrisan."Despacho" e JOIN afrisan."Cilindros" c USING("Id_Cilindro")
  JOIN afrisan."Pesaje" p ON p."Id_Pesaje"=e."Id_Pesaje" WHERE e."Id_OrdenTrabajo"=d."Id_OrdenTrabajo" AND c."Id_Refrigerante"=d."Id_Refrigerante") AS "GrContenidoDespachado",
 (SELECT sum(p."PesoBrutoGr"-c."TaraGr") FROM afrisan."OperacionTerreno" op JOIN afrisan."Cilindros" c ON c."Id_Cilindro"=op."Id_Instalado"
  JOIN afrisan."Pesaje" p ON p."Id_Pesaje"=op."Id_PesajeInstalado" WHERE op."Id_OrdenTrabajo"=d."Id_OrdenTrabajo" AND op."Estado"='Registrada' AND c."Id_Refrigerante"=d."Id_Refrigerante") AS "GrContenidoInstalado"
FROM afrisan."DetalleOT" d JOIN afrisan."OrdenTrabajo" o USING("Id_OrdenTrabajo");
COMMENT ON VIEW afrisan."PedidoYEjecucion" IS 'Masas de etapas diferentes. No sumar despacho e instalación ni interpretar esas masas como consumo/emisiones. Sin registros permanece NULL.';

-- Bloqueos, auditoría e índices de consulta. No se instala ninguna cuenta.
DO $$ DECLARE t record; BEGIN
  FOR t IN SELECT tablename FROM pg_tables WHERE schemaname='afrisan' AND tablename<>'Auditoria' LOOP
    EXECUTE format('CREATE TRIGGER %I AFTER INSERT OR UPDATE OR DELETE ON afrisan.%I FOR EACH ROW EXECUTE FUNCTION afrisan.auditar()','Auditar_'||t.tablename,t.tablename);
  END LOOP;
  FOR t IN SELECT tablename FROM pg_tables WHERE schemaname='afrisan' LOOP
    EXECUTE format('ALTER TABLE afrisan.%I ENABLE ROW LEVEL SECURITY',t.tablename);
  END LOOP;
END $$;
CREATE INDEX "IX_Pesaje_Cilindro_Fecha" ON afrisan."Pesaje"("Id_Cilindro","Fecha");
CREATE INDEX "IX_Despacho_OT" ON afrisan."Despacho"("Id_OrdenTrabajo");
CREATE INDEX "IX_Operacion_OT" ON afrisan."OperacionTerreno"("Id_OrdenTrabajo");
CREATE INDEX "IX_Recepcion_OT" ON afrisan."RecepcionBodega"("Id_OrdenTrabajo");
CREATE INDEX "IX_Incidencia_OT" ON afrisan."Incidencia"("Id_OrdenTrabajo");
CREATE INDEX "IX_Evento_OT" ON afrisan."EventoOT"("Id_OrdenTrabajo","Fecha");
CREATE INDEX "IX_Auditoria_Tabla_Fecha" ON afrisan."Auditoria"("Tabla","Fecha");

REVOKE ALL ON ALL TABLES IN SCHEMA afrisan FROM PUBLIC;
REVOKE ALL ON ALL SEQUENCES IN SCHEMA afrisan FROM PUBLIC;
REVOKE EXECUTE ON ALL FUNCTIONS IN SCHEMA afrisan FROM PUBLIC;
ALTER DEFAULT PRIVILEGES IN SCHEMA afrisan REVOKE EXECUTE ON FUNCTIONS FROM PUBLIC;
ALTER DEFAULT PRIVILEGES IN SCHEMA afrisan REVOKE ALL ON TABLES FROM PUBLIC;
DO $$ DECLARE r text; BEGIN
  FOREACH r IN ARRAY ARRAY['anon','authenticated'] LOOP
    IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname=r) THEN
      EXECUTE format('REVOKE ALL ON SCHEMA afrisan FROM %I',r);
      EXECUTE format('REVOKE ALL ON ALL TABLES IN SCHEMA afrisan FROM %I',r);
      EXECUTE format('REVOKE ALL ON ALL SEQUENCES IN SCHEMA afrisan FROM %I',r);
      EXECUTE format('REVOKE EXECUTE ON ALL FUNCTIONS IN SCHEMA afrisan FROM %I',r);
    END IF;
  END LOOP;
END $$;
COMMENT ON SCHEMA afrisan IS 'Instalación vacía del contexto actual. Acceso solo desde backend de confianza. Sin políticas de navegador ni credenciales precargadas.';
COMMIT;
