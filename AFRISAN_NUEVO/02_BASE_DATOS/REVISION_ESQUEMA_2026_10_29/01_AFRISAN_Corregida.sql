-- AFRISAN | CORRECCIÓN DEL ESQUEMA RECIBIDO | 29/10/2026
-- Sustituye el modelo de cargas a equipos por el reemplazo físico de cilindros.
-- Derivado de 04_Crear_Afrisan_Contexto_Actual.sql, conservando su contenido completo.
-- INSTALACIÓN NUEVA AISLADA: no es una migración de los datos de afrisan.
-- Ejecutar solo 01_AFRISAN_Corregida.sql completo, sin el original ni otros instaladores.
-- Crea afrisan_actual y afrisan_bi_actual; se detiene si ya existen.
-- Sin usuarios, roles ni ejemplos precargados. Ver README.md antes de instalar.

-- AFRISAN | CONTEXTO ACTUAL | 25/10/2026
-- INSTALACIÓN VACÍA. Ejecutar únicamente este archivo completo.
-- Crea afrisan_actual y afrisan_bi_actual. No borra/migra afrisan ni public.
-- Se detiene si esos esquemas ya existen. No instala cuentas ni ejemplos.
-- La API actual sigue en memoria: este SQL no conecta ni adapta automáticamente EF Core.
-- Fotos: metadatos de objetos privados; la API verifica el archivo real en Storage.
-- Gramos enteros, fechas timestamptz, precios NUMERIC. No inferir consumo/emisiones.
-- La medición de límites se declara con criterio explícito; no inventa su fórmula.
-- Requiere PostgreSQL 15+; ver README y validación local antes de usar en Supabase.
BEGIN;
-- COMPONENTE: 01_operacion.sql
-- Componente de la instalación 04. No ejecutar junto con el paquete completo.
-- Flujo confirmado el 25/10/2026. Pesos en gramos enteros; fotos en Storage privado.
CREATE SCHEMA afrisan_actual;
CREATE SCHEMA afrisan_bi_actual;
REVOKE ALL ON SCHEMA afrisan_actual, afrisan_bi_actual FROM PUBLIC;

CREATE TABLE afrisan_actual."Rol" (
  "Id_Rol" smallint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Nombre_Rol" varchar(40) NOT NULL UNIQUE CHECK ("Nombre_Rol" IN ('Administrador','Jefatura','Autorizador','Bodega','Tecnico'))
);
CREATE TABLE afrisan_actual."Usuario" (
  "Id_Usuario" integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Nombre" varchar(100) NOT NULL CHECK (btrim("Nombre")<>''),
  "Apellido" varchar(100) NOT NULL CHECK (btrim("Apellido")<>''),
  "Correo_Usuario" varchar(254) NOT NULL CHECK (btrim("Correo_Usuario")<>''),
  "Clave_Usuario" text NOT NULL CHECK (length("Clave_Usuario")>=32),
  "Id_Rol" smallint NOT NULL REFERENCES afrisan_actual."Rol",
  "Activo" boolean NOT NULL DEFAULT true,
  "IntentosFallidos" integer NOT NULL DEFAULT 0 CHECK ("IntentosFallidos">=0),
  "BloqueadoHasta" timestamptz,
  "VersionSeguridad" uuid NOT NULL DEFAULT gen_random_uuid(),
  "FechaCreacion" timestamptz NOT NULL DEFAULT clock_timestamp()
);
CREATE UNIQUE INDEX "UX_Usuario_Correo" ON afrisan_actual."Usuario" (lower(btrim("Correo_Usuario")));
COMMENT ON COLUMN afrisan_actual."Usuario"."Clave_Usuario" IS 'Solo hash seguro generado por la API. La longitud no verifica algoritmo. Nunca contraseña en texto plano.';

-- El backend de confianza establece el actor validado por JWT EN CADA TRANSACCIÓN.
-- SELECT set_config('afrisan_actual.id_usuario', :actor_autenticado::text, true);
-- Este contexto no es autenticación por sí mismo ni debe aceptarse desde el navegador.
CREATE FUNCTION afrisan_actual.actor(roles text[]) RETURNS integer LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
DECLARE identificador integer; nombre text;
BEGIN
  identificador := nullif(current_setting('afrisan_actual.id_usuario',true),'')::integer;
  SELECT r."Nombre_Rol" INTO nombre FROM afrisan_actual."Usuario" u
    JOIN afrisan_actual."Rol" r USING ("Id_Rol") WHERE u."Id_Usuario"=identificador AND u."Activo";
  IF nombre IS NULL OR NOT(nombre=ANY(roles)) THEN RAISE EXCEPTION 'Actor ausente, inactivo o sin rol para esta acción' USING ERRCODE='42501'; END IF;
  RETURN identificador;
END $$;
CREATE FUNCTION afrisan_actual.inmutable() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN RAISE EXCEPTION 'El registro confirmado/histórico no se modifica ni elimina: %',TG_TABLE_NAME; END $$;

CREATE TABLE afrisan_actual."Cliente" (
  "Id_Cliente" integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "RutCliente" varchar(20) NOT NULL UNIQUE CHECK (btrim("RutCliente")<>''),
  "NombreEmpresa" varchar(200) NOT NULL CHECK (btrim("NombreEmpresa")<>''), "Activo" boolean NOT NULL DEFAULT true
);
CREATE TABLE afrisan_actual."Sucursales" (
  "Id_Sucursal" integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_Cliente" integer NOT NULL REFERENCES afrisan_actual."Cliente",
  "NombreSucursal" varchar(200) NOT NULL CHECK (btrim("NombreSucursal")<>''),
  "Direccion" varchar(400) NOT NULL CHECK (btrim("Direccion")<>''), "Activo" boolean NOT NULL DEFAULT true,
  UNIQUE ("Id_Cliente","NombreSucursal")
);
CREATE TABLE afrisan_actual."Refrigerantes" (
  "Id_Refrigerante" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "CodigoRefrigerante" varchar(40) NOT NULL CHECK (btrim("CodigoRefrigerante")<>''),
  "NombreRefrigerante" varchar(150) NOT NULL CHECK (btrim("NombreRefrigerante")<>''),
  "FactorGWP" numeric(14,5) CHECK ("FactorGWP">=0), "FuenteGWP" text, "MetodologiaGWP" text,
  "Activo" boolean NOT NULL DEFAULT true,
  CHECK ("FactorGWP" IS NULL OR (coalesce(btrim("FuenteGWP"),'')<>'' AND coalesce(btrim("MetodologiaGWP"),'')<>''))
);
CREATE UNIQUE INDEX "UX_Refrigerante_Codigo" ON afrisan_actual."Refrigerantes" (upper(btrim("CodigoRefrigerante")));
CREATE TABLE afrisan_actual."Cilindros" (
  "Id_Cilindro" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "CilindroQR" varchar(120) NOT NULL UNIQUE CHECK (btrim("CilindroQR")<>''), "NumeroSerie" varchar(120),
  "Id_Refrigerante" bigint NOT NULL REFERENCES afrisan_actual."Refrigerantes",
  "Capacidadgr" bigint NOT NULL CHECK ("Capacidadgr">0), "TaraGr" bigint NOT NULL CHECK ("TaraGr">0),
  "Ubicacion" varchar(30) NOT NULL DEFAULT 'Bodega' CHECK ("Ubicacion" IN ('Bodega','Sucursal','Tecnico','FueraServicio')),
  "Id_Sucursal" integer REFERENCES afrisan_actual."Sucursales", "Id_Tecnico" integer REFERENCES afrisan_actual."Usuario",
  "Estado" varchar(40) NOT NULL DEFAULT 'PendientePesaje' CHECK ("Estado" IN ('PendientePesaje','Disponible','Vacio','EnProcesoDespacho','Instalado','RetiradoPendienteRecepcion','Chatarra','DisposicionFinalCertificada','Inactivo')),
  "Id_UltimoPesaje" uuid, "FechaRegistro" timestamptz NOT NULL DEFAULT clock_timestamp(),
  "Id_Registrador" integer NOT NULL REFERENCES afrisan_actual."Usuario",
  CHECK (("Ubicacion"='Sucursal')=("Id_Sucursal" IS NOT NULL)),
  CHECK (("Ubicacion"='Tecnico')=("Id_Tecnico" IS NOT NULL)),
  CHECK (("Ubicacion"='Bodega' AND "Estado" IN ('PendientePesaje','Disponible','Vacio')) OR
         ("Ubicacion"='Sucursal' AND "Estado"='Instalado') OR
         ("Ubicacion"='Tecnico' AND "Estado" IN ('EnProcesoDespacho','RetiradoPendienteRecepcion')) OR
         ("Ubicacion"='FueraServicio' AND "Estado" IN ('Chatarra','DisposicionFinalCertificada','Inactivo')))
);
CREATE UNIQUE INDEX "UX_Cilindro_Serie" ON afrisan_actual."Cilindros" (upper(btrim("NumeroSerie"))) WHERE "NumeroSerie" IS NOT NULL;
CREATE TABLE afrisan_actual."OrdenTrabajo" (
  "Id_OrdenTrabajo" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_Publico" uuid NOT NULL UNIQUE DEFAULT gen_random_uuid(),
  "CodigoOT" varchar(100) NOT NULL UNIQUE CHECK (btrim("CodigoOT")<>''),
  "Origen" varchar(20) NOT NULL DEFAULT 'Local' CHECK ("Origen" IN ('Local','Defontana')),
  "CodigoExterno" varchar(100), "Id_Sucursal" integer NOT NULL REFERENCES afrisan_actual."Sucursales",
  "Id_Tecnico" integer NOT NULL REFERENCES afrisan_actual."Usuario", "Id_Autorizador" integer NOT NULL REFERENCES afrisan_actual."Usuario",
  "Descripcion" varchar(2000) NOT NULL CHECK (btrim("Descripcion")<>''),
  "Estado" varchar(40) NOT NULL DEFAULT 'Asignada' CHECK ("Estado" IN ('Asignada','EnProcesoDespacho','EnTerreno','PendienteBodega','PendienteRevision','Cerrada')),
  "FechaCreacion" timestamptz NOT NULL DEFAULT clock_timestamp(), "FechaTerminoTecnico" timestamptz,
  "ObservacionTermino" text, "FechaCierre" timestamptz,
  CHECK ("Origen"<>'Defontana' OR coalesce(btrim("CodigoExterno"),'')<>''),
  CHECK (("Estado"='Cerrada')=("FechaCierre" IS NOT NULL)),
  CHECK ("Estado" NOT IN ('PendienteBodega','PendienteRevision','Cerrada') OR "FechaTerminoTecnico" IS NOT NULL)
);
CREATE UNIQUE INDEX "UX_OT_Externa" ON afrisan_actual."OrdenTrabajo" ("Origen","CodigoExterno") WHERE "CodigoExterno" IS NOT NULL;
CREATE TABLE afrisan_actual."DetalleOT" (
  "Id_OrdenTrabajo" bigint NOT NULL REFERENCES afrisan_actual."OrdenTrabajo",
  "Id_Refrigerante" bigint NOT NULL REFERENCES afrisan_actual."Refrigerantes",
  "GrSolicitados" bigint NOT NULL CHECK ("GrSolicitados">0), "GrAutorizados" bigint NOT NULL CHECK ("GrAutorizados">0),
  PRIMARY KEY ("Id_OrdenTrabajo","Id_Refrigerante")
);
CREATE TABLE afrisan_actual."RetiroPrevisto" (
  "Id_OrdenTrabajo" bigint NOT NULL REFERENCES afrisan_actual."OrdenTrabajo",
  "Id_Cilindro" bigint NOT NULL REFERENCES afrisan_actual."Cilindros",
  "Estado" varchar(20) NOT NULL DEFAULT 'Reservado' CHECK ("Estado" IN ('Reservado','Ejecutado','NoEjecutado')),
  PRIMARY KEY ("Id_OrdenTrabajo","Id_Cilindro")
);
CREATE UNIQUE INDEX "UX_Retiro_Reserva" ON afrisan_actual."RetiroPrevisto" ("Id_Cilindro") WHERE "Estado"='Reservado';

CREATE TABLE afrisan_actual."Pesaje" (
  "Id_Pesaje" uuid PRIMARY KEY DEFAULT gen_random_uuid(), "Id_Cilindro" bigint NOT NULL REFERENCES afrisan_actual."Cilindros",
  "Id_OrdenTrabajo" bigint REFERENCES afrisan_actual."OrdenTrabajo",
  "Tipo" varchar(20) NOT NULL CHECK ("Tipo" IN ('Inicial','Despacho','Retirado','Instalado','Recepcion')),
  "PesoBrutoGr" bigint NOT NULL CHECK ("PesoBrutoGr">=0),
  "Id_Usuario" integer NOT NULL REFERENCES afrisan_actual."Usuario", "Fecha" timestamptz NOT NULL DEFAULT clock_timestamp(),
  "Observacion" text,
  CHECK (("Tipo"='Inicial')=("Id_OrdenTrabajo" IS NULL)), UNIQUE ("Id_Pesaje","Id_Cilindro")
);
CREATE UNIQUE INDEX "UX_Pesaje_Inicial" ON afrisan_actual."Pesaje" ("Id_Cilindro") WHERE "Tipo"='Inicial';
ALTER TABLE afrisan_actual."Cilindros" ADD FOREIGN KEY ("Id_UltimoPesaje","Id_Cilindro") REFERENCES afrisan_actual."Pesaje" ("Id_Pesaje","Id_Cilindro");
CREATE TABLE afrisan_actual."EvidenciaPesaje" (
  "Id_Evidencia" uuid PRIMARY KEY DEFAULT gen_random_uuid(), "Id_Pesaje" uuid NOT NULL REFERENCES afrisan_actual."Pesaje",
  "BucketPrivado" varchar(100) NOT NULL CHECK (btrim("BucketPrivado")<>''),
  "RutaObjeto" text NOT NULL CHECK (btrim("RutaObjeto")<>'' AND "RutaObjeto" !~* '^https?://' AND "RutaObjeto" !~ '(^|/)\.\.(/|$)'),
  "NombreArchivo" varchar(150) NOT NULL CHECK (btrim("NombreArchivo")<>''),
  "ContentType" varchar(30) NOT NULL CHECK ("ContentType" IN ('image/jpeg','image/png','image/webp')),
  "Bytes" bigint NOT NULL CHECK ("Bytes" BETWEEN 12 AND 4194304),
  "SHA256" varchar(64) NOT NULL CHECK ("SHA256" ~ '^[0-9a-f]{64}$'),
  "Id_Usuario" integer NOT NULL REFERENCES afrisan_actual."Usuario", "Fecha" timestamptz NOT NULL DEFAULT clock_timestamp(),
  UNIQUE ("Id_Pesaje","SHA256"), UNIQUE ("BucketPrivado","RutaObjeto")
);
COMMENT ON TABLE afrisan_actual."EvidenciaPesaje" IS 'Metadatos de objeto privado. La API verifica archivo real, firma, hash y acceso Storage antes de confirmar; la ruta no acredita una foto.';
CREATE TABLE afrisan_actual."Despacho" (
  "Id_Despacho" uuid PRIMARY KEY, "Id_OrdenTrabajo" bigint NOT NULL REFERENCES afrisan_actual."OrdenTrabajo",
  "Id_Cilindro" bigint NOT NULL REFERENCES afrisan_actual."Cilindros", "Id_Pesaje" uuid NOT NULL UNIQUE,
  "Id_Tecnico" integer NOT NULL REFERENCES afrisan_actual."Usuario", "Fecha" timestamptz NOT NULL DEFAULT clock_timestamp(),
  "Estado" varchar(25) NOT NULL DEFAULT 'EnProcesoDespacho' CHECK ("Estado" IN ('EnProcesoDespacho','Instalado','Devuelto')),
  "HuellaSolicitud" text NOT NULL,
  FOREIGN KEY ("Id_Pesaje","Id_Cilindro") REFERENCES afrisan_actual."Pesaje" ("Id_Pesaje","Id_Cilindro"),
  UNIQUE ("Id_Despacho","Id_OrdenTrabajo","Id_Cilindro")
);
CREATE UNIQUE INDEX "UX_Despacho_Activo" ON afrisan_actual."Despacho" ("Id_Cilindro") WHERE "Estado"='EnProcesoDespacho';
CREATE TABLE afrisan_actual."OperacionTerreno" (
  "Id_Operacion" uuid PRIMARY KEY, "Id_OrdenTrabajo" bigint NOT NULL REFERENCES afrisan_actual."OrdenTrabajo",
  "Tipo" varchar(20) NOT NULL CHECK ("Tipo" IN ('Reemplazo','Agregado')),
  "Id_Instalado" bigint REFERENCES afrisan_actual."Cilindros", "Id_Retirado" bigint REFERENCES afrisan_actual."Cilindros",
  "Id_Despacho" uuid, "Id_PesajeInstalado" uuid UNIQUE, "Id_PesajeRetirado" uuid UNIQUE,
  "Estado" varchar(20) NOT NULL DEFAULT 'Borrador' CHECK ("Estado" IN ('Borrador','Registrada','Descartada')),
  "Version" integer NOT NULL DEFAULT 1 CHECK ("Version">0),
  "Id_Tecnico" integer NOT NULL REFERENCES afrisan_actual."Usuario", "Observacion" text,
  "FechaCreacionLocal" timestamptz, "Dispositivo" varchar(150),
  "FechaRecepcion" timestamptz NOT NULL DEFAULT clock_timestamp(), "FechaConfirmacion" timestamptz,
  "FechaEntregaInformada" timestamptz, "Id_EntregaInformadaPor" integer REFERENCES afrisan_actual."Usuario",
  FOREIGN KEY ("Id_Despacho","Id_OrdenTrabajo","Id_Instalado") REFERENCES afrisan_actual."Despacho" ("Id_Despacho","Id_OrdenTrabajo","Id_Cilindro"),
  FOREIGN KEY ("Id_PesajeInstalado","Id_Instalado") REFERENCES afrisan_actual."Pesaje" ("Id_Pesaje","Id_Cilindro"),
  FOREIGN KEY ("Id_PesajeRetirado","Id_Retirado") REFERENCES afrisan_actual."Pesaje" ("Id_Pesaje","Id_Cilindro"),
  CHECK ("Tipo"<>'Agregado' OR ("Id_Retirado" IS NULL AND "Id_PesajeRetirado" IS NULL AND "FechaEntregaInformada" IS NULL)),
  CHECK ("Id_Retirado" IS NULL OR "Id_Retirado"<>"Id_Instalado"),
  CHECK ("Estado"<>'Registrada' OR ("Id_Instalado" IS NOT NULL AND "Id_Despacho" IS NOT NULL AND "Id_PesajeInstalado" IS NOT NULL AND "FechaConfirmacion" IS NOT NULL)),
  CHECK ("Estado"<>'Registrada' OR "Tipo"<>'Reemplazo' OR ("Id_Retirado" IS NOT NULL AND "Id_PesajeRetirado" IS NOT NULL)),
  CHECK (("FechaEntregaInformada" IS NULL)=("Id_EntregaInformadaPor" IS NULL))
);
CREATE UNIQUE INDEX "UX_Operacion_Instalado" ON afrisan_actual."OperacionTerreno" ("Id_OrdenTrabajo","Id_Instalado") WHERE "Estado"='Registrada';
CREATE UNIQUE INDEX "UX_Operacion_Retirado" ON afrisan_actual."OperacionTerreno" ("Id_OrdenTrabajo","Id_Retirado") WHERE "Estado"='Registrada';
CREATE UNIQUE INDEX "UX_Operacion_Despacho" ON afrisan_actual."OperacionTerreno" ("Id_Despacho") WHERE "Estado"='Registrada';
CREATE TABLE afrisan_actual."CustodiaActual" (
  "Id_Cilindro" bigint PRIMARY KEY REFERENCES afrisan_actual."Cilindros",
  "Id_OrdenTrabajo" bigint NOT NULL REFERENCES afrisan_actual."OrdenTrabajo", "Id_Tecnico" integer NOT NULL REFERENCES afrisan_actual."Usuario",
  "Id_Despacho" uuid REFERENCES afrisan_actual."Despacho", "Id_Operacion" uuid REFERENCES afrisan_actual."OperacionTerreno",
  CHECK (("Id_Despacho" IS NULL)<>("Id_Operacion" IS NULL))
);
CREATE TABLE afrisan_actual."RecepcionBodega" (
  "Id_Recepcion" uuid PRIMARY KEY DEFAULT gen_random_uuid(), "Id_OrdenTrabajo" bigint NOT NULL REFERENCES afrisan_actual."OrdenTrabajo",
  "Id_Cilindro" bigint NOT NULL REFERENCES afrisan_actual."Cilindros", "Id_Operacion" uuid UNIQUE REFERENCES afrisan_actual."OperacionTerreno",
  "Id_DespachoSinUso" uuid UNIQUE REFERENCES afrisan_actual."Despacho", "Id_Pesaje" uuid NOT NULL UNIQUE,
  "Id_Bodeguero" integer NOT NULL REFERENCES afrisan_actual."Usuario", "Fecha" timestamptz NOT NULL DEFAULT clock_timestamp(), "Observacion" text,
  CHECK (("Id_Operacion" IS NULL)<>("Id_DespachoSinUso" IS NULL)),
  FOREIGN KEY ("Id_Pesaje","Id_Cilindro") REFERENCES afrisan_actual."Pesaje" ("Id_Pesaje","Id_Cilindro")
);
CREATE TABLE afrisan_actual."RevisionOT" (
  "Id_Revision" uuid PRIMARY KEY DEFAULT gen_random_uuid(), "Id_OrdenTrabajo" bigint NOT NULL REFERENCES afrisan_actual."OrdenTrabajo",
  "Etapa" varchar(20) NOT NULL CHECK ("Etapa" IN ('Bodega','Autorizador')),
  "Id_Usuario" integer NOT NULL REFERENCES afrisan_actual."Usuario", "Fecha" timestamptz NOT NULL DEFAULT clock_timestamp(),
  "ConfirmoRevision" boolean NOT NULL CHECK ("ConfirmoRevision"), "Observacion" text,
  UNIQUE ("Id_OrdenTrabajo","Etapa")
);
CREATE TABLE afrisan_actual."Incidencia" (
  "Id_Incidencia" uuid PRIMARY KEY DEFAULT gen_random_uuid(), "Id_OrdenTrabajo" bigint NOT NULL REFERENCES afrisan_actual."OrdenTrabajo",
  "Id_Operacion" uuid REFERENCES afrisan_actual."OperacionTerreno",
  "Tipo" varchar(20) NOT NULL CHECK ("Tipo" IN ('Incidencia','Rectificacion')), "Motivo" text NOT NULL CHECK (length(btrim("Motivo"))>=5),
  "Id_Usuario" integer NOT NULL REFERENCES afrisan_actual."Usuario", "Fecha" timestamptz NOT NULL DEFAULT clock_timestamp(),
  "Respuesta" text, "Id_Revisor" integer REFERENCES afrisan_actual."Usuario", "FechaRevision" timestamptz,
  CHECK (("Respuesta" IS NULL AND "Id_Revisor" IS NULL AND "FechaRevision" IS NULL) OR
         ("Respuesta" IS NOT NULL AND length(btrim("Respuesta"))>=5 AND "Id_Revisor" IS NOT NULL AND "FechaRevision" IS NOT NULL))
);
CREATE TABLE afrisan_actual."HistorialCilindro" (
  "Id_Historial" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY, "Id_Cilindro" bigint NOT NULL REFERENCES afrisan_actual."Cilindros",
  "Ubicacion" text NOT NULL, "Estado" text NOT NULL, "Id_Sucursal" integer REFERENCES afrisan_actual."Sucursales",
  "Id_Tecnico" integer REFERENCES afrisan_actual."Usuario", "Id_Pesaje" uuid REFERENCES afrisan_actual."Pesaje",
  "Id_Usuario" integer NOT NULL REFERENCES afrisan_actual."Usuario", "Fecha" timestamptz NOT NULL DEFAULT clock_timestamp()
);
CREATE TABLE afrisan_actual."EventoOT" (
  "Id_Evento" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY, "Id_OrdenTrabajo" bigint NOT NULL REFERENCES afrisan_actual."OrdenTrabajo",
  "Accion" text NOT NULL, "Id_Usuario" integer NOT NULL REFERENCES afrisan_actual."Usuario", "Fecha" timestamptz NOT NULL DEFAULT clock_timestamp(), "Detalle" text
);

CREATE FUNCTION afrisan_actual.ot_actor(id_ot bigint, roles text[], editable boolean DEFAULT false) RETURNS afrisan_actual."OrdenTrabajo"
LANGUAGE plpgsql SET search_path=pg_catalog,afrisan_actual AS $$
DECLARE o afrisan_actual."OrdenTrabajo"; a integer;
BEGIN
  a:=afrisan_actual.actor(roles);
  SELECT * INTO STRICT o FROM afrisan_actual."OrdenTrabajo" WHERE "Id_OrdenTrabajo"=id_ot FOR UPDATE;
  IF 'Tecnico'=ANY(roles) AND a<>o."Id_Tecnico" AND EXISTS(SELECT 1 FROM afrisan_actual."Usuario" u JOIN afrisan_actual."Rol" r USING("Id_Rol") WHERE u."Id_Usuario"=a AND r."Nombre_Rol"='Tecnico') THEN RAISE EXCEPTION 'OT de otro técnico'; END IF;
  IF editable AND o."Estado" NOT IN ('Asignada','EnProcesoDespacho','EnTerreno') THEN RAISE EXCEPTION 'La OT ya se envió a revisión'; END IF;
  RETURN o;
END $$;
CREATE FUNCTION afrisan_actual.peso_valido(id_c bigint,peso bigint) RETURNS void LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
DECLARE c afrisan_actual."Cilindros";
BEGIN SELECT * INTO STRICT c FROM afrisan_actual."Cilindros" WHERE "Id_Cilindro"=id_c;
  IF peso<c."TaraGr" OR peso::numeric>c."TaraGr"::numeric+c."Capacidadgr" THEN RAISE EXCEPTION 'Peso fuera de tara/capacidad'; END IF;
END $$;
CREATE FUNCTION afrisan_actual.cilindro_guardar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
BEGIN
  IF TG_OP='INSERT' THEN
    NEW."Id_Registrador":=afrisan_actual.actor(ARRAY['Bodega','Administrador']); NEW."FechaRegistro":=clock_timestamp();
    IF NEW."Ubicacion" NOT IN ('Bodega','Sucursal') OR NEW."Id_UltimoPesaje" IS NOT NULL THEN RAISE EXCEPTION 'Alta solo en Bodega/Sucursal y sin pesaje ficticio'; END IF;
    NEW."Estado":=CASE WHEN NEW."Ubicacion"='Sucursal' THEN 'Instalado' ELSE 'PendientePesaje' END;
  ELSE
    IF (NEW."Id_Refrigerante",NEW."TaraGr",NEW."Capacidadgr",NEW."CilindroQR",NEW."Id_Registrador",NEW."FechaRegistro") IS DISTINCT FROM
       (OLD."Id_Refrigerante",OLD."TaraGr",OLD."Capacidadgr",OLD."CilindroQR",OLD."Id_Registrador",OLD."FechaRegistro") THEN RAISE EXCEPTION 'Identidad/tara/capacidad no se reescriben'; END IF;
    IF pg_trigger_depth()<2 THEN RAISE EXCEPTION 'Ubicación/peso se cambian mediante los registros operativos'; END IF;
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER "Cilindro_Validar" BEFORE INSERT OR UPDATE ON afrisan_actual."Cilindros" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.cilindro_guardar();
CREATE FUNCTION afrisan_actual.cilindro_historial() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
BEGIN
  INSERT INTO afrisan_actual."HistorialCilindro"("Id_Cilindro","Ubicacion","Estado","Id_Sucursal","Id_Tecnico","Id_Pesaje","Id_Usuario")
  VALUES(NEW."Id_Cilindro",NEW."Ubicacion",NEW."Estado",NEW."Id_Sucursal",NEW."Id_Tecnico",NEW."Id_UltimoPesaje",afrisan_actual.actor(ARRAY['Bodega','Tecnico','Administrador']));
  RETURN NULL;
END $$;
CREATE TRIGGER "Cilindro_Historial" AFTER INSERT OR UPDATE ON afrisan_actual."Cilindros" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.cilindro_historial();

CREATE FUNCTION afrisan_actual.pesaje_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
DECLARE c afrisan_actual."Cilindros"; o afrisan_actual."OrdenTrabajo";
BEGIN
  IF NEW."Tipo"='Inicial' THEN NEW."Id_Usuario":=afrisan_actual.actor(ARRAY['Bodega','Administrador']);
  ELSIF NEW."Tipo"='Recepcion' THEN o:=afrisan_actual.ot_actor(NEW."Id_OrdenTrabajo",ARRAY['Bodega']); NEW."Id_Usuario":=afrisan_actual.actor(ARRAY['Bodega']);
  ELSE o:=afrisan_actual.ot_actor(NEW."Id_OrdenTrabajo",ARRAY['Tecnico'],true); NEW."Id_Usuario":=o."Id_Tecnico"; END IF;
  SELECT * INTO STRICT c FROM afrisan_actual."Cilindros" WHERE "Id_Cilindro"=NEW."Id_Cilindro" FOR UPDATE;
  PERFORM afrisan_actual.peso_valido(c."Id_Cilindro",NEW."PesoBrutoGr");
  IF NEW."Tipo"='Inicial' AND (c."Id_UltimoPesaje" IS NOT NULL OR EXISTS(SELECT 1 FROM afrisan_actual."Pesaje" WHERE "Id_Cilindro"=c."Id_Cilindro")) THEN RAISE EXCEPTION 'Ya existe pesaje inicial u operativo'; END IF;
  IF NEW."Tipo"='Recepcion' AND (o."Estado"='Cerrada' OR EXISTS(SELECT 1 FROM afrisan_actual."RevisionOT" WHERE "Id_OrdenTrabajo"=o."Id_OrdenTrabajo")) THEN RAISE EXCEPTION 'Recepción ya revisada/cerrada'; END IF;
  NEW."Fecha":=clock_timestamp(); RETURN NEW;
END $$;
CREATE TRIGGER "Pesaje_Validar" BEFORE INSERT ON afrisan_actual."Pesaje" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.pesaje_validar();
CREATE FUNCTION afrisan_actual.pesaje_inicial_aplicar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
BEGIN
  IF NEW."Tipo"='Inicial' THEN
    UPDATE afrisan_actual."Cilindros" SET "Id_UltimoPesaje"=NEW."Id_Pesaje", "Estado"=CASE WHEN "Ubicacion"='Sucursal' THEN 'Instalado' WHEN NEW."PesoBrutoGr"="TaraGr" THEN 'Vacio' ELSE 'Disponible' END WHERE "Id_Cilindro"=NEW."Id_Cilindro";
  END IF; RETURN NULL;
END $$;
CREATE TRIGGER "Pesaje_Inicial_Aplicar" AFTER INSERT ON afrisan_actual."Pesaje" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.pesaje_inicial_aplicar();
CREATE FUNCTION afrisan_actual.evidencia_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
DECLARE p afrisan_actual."Pesaje"; o afrisan_actual."OrdenTrabajo";
BEGIN
  SELECT * INTO STRICT p FROM afrisan_actual."Pesaje" WHERE "Id_Pesaje"=NEW."Id_Pesaje";
  IF p."Id_OrdenTrabajo" IS NOT NULL THEN
    o:=afrisan_actual.ot_actor(p."Id_OrdenTrabajo",CASE WHEN p."Tipo"='Recepcion' THEN ARRAY['Bodega'] ELSE ARRAY['Tecnico'] END,p."Tipo"<>'Recepcion');
    IF EXISTS(SELECT 1 FROM afrisan_actual."RevisionOT" WHERE "Id_OrdenTrabajo"=p."Id_OrdenTrabajo") THEN RAISE EXCEPTION 'No se agregan fotos después de la revisión'; END IF;
  END IF;
  SELECT * INTO STRICT p FROM afrisan_actual."Pesaje" WHERE "Id_Pesaje"=NEW."Id_Pesaje" FOR UPDATE;
  NEW."Id_Usuario":=afrisan_actual.actor(CASE WHEN p."Tipo" IN ('Inicial','Recepcion') THEN ARRAY['Bodega','Administrador'] ELSE ARRAY['Tecnico'] END);
  IF p."Id_Usuario"<>NEW."Id_Usuario" THEN RAISE EXCEPTION 'La evidencia pertenece a otro registrador'; END IF;
  IF (SELECT count(*) FROM afrisan_actual."EvidenciaPesaje" WHERE "Id_Pesaje"=p."Id_Pesaje")>=8 THEN RAISE EXCEPTION 'Máximo ocho fotos por pesaje'; END IF;
  IF EXISTS(SELECT 1 FROM afrisan_actual."Despacho" WHERE "Id_Pesaje"=p."Id_Pesaje") OR EXISTS(SELECT 1 FROM afrisan_actual."RecepcionBodega" WHERE "Id_Pesaje"=p."Id_Pesaje") OR
     EXISTS(SELECT 1 FROM afrisan_actual."OperacionTerreno" WHERE "Estado"='Registrada' AND p."Id_Pesaje" IN ("Id_PesajeRetirado","Id_PesajeInstalado")) THEN RAISE EXCEPTION 'No se agregan fotos a pesajes confirmados'; END IF;
  NEW."Fecha":=clock_timestamp(); RETURN NEW;
END $$;
CREATE TRIGGER "Evidencia_Validar" BEFORE INSERT ON afrisan_actual."EvidenciaPesaje" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.evidencia_validar();

CREATE FUNCTION afrisan_actual.pedido_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
DECLARE o afrisan_actual."OrdenTrabajo"; c afrisan_actual."Cilindros";
BEGIN
  IF TG_OP='DELETE' THEN RAISE EXCEPTION 'No borrar el pedido; requiere procedimiento de anulación'; END IF;
  IF TG_TABLE_NAME='RetiroPrevisto' AND TG_OP='UPDATE' AND pg_trigger_depth()>1 THEN
    IF OLD."Estado"<>'Reservado' OR NEW."Estado" NOT IN ('Ejecutado','NoEjecutado') OR
       (NEW."Id_OrdenTrabajo",NEW."Id_Cilindro") IS DISTINCT FROM (OLD."Id_OrdenTrabajo",OLD."Id_Cilindro") THEN RAISE EXCEPTION 'Transición interna de reserva inválida'; END IF;
    IF NEW."Estado"='NoEjecutado' AND NOT EXISTS(SELECT 1 FROM afrisan_actual."OrdenTrabajo" WHERE "Id_OrdenTrabajo"=NEW."Id_OrdenTrabajo" AND "Estado"='Cerrada') THEN RAISE EXCEPTION 'Solo liberar un retiro no ejecutado al cerrar'; END IF;
    RETURN NEW;
  END IF;
  o:=afrisan_actual.ot_actor(NEW."Id_OrdenTrabajo",ARRAY['Autorizador']);
  IF o."Estado"<>'Asignada' OR TG_OP='UPDATE' THEN RAISE EXCEPTION 'Pedido autorizado inmutable; agregar detalle solo antes del despacho'; END IF;
  IF TG_TABLE_NAME='RetiroPrevisto' THEN
    SELECT * INTO STRICT c FROM afrisan_actual."Cilindros" WHERE "Id_Cilindro"=NEW."Id_Cilindro" FOR UPDATE;
    IF c."Ubicacion"<>'Sucursal' OR c."Id_Sucursal"<>o."Id_Sucursal" OR NOT EXISTS(SELECT 1 FROM afrisan_actual."DetalleOT" WHERE "Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND "Id_Refrigerante"=c."Id_Refrigerante") THEN RAISE EXCEPTION 'Retiro no corresponde a sucursal/gas encargado'; END IF;
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER "Detalle_Validar" BEFORE INSERT OR UPDATE OR DELETE ON afrisan_actual."DetalleOT" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.pedido_validar();
CREATE TRIGGER "RetiroPrevisto_Validar" BEFORE INSERT OR UPDATE OR DELETE ON afrisan_actual."RetiroPrevisto" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.pedido_validar();

CREATE FUNCTION afrisan_actual.despacho_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
DECLARE o afrisan_actual."OrdenTrabajo"; c afrisan_actual."Cilindros"; p afrisan_actual."Pesaje";
BEGIN
  IF TG_OP<>'INSERT' THEN
    IF TG_OP='DELETE' OR pg_trigger_depth()<2 OR (to_jsonb(NEW)-'Estado')<>(to_jsonb(OLD)-'Estado') OR OLD."Estado"<>'EnProcesoDespacho' THEN RAISE EXCEPTION 'Despacho confirmado no se modifica'; END IF;
    RETURN NEW;
  END IF;
  o:=afrisan_actual.ot_actor(NEW."Id_OrdenTrabajo",ARRAY['Tecnico'],true);
  SELECT * INTO STRICT c FROM afrisan_actual."Cilindros" WHERE "Id_Cilindro"=NEW."Id_Cilindro" FOR UPDATE;
  SELECT * INTO STRICT p FROM afrisan_actual."Pesaje" WHERE "Id_Pesaje"=NEW."Id_Pesaje";
  IF c."Ubicacion"<>'Bodega' OR c."Estado"<>'Disponible' OR c."Id_UltimoPesaje" IS NULL OR p."PesoBrutoGr"<=c."TaraGr" THEN RAISE EXCEPTION 'Cilindro no disponible/pesado/con contenido en Bodega'; END IF;
  IF NOT EXISTS(SELECT 1 FROM afrisan_actual."DetalleOT" WHERE "Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND "Id_Refrigerante"=c."Id_Refrigerante") THEN RAISE EXCEPTION 'Gas fuera del pedido'; END IF;
  IF p."Tipo"<>'Despacho' OR p."Id_OrdenTrabajo"<>o."Id_OrdenTrabajo" OR p."Id_Cilindro"<>c."Id_Cilindro" OR p."Id_Usuario"<>o."Id_Tecnico" THEN RAISE EXCEPTION 'Pesaje de salida ajeno'; END IF;
  IF NOT EXISTS(SELECT 1 FROM afrisan_actual."EvidenciaPesaje" WHERE "Id_Pesaje"=p."Id_Pesaje") THEN RAISE EXCEPTION 'Falta foto del peso de salida'; END IF;
  NEW."Id_Tecnico":=o."Id_Tecnico"; NEW."Fecha":=clock_timestamp(); NEW."Estado":='EnProcesoDespacho'; RETURN NEW;
END $$;
CREATE TRIGGER "Despacho_Validar" BEFORE INSERT OR UPDATE OR DELETE ON afrisan_actual."Despacho" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.despacho_validar();
CREATE FUNCTION afrisan_actual.despacho_aplicar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
BEGIN
  INSERT INTO afrisan_actual."CustodiaActual"("Id_Cilindro","Id_OrdenTrabajo","Id_Tecnico","Id_Despacho") VALUES(NEW."Id_Cilindro",NEW."Id_OrdenTrabajo",NEW."Id_Tecnico",NEW."Id_Despacho");
  UPDATE afrisan_actual."Cilindros" SET "Ubicacion"='Tecnico',"Id_Tecnico"=NEW."Id_Tecnico","Id_Sucursal"=NULL,"Estado"='EnProcesoDespacho',"Id_UltimoPesaje"=NEW."Id_Pesaje" WHERE "Id_Cilindro"=NEW."Id_Cilindro";
  UPDATE afrisan_actual."OrdenTrabajo" SET "Estado"='EnProcesoDespacho' WHERE "Id_OrdenTrabajo"=NEW."Id_OrdenTrabajo" AND "Estado"='Asignada';
  INSERT INTO afrisan_actual."EventoOT"("Id_OrdenTrabajo","Accion","Id_Usuario","Detalle") VALUES(NEW."Id_OrdenTrabajo",'Retiro de Bodega por Técnico',NEW."Id_Tecnico",NEW."Id_Cilindro"::text);
  RETURN NULL;
END $$;
CREATE TRIGGER "Despacho_Aplicar" AFTER INSERT ON afrisan_actual."Despacho" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.despacho_aplicar();

CREATE FUNCTION afrisan_actual.operacion_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
DECLARE o afrisan_actual."OrdenTrabajo"; n afrisan_actual."Cilindros"; v afrisan_actual."Cilindros"; p afrisan_actual."Pesaje";
BEGIN
  IF TG_OP='DELETE' THEN RAISE EXCEPTION 'No borrar operaciones; descartar borrador o solicitar rectificación'; END IF;
  o:=afrisan_actual.ot_actor(NEW."Id_OrdenTrabajo",ARRAY['Tecnico']);
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
  PERFORM 1 FROM afrisan_actual."Cilindros" WHERE "Id_Cilindro" IN (NEW."Id_Instalado",NEW."Id_Retirado") ORDER BY "Id_Cilindro" FOR UPDATE;
  SELECT * INTO STRICT n FROM afrisan_actual."Cilindros" WHERE "Id_Cilindro"=NEW."Id_Instalado";
  IF NOT EXISTS(SELECT 1 FROM afrisan_actual."CustodiaActual" WHERE "Id_Cilindro"=n."Id_Cilindro" AND "Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND "Id_Despacho"=NEW."Id_Despacho") THEN RAISE EXCEPTION 'Instalado fuera de custodia de esta OT'; END IF;
  SELECT * INTO STRICT p FROM afrisan_actual."Pesaje" WHERE "Id_Pesaje"=NEW."Id_PesajeInstalado";
  IF p."Tipo"<>'Instalado' OR p."Id_OrdenTrabajo"<>o."Id_OrdenTrabajo" OR p."Id_Cilindro"<>n."Id_Cilindro" THEN RAISE EXCEPTION 'Pesaje instalado incompatible'; END IF;
  IF NOT EXISTS(SELECT 1 FROM afrisan_actual."EvidenciaPesaje" WHERE "Id_Pesaje"=p."Id_Pesaje") THEN RAISE EXCEPTION 'Falta foto del instalado'; END IF;
  IF NEW."Tipo"='Reemplazo' THEN
    IF NEW."Id_Retirado" IS NULL OR NEW."Id_PesajeRetirado" IS NULL THEN RAISE EXCEPTION 'Falta cilindro/pesaje retirado'; END IF;
    SELECT * INTO STRICT v FROM afrisan_actual."Cilindros" WHERE "Id_Cilindro"=NEW."Id_Retirado";
    IF v."Ubicacion"<>'Sucursal' OR v."Id_Sucursal"<>o."Id_Sucursal" OR v."Id_Refrigerante"<>n."Id_Refrigerante" OR NOT EXISTS(SELECT 1 FROM afrisan_actual."RetiroPrevisto" WHERE "Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND "Id_Cilindro"=v."Id_Cilindro" AND "Estado"='Reservado') THEN RAISE EXCEPTION 'Retirado de otra sucursal, gas o pedido'; END IF;
    SELECT * INTO STRICT p FROM afrisan_actual."Pesaje" WHERE "Id_Pesaje"=NEW."Id_PesajeRetirado";
    IF p."Tipo"<>'Retirado' OR p."Id_OrdenTrabajo"<>o."Id_OrdenTrabajo" OR p."Id_Cilindro"<>v."Id_Cilindro" THEN RAISE EXCEPTION 'Pesaje retirado incompatible'; END IF;
    IF NOT EXISTS(SELECT 1 FROM afrisan_actual."EvidenciaPesaje" WHERE "Id_Pesaje"=p."Id_Pesaje") THEN RAISE EXCEPTION 'Falta foto del retirado'; END IF;
    IF EXISTS(SELECT 1 FROM afrisan_actual."EvidenciaPesaje" a JOIN afrisan_actual."EvidenciaPesaje" b USING("SHA256") WHERE a."Id_Pesaje"=NEW."Id_PesajeRetirado" AND b."Id_Pesaje"=NEW."Id_PesajeInstalado") THEN RAISE EXCEPTION 'No reutilizar la misma foto para ambos cilindros'; END IF;
  END IF;
  NEW."FechaConfirmacion":=clock_timestamp(); RETURN NEW;
END $$;
CREATE TRIGGER "Operacion_Validar" BEFORE INSERT OR UPDATE OR DELETE ON afrisan_actual."OperacionTerreno" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.operacion_validar();
CREATE FUNCTION afrisan_actual.operacion_aplicar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
DECLARE o afrisan_actual."OrdenTrabajo";
BEGIN
  IF NEW."Estado"<>'Registrada' OR OLD."Estado"='Registrada' THEN RETURN NULL; END IF;
  SELECT * INTO STRICT o FROM afrisan_actual."OrdenTrabajo" WHERE "Id_OrdenTrabajo"=NEW."Id_OrdenTrabajo";
  DELETE FROM afrisan_actual."CustodiaActual" WHERE "Id_Cilindro"=NEW."Id_Instalado";
  UPDATE afrisan_actual."Despacho" SET "Estado"='Instalado' WHERE "Id_Despacho"=NEW."Id_Despacho";
  UPDATE afrisan_actual."Cilindros" SET "Ubicacion"='Sucursal',"Id_Sucursal"=o."Id_Sucursal","Id_Tecnico"=NULL,"Estado"='Instalado',"Id_UltimoPesaje"=NEW."Id_PesajeInstalado" WHERE "Id_Cilindro"=NEW."Id_Instalado";
  IF NEW."Tipo"='Reemplazo' THEN
    INSERT INTO afrisan_actual."CustodiaActual"("Id_Cilindro","Id_OrdenTrabajo","Id_Tecnico","Id_Operacion") VALUES(NEW."Id_Retirado",NEW."Id_OrdenTrabajo",NEW."Id_Tecnico",NEW."Id_Operacion");
    UPDATE afrisan_actual."Cilindros" SET "Ubicacion"='Tecnico',"Id_Sucursal"=NULL,"Id_Tecnico"=NEW."Id_Tecnico","Estado"='RetiradoPendienteRecepcion',"Id_UltimoPesaje"=NEW."Id_PesajeRetirado" WHERE "Id_Cilindro"=NEW."Id_Retirado";
    UPDATE afrisan_actual."RetiroPrevisto" SET "Estado"='Ejecutado' WHERE "Id_OrdenTrabajo"=NEW."Id_OrdenTrabajo" AND "Id_Cilindro"=NEW."Id_Retirado";
  END IF;
  UPDATE afrisan_actual."OrdenTrabajo" SET "Estado"='EnTerreno' WHERE "Id_OrdenTrabajo"=NEW."Id_OrdenTrabajo" AND "Estado"<>'EnTerreno';
  INSERT INTO afrisan_actual."EventoOT"("Id_OrdenTrabajo","Accion","Id_Usuario","Detalle") VALUES(NEW."Id_OrdenTrabajo",'Cambio físico confirmado',NEW."Id_Tecnico",NEW."Id_Operacion"::text);
  RETURN NULL;
END $$;
CREATE TRIGGER "Operacion_Aplicar" AFTER UPDATE ON afrisan_actual."OperacionTerreno" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.operacion_aplicar();

CREATE FUNCTION afrisan_actual.recepcion_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
DECLARE o afrisan_actual."OrdenTrabajo"; p afrisan_actual."Pesaje";
BEGIN
  o:=afrisan_actual.ot_actor(NEW."Id_OrdenTrabajo",ARRAY['Bodega']);
  IF o."Estado"='Cerrada' OR EXISTS(SELECT 1 FROM afrisan_actual."RevisionOT" WHERE "Id_OrdenTrabajo"=o."Id_OrdenTrabajo") THEN RAISE EXCEPTION 'OT ya aprobada'; END IF;
  PERFORM 1 FROM afrisan_actual."Cilindros" WHERE "Id_Cilindro"=NEW."Id_Cilindro" FOR UPDATE;
  IF NOT EXISTS(SELECT 1 FROM afrisan_actual."CustodiaActual" WHERE "Id_Cilindro"=NEW."Id_Cilindro" AND "Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND "Id_Tecnico"=o."Id_Tecnico") THEN RAISE EXCEPTION 'Cilindro fuera de custodia de esta OT'; END IF;
  SELECT * INTO STRICT p FROM afrisan_actual."Pesaje" WHERE "Id_Pesaje"=NEW."Id_Pesaje";
  IF p."Tipo"<>'Recepcion' OR p."Id_OrdenTrabajo"<>o."Id_OrdenTrabajo" OR p."Id_Cilindro"<>NEW."Id_Cilindro" OR p."Id_Usuario"<>afrisan_actual.actor(ARRAY['Bodega']) THEN RAISE EXCEPTION 'Pesaje de recepción incompatible'; END IF;
  IF NEW."Id_Operacion" IS NOT NULL THEN
    IF NOT EXISTS(SELECT 1 FROM afrisan_actual."OperacionTerreno" WHERE "Id_Operacion"=NEW."Id_Operacion" AND "Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND "Estado"='Registrada' AND "Id_Retirado"=NEW."Id_Cilindro" AND "FechaEntregaInformada" IS NOT NULL) THEN RAISE EXCEPTION 'Falta devolución informada del retirado'; END IF;
  ELSE
    IF NOT EXISTS(SELECT 1 FROM afrisan_actual."Despacho" WHERE "Id_Despacho"=NEW."Id_DespachoSinUso" AND "Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND "Id_Cilindro"=NEW."Id_Cilindro" AND "Estado"='EnProcesoDespacho') THEN RAISE EXCEPTION 'No corresponde a devolución sin uso'; END IF;
  END IF;
  NEW."Id_Bodeguero":=afrisan_actual.actor(ARRAY['Bodega']); NEW."Fecha":=clock_timestamp(); RETURN NEW;
END $$;
CREATE TRIGGER "Recepcion_Validar" BEFORE INSERT ON afrisan_actual."RecepcionBodega" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.recepcion_validar();
CREATE FUNCTION afrisan_actual.recepcion_aplicar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
BEGIN
  DELETE FROM afrisan_actual."CustodiaActual" WHERE "Id_Cilindro"=NEW."Id_Cilindro";
  UPDATE afrisan_actual."Cilindros" c SET "Ubicacion"='Bodega',"Id_Sucursal"=NULL,"Id_Tecnico"=NULL,"Id_UltimoPesaje"=NEW."Id_Pesaje",
    "Estado"=CASE WHEN p."PesoBrutoGr"=c."TaraGr" THEN 'Vacio' ELSE 'Disponible' END FROM afrisan_actual."Pesaje" p WHERE c."Id_Cilindro"=NEW."Id_Cilindro" AND p."Id_Pesaje"=NEW."Id_Pesaje";
  IF NEW."Id_DespachoSinUso" IS NOT NULL THEN UPDATE afrisan_actual."Despacho" SET "Estado"='Devuelto' WHERE "Id_Despacho"=NEW."Id_DespachoSinUso"; END IF;
  INSERT INTO afrisan_actual."EventoOT"("Id_OrdenTrabajo","Accion","Id_Usuario","Detalle") VALUES(NEW."Id_OrdenTrabajo",'Recepción oficial Bodega',NEW."Id_Bodeguero",NEW."Id_Cilindro"::text);
  RETURN NULL;
END $$;
CREATE TRIGGER "Recepcion_Aplicar" AFTER INSERT ON afrisan_actual."RecepcionBodega" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.recepcion_aplicar();

CREATE FUNCTION afrisan_actual.sin_pendientes(id_ot bigint) RETURNS void LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
BEGIN
  IF NOT EXISTS(SELECT 1 FROM afrisan_actual."OperacionTerreno" WHERE "Id_OrdenTrabajo"=id_ot AND "Estado"='Registrada') OR
     EXISTS(SELECT 1 FROM afrisan_actual."OperacionTerreno" WHERE "Id_OrdenTrabajo"=id_ot AND "Estado"='Borrador') THEN RAISE EXCEPTION 'Falta trabajo confirmado o hay borradores pendientes'; END IF;
  IF EXISTS(SELECT 1 FROM afrisan_actual."Despacho" WHERE "Id_OrdenTrabajo"=id_ot AND "Estado"='EnProcesoDespacho') THEN RAISE EXCEPTION 'Hay cilindros sin instalar ni devolver'; END IF;
  IF EXISTS(SELECT 1 FROM afrisan_actual."OperacionTerreno" op WHERE op."Id_OrdenTrabajo"=id_ot AND op."Estado"='Registrada' AND op."Tipo"='Reemplazo' AND NOT EXISTS(SELECT 1 FROM afrisan_actual."RecepcionBodega" r WHERE r."Id_Operacion"=op."Id_Operacion")) THEN RAISE EXCEPTION 'Hay retirados sin recepción de Bodega'; END IF;
END $$;
CREATE FUNCTION afrisan_actual.revision_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
DECLARE o afrisan_actual."OrdenTrabajo";
BEGIN
  o:=afrisan_actual.ot_actor(NEW."Id_OrdenTrabajo",ARRAY[NEW."Etapa"]);
  PERFORM afrisan_actual.sin_pendientes(o."Id_OrdenTrabajo");
  IF NEW."Etapa"='Bodega' AND o."Estado"<>'PendienteBodega' THEN RAISE EXCEPTION 'Primero término técnico'; END IF;
  IF NEW."Etapa"='Autorizador' THEN
    IF o."Estado"<>'PendienteRevision' OR NOT EXISTS(SELECT 1 FROM afrisan_actual."RevisionOT" WHERE "Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND "Etapa"='Bodega') THEN RAISE EXCEPTION 'Primero aprobación de Bodega'; END IF;
    IF EXISTS(SELECT 1 FROM afrisan_actual."Incidencia" WHERE "Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND "Respuesta" IS NULL) THEN RAISE EXCEPTION 'Incidencias sin revisar'; END IF;
  END IF;
  NEW."Id_Usuario":=afrisan_actual.actor(ARRAY[NEW."Etapa"]); NEW."Fecha":=clock_timestamp(); RETURN NEW;
END $$;
CREATE TRIGGER "Revision_Validar" BEFORE INSERT ON afrisan_actual."RevisionOT" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.revision_validar();
CREATE FUNCTION afrisan_actual.revision_aplicar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
BEGIN
  IF NEW."Etapa"='Bodega' THEN UPDATE afrisan_actual."OrdenTrabajo" SET "Estado"='PendienteRevision' WHERE "Id_OrdenTrabajo"=NEW."Id_OrdenTrabajo"; END IF;
  INSERT INTO afrisan_actual."EventoOT"("Id_OrdenTrabajo","Accion","Id_Usuario","Detalle") VALUES(NEW."Id_OrdenTrabajo",'Revisión '||NEW."Etapa",NEW."Id_Usuario",NEW."Observacion");
  RETURN NULL;
END $$;
CREATE TRIGGER "Revision_Aplicar" AFTER INSERT ON afrisan_actual."RevisionOT" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.revision_aplicar();

CREATE FUNCTION afrisan_actual.ot_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
DECLARE a integer;
BEGIN
  IF TG_OP='DELETE' THEN RAISE EXCEPTION 'No borrar OT'; END IF;
  IF TG_OP='INSERT' THEN
    a:=afrisan_actual.actor(ARRAY['Autorizador']); NEW."Id_Autorizador":=a; NEW."FechaCreacion":=clock_timestamp();
    IF NEW."Estado"<>'Asignada' OR NEW."FechaTerminoTecnico" IS NOT NULL THEN RAISE EXCEPTION 'Crear OT asignada, no cerrada'; END IF;
    IF NOT EXISTS(SELECT 1 FROM afrisan_actual."Usuario" u JOIN afrisan_actual."Rol" r USING("Id_Rol") WHERE u."Id_Usuario"=NEW."Id_Tecnico" AND u."Activo" AND r."Nombre_Rol"='Tecnico') THEN RAISE EXCEPTION 'Técnico asignado inválido'; END IF;
    RETURN NEW;
  END IF;
  IF (to_jsonb(NEW)-ARRAY['Estado','FechaTerminoTecnico','ObservacionTermino','FechaCierre'])<>(to_jsonb(OLD)-ARRAY['Estado','FechaTerminoTecnico','ObservacionTermino','FechaCierre']) THEN RAISE EXCEPTION 'No reescribir OT autorizada'; END IF;
  IF NEW."Estado"=OLD."Estado" THEN RAISE EXCEPTION 'Sin transición válida'; END IF;
  IF NEW."Estado"='EnProcesoDespacho' AND OLD."Estado"='Asignada' THEN
    a:=afrisan_actual.actor(ARRAY['Tecnico']);
    IF NOT EXISTS(SELECT 1 FROM afrisan_actual."Despacho" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo") THEN RAISE EXCEPTION 'Falta despacho'; END IF;
  ELSIF NEW."Estado"='EnTerreno' AND OLD."Estado" IN ('EnProcesoDespacho','Asignada') THEN
    a:=afrisan_actual.actor(ARRAY['Tecnico']);
    IF NOT EXISTS(SELECT 1 FROM afrisan_actual."OperacionTerreno" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo" AND "Estado"='Registrada') THEN RAISE EXCEPTION 'Falta cambio confirmado'; END IF;
  ELSIF NEW."Estado"='PendienteBodega' AND OLD."Estado"='EnTerreno' THEN
    a:=afrisan_actual.actor(ARRAY['Tecnico']);
    IF EXISTS(SELECT 1 FROM afrisan_actual."OperacionTerreno" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo" AND "Estado"='Borrador') THEN RAISE EXCEPTION 'Resolver borradores antes de terminar'; END IF;
    IF EXISTS(SELECT 1 FROM afrisan_actual."RetiroPrevisto" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo" AND "Estado"='Reservado') AND coalesce(btrim(NEW."ObservacionTermino"),'')='' THEN RAISE EXCEPTION 'Explicar ejecución parcial'; END IF;
    NEW."FechaTerminoTecnico":=clock_timestamp();
  ELSIF NEW."Estado"='PendienteRevision' AND OLD."Estado"='PendienteBodega' THEN
    a:=afrisan_actual.actor(ARRAY['Bodega']);
    IF NOT EXISTS(SELECT 1 FROM afrisan_actual."RevisionOT" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo" AND "Etapa"='Bodega') THEN RAISE EXCEPTION 'Falta aprobación Bodega'; END IF;
  ELSIF NEW."Estado"='Cerrada' AND OLD."Estado"='PendienteRevision' THEN
    a:=afrisan_actual.actor(ARRAY['Autorizador']); PERFORM afrisan_actual.sin_pendientes(OLD."Id_OrdenTrabajo");
    IF (SELECT count(*) FROM afrisan_actual."RevisionOT" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo")<>2 OR EXISTS(SELECT 1 FROM afrisan_actual."Incidencia" WHERE "Id_OrdenTrabajo"=OLD."Id_OrdenTrabajo" AND "Respuesta" IS NULL) THEN RAISE EXCEPTION 'Faltan revisiones/gestión de incidencias'; END IF;
    NEW."FechaCierre":=clock_timestamp();
  ELSE RAISE EXCEPTION 'Transición de OT no permitida'; END IF;
  IF NEW."Estado" IN ('EnProcesoDespacho','EnTerreno','PendienteBodega') AND a<>OLD."Id_Tecnico" THEN RAISE EXCEPTION 'OT de otro técnico'; END IF;
  IF NEW."Estado"<>'PendienteBodega' AND (NEW."FechaTerminoTecnico",NEW."ObservacionTermino") IS DISTINCT FROM (OLD."FechaTerminoTecnico",OLD."ObservacionTermino") THEN RAISE EXCEPTION 'No sobrescribir informe técnico'; END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER "OT_Validar" BEFORE INSERT OR UPDATE OR DELETE ON afrisan_actual."OrdenTrabajo" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.ot_validar();
CREATE FUNCTION afrisan_actual.ot_cierre_aplicar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
BEGIN
  IF NEW."Estado"='Cerrada' AND OLD."Estado"<>'Cerrada' THEN
    UPDATE afrisan_actual."RetiroPrevisto" SET "Estado"='NoEjecutado' WHERE "Id_OrdenTrabajo"=NEW."Id_OrdenTrabajo" AND "Estado"='Reservado';
    INSERT INTO afrisan_actual."EventoOT"("Id_OrdenTrabajo","Accion","Id_Usuario") VALUES(NEW."Id_OrdenTrabajo",'Cierre administrativo',afrisan_actual.actor(ARRAY['Autorizador']));
  END IF; RETURN NULL;
END $$;
CREATE TRIGGER "OT_Cierre_Aplicar" AFTER UPDATE ON afrisan_actual."OrdenTrabajo" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.ot_cierre_aplicar();

CREATE FUNCTION afrisan_actual.incidencia_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
DECLARE o afrisan_actual."OrdenTrabajo";
BEGIN
  IF TG_OP='DELETE' THEN RAISE EXCEPTION 'No borrar incidencias'; END IF;
  IF TG_OP='INSERT' THEN
    o:=afrisan_actual.ot_actor(NEW."Id_OrdenTrabajo",ARRAY['Tecnico','Bodega']);
    IF o."Estado"='Cerrada' OR EXISTS(SELECT 1 FROM afrisan_actual."RevisionOT" WHERE "Id_OrdenTrabajo"=o."Id_OrdenTrabajo") OR NEW."Respuesta" IS NOT NULL OR NEW."Id_Revisor" IS NOT NULL OR NEW."FechaRevision" IS NOT NULL THEN RAISE EXCEPTION 'Incidencia fuera de etapa'; END IF;
    IF NEW."Id_Operacion" IS NOT NULL AND NOT EXISTS(SELECT 1 FROM afrisan_actual."OperacionTerreno" WHERE "Id_Operacion"=NEW."Id_Operacion" AND "Id_OrdenTrabajo"=o."Id_OrdenTrabajo") THEN RAISE EXCEPTION 'Operación de otra OT'; END IF;
    NEW."Id_Usuario":=afrisan_actual.actor(ARRAY['Tecnico','Bodega']); NEW."Fecha":=clock_timestamp();
  ELSE
    o:=afrisan_actual.ot_actor(OLD."Id_OrdenTrabajo",ARRAY['Autorizador']);
    IF o."Estado"<>'PendienteRevision' OR OLD."Respuesta" IS NOT NULL OR NEW."Respuesta" IS NULL OR
      (to_jsonb(NEW)-ARRAY['Respuesta','Id_Revisor','FechaRevision'])<>(to_jsonb(OLD)-ARRAY['Respuesta','Id_Revisor','FechaRevision']) THEN RAISE EXCEPTION 'Solo revisar una incidencia pendiente después de Bodega'; END IF;
    NEW."Id_Revisor":=afrisan_actual.actor(ARRAY['Autorizador']); NEW."FechaRevision":=clock_timestamp();
  END IF; RETURN NEW;
END $$;
CREATE TRIGGER "Incidencia_Validar" BEFORE INSERT OR UPDATE OR DELETE ON afrisan_actual."Incidencia" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.incidencia_validar();

-- Función de retiro atómica e idempotente. fotos es array JSON de metadatos, no binarios.
CREATE FUNCTION afrisan_actual.retirar_cilindro(id uuid,ot bigint,cilindro bigint,peso bigint,fotos jsonb) RETURNS afrisan_actual."Despacho"
LANGUAGE plpgsql SET search_path=pg_catalog,afrisan_actual AS $$
DECLARE d afrisan_actual."Despacho"; firma text; a integer; f jsonb;
BEGIN
  a:=afrisan_actual.actor(ARRAY['Tecnico']);
  PERFORM afrisan_actual.ot_actor(ot,ARRAY['Tecnico']);
  firma:=encode(sha256(convert_to(jsonb_build_array(ot,cilindro,peso,fotos)::text,'UTF8')),'hex');
  SELECT * INTO d FROM afrisan_actual."Despacho" WHERE "Id_Despacho"=id;
  IF FOUND THEN
    IF d."HuellaSolicitud"<>firma OR d."Id_Tecnico"<>a THEN RAISE EXCEPTION 'UUID usado con otra solicitud'; END IF;
    RETURN d;
  END IF;
  IF jsonb_typeof(fotos) IS DISTINCT FROM 'array' OR jsonb_array_length(fotos) NOT BETWEEN 1 AND 8 THEN RAISE EXCEPTION 'Adjuntar entre una y ocho fotos del peso'; END IF;
  INSERT INTO afrisan_actual."Pesaje"("Id_Pesaje","Id_Cilindro","Id_OrdenTrabajo","Tipo","PesoBrutoGr","Id_Usuario") VALUES(id,cilindro,ot,'Despacho',peso,a);
  FOR f IN SELECT * FROM jsonb_array_elements(fotos) LOOP
    INSERT INTO afrisan_actual."EvidenciaPesaje"("Id_Evidencia","Id_Pesaje","BucketPrivado","RutaObjeto","NombreArchivo","ContentType","Bytes","SHA256","Id_Usuario")
    VALUES((f->>'id')::uuid,id,f->>'bucket',f->>'ruta',f->>'nombre',f->>'mime',(f->>'bytes')::bigint,f->>'sha256',a);
  END LOOP;
  INSERT INTO afrisan_actual."Despacho"("Id_Despacho","Id_OrdenTrabajo","Id_Cilindro","Id_Pesaje","Id_Tecnico","HuellaSolicitud") VALUES(id,ot,cilindro,id,a,firma) RETURNING * INTO d;
  RETURN d;
END $$;

-- Registros de hechos inmutables. Custodia es una proyección operativa, no historial.
DO $$ DECLARE t text; BEGIN
  FOREACH t IN ARRAY ARRAY['Pesaje','EvidenciaPesaje','RecepcionBodega','RevisionOT','HistorialCilindro','EventoOT'] LOOP
    EXECUTE format('CREATE TRIGGER %I BEFORE UPDATE OR DELETE ON afrisan_actual.%I FOR EACH ROW EXECUTE FUNCTION afrisan_actual.inmutable()','Inmutable_'||t,t);
  END LOOP;
END $$;
CREATE FUNCTION afrisan_actual.custodia_proteger() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF pg_trigger_depth()<2 THEN RAISE EXCEPTION 'Custodia cambia solo al despachar, confirmar cambio o recibir'; END IF;
  IF TG_OP='DELETE' THEN RETURN OLD; END IF; RETURN NEW;
END $$;
CREATE TRIGGER "Custodia_Proteger" BEFORE INSERT OR UPDATE OR DELETE ON afrisan_actual."CustodiaActual" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.custodia_proteger();

CREATE VIEW afrisan_bi_actual."Inventario" AS
SELECT c."Id_Cilindro",c."CilindroQR",c."Id_Refrigerante",c."Ubicacion",c."Estado",c."Id_Sucursal",c."Id_Tecnico",
 c."TaraGr",c."Capacidadgr",p."PesoBrutoGr",p."PesoBrutoGr"-c."TaraGr" AS "ContenidoSegunPesajeGr",p."Fecha" AS "FechaUltimoPesaje"
FROM afrisan_actual."Cilindros" c LEFT JOIN afrisan_actual."Pesaje" p ON p."Id_Pesaje"=c."Id_UltimoPesaje";
CREATE VIEW afrisan_bi_actual."ResumenOT" AS
SELECT o."Id_OrdenTrabajo",o."CodigoOT",o."Id_Sucursal",o."Id_Tecnico",o."Estado",o."FechaCreacion",o."FechaCierre",
 (SELECT count(*) FROM afrisan_actual."Despacho" d WHERE d."Id_OrdenTrabajo"=o."Id_OrdenTrabajo") AS "CilindrosDespachados",
 (SELECT count(*) FROM afrisan_actual."OperacionTerreno" op WHERE op."Id_OrdenTrabajo"=o."Id_OrdenTrabajo" AND op."Estado"='Registrada') AS "OperacionesConfirmadas"
FROM afrisan_actual."OrdenTrabajo" o;
CREATE VIEW afrisan_bi_actual."PesajesRecepcion" AS
SELECT r."Id_Recepcion",r."Id_OrdenTrabajo",r."Id_Cilindro",r."Id_Operacion",r."Id_DespachoSinUso",r."Fecha",
 pr."PesoBrutoGr" AS "PesoRecepcionGr",pt."PesoBrutoGr" AS "PesoTerrenoGr",pd."PesoBrutoGr" AS "PesoSalidaSinUsoGr",
 pr."PesoBrutoGr"-coalesce(pt."PesoBrutoGr",pd."PesoBrutoGr") AS "DiferenciaGr"
FROM afrisan_actual."RecepcionBodega" r JOIN afrisan_actual."Pesaje" pr ON pr."Id_Pesaje"=r."Id_Pesaje"
LEFT JOIN afrisan_actual."OperacionTerreno" op ON op."Id_Operacion"=r."Id_Operacion"
LEFT JOIN afrisan_actual."Pesaje" pt ON pt."Id_Pesaje"=op."Id_PesajeRetirado"
LEFT JOIN afrisan_actual."Despacho" d ON d."Id_Despacho"=r."Id_DespachoSinUso"
LEFT JOIN afrisan_actual."Pesaje" pd ON pd."Id_Pesaje"=d."Id_Pesaje";

-- COMPONENTE: 02_soporte.sql
-- Módulos de soporte del contexto actual. Ejecutar después de 01_operacion.sql.
-- No inserta roles, usuarios, catálogos ni datos de ejemplo.
-- En el archivo canónico este módulo comparte la transacción de instalación.

-- LÍMITES: el criterio de cómputo sigue requiriendo acuerdo de negocio.
-- Se guardan límites y mediciones declaradas, no un consumo/emisión inferidos.
CREATE TABLE afrisan_actual."LimiteSucursal" (
  "Id_Limite" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_Sucursal" integer NOT NULL REFERENCES afrisan_actual."Sucursales" ON DELETE RESTRICT,
  "Periodicidad" varchar(10) NOT NULL CHECK ("Periodicidad" IN ('Semana','Mes')),
  "EstadoDefinicion" varchar(15) NOT NULL DEFAULT 'Pendiente'
    CHECK ("EstadoDefinicion" IN ('Pendiente','Definido')),
  "GrLimite" bigint CHECK ("GrLimite" > 0),
  "CriterioComputo" text,
  "VigenteDesde" timestamptz NOT NULL DEFAULT now(),
  "BloqueadoBodega" boolean NOT NULL DEFAULT false,
  "Version" integer NOT NULL DEFAULT 1 CHECK ("Version" > 0),
  "Id_UltimoEditor" integer NOT NULL REFERENCES afrisan_actual."Usuario" ON DELETE RESTRICT,
  "FechaActualizacion" timestamptz NOT NULL DEFAULT now(),
  UNIQUE ("Id_Sucursal", "Periodicidad"),
  CHECK ("EstadoDefinicion" <> 'Definido' OR
    ("GrLimite" IS NOT NULL AND coalesce(btrim("CriterioComputo"),'') <> ''))
);

CREATE TABLE afrisan_actual."LimiteSucursalHistorial" (
  "Id_HistorialLimite" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_Limite" bigint NOT NULL REFERENCES afrisan_actual."LimiteSucursal" ON DELETE RESTRICT,
  "Version" integer NOT NULL,
  "Id_Sucursal" integer NOT NULL REFERENCES afrisan_actual."Sucursales" ON DELETE RESTRICT,
  "Periodicidad" varchar(10) NOT NULL,
  "EstadoDefinicion" varchar(15) NOT NULL,
  "GrLimite" bigint,
  "CriterioComputo" text,
  "VigenteDesde" timestamptz NOT NULL,
  "BloqueadoBodega" boolean NOT NULL,
  "Id_Actor" integer NOT NULL REFERENCES afrisan_actual."Usuario" ON DELETE RESTRICT,
  "FechaRegistro" timestamptz NOT NULL,
  UNIQUE ("Id_Limite", "Version")
);

CREATE FUNCTION afrisan_actual.validar_limite_sucursal() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog, afrisan_actual AS $$
DECLARE v_actor integer; v_rol text; v_cambio boolean;
BEGIN
  IF TG_OP = 'DELETE' THEN
    RAISE EXCEPTION 'El límite y su historial se conservan; no se eliminan';
  END IF;
  v_actor := afrisan_actual.actor(ARRAY['Bodega','Jefatura']);
  SELECT r."Nombre_Rol" INTO v_rol FROM afrisan_actual."Usuario" u
    JOIN afrisan_actual."Rol" r ON r."Id_Rol" = u."Id_Rol"
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
ON afrisan_actual."LimiteSucursal" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.validar_limite_sucursal();

CREATE FUNCTION afrisan_actual.historiar_limite_sucursal() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog, afrisan_actual AS $$
BEGIN
  INSERT INTO afrisan_actual."LimiteSucursalHistorial"
    ("Id_Limite","Version","Id_Sucursal","Periodicidad","EstadoDefinicion","GrLimite",
     "CriterioComputo","VigenteDesde","BloqueadoBodega","Id_Actor","FechaRegistro")
  VALUES (NEW."Id_Limite",NEW."Version",NEW."Id_Sucursal",NEW."Periodicidad",NEW."EstadoDefinicion",
    NEW."GrLimite",NEW."CriterioComputo",NEW."VigenteDesde",NEW."BloqueadoBodega",
    NEW."Id_UltimoEditor",NEW."FechaActualizacion");
  RETURN NEW;
END $$;
CREATE TRIGGER "TR_Limite_Historial" AFTER INSERT OR UPDATE ON afrisan_actual."LimiteSucursal"
FOR EACH ROW EXECUTE FUNCTION afrisan_actual.historiar_limite_sucursal();

CREATE FUNCTION afrisan_actual.proteger_historial_limite() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog, afrisan_actual AS $$
BEGIN
  IF TG_OP <> 'INSERT' OR pg_trigger_depth() < 2 THEN
    RAISE EXCEPTION 'El historial de límites solo se genera al registrar una versión del límite';
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER "TR_LimiteHistorial_Inmutable" BEFORE INSERT OR UPDATE OR DELETE
ON afrisan_actual."LimiteSucursalHistorial" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.proteger_historial_limite();

CREATE TABLE afrisan_actual."MedicionLimite" (
  "Id_Medicion" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_HistorialLimite" bigint NOT NULL REFERENCES afrisan_actual."LimiteSucursalHistorial" ON DELETE RESTRICT,
  "PeriodoInicio" date NOT NULL,
  "PeriodoFin" date NOT NULL,
  "GrComputados" bigint NOT NULL CHECK ("GrComputados" >= 0),
  "MetodologiaComputo" text NOT NULL CHECK (btrim("MetodologiaComputo") <> ''),
  "Observaciones" text,
  "Id_Registrador" integer NOT NULL REFERENCES afrisan_actual."Usuario" ON DELETE RESTRICT,
  "FechaRegistro" timestamptz NOT NULL DEFAULT now(),
  CHECK ("PeriodoFin" >= "PeriodoInicio")
);
COMMENT ON TABLE afrisan_actual."MedicionLimite" IS
  'Cómputo declarado con metodología explícita y límite histórico. No calcula automáticamente consumo ni el criterio de semana/mes; los períodos se indican expresamente hasta acordar calendario, masa y desglose.';
CREATE TABLE afrisan_actual."MedicionLimiteOperaciones" (
  "Id_Medicion" bigint NOT NULL REFERENCES afrisan_actual."MedicionLimite" ON DELETE RESTRICT,
  "Id_Operacion" uuid NOT NULL REFERENCES afrisan_actual."OperacionTerreno" ON DELETE RESTRICT,
  PRIMARY KEY ("Id_Medicion", "Id_Operacion")
);
CREATE INDEX "IX_MedicionLimite_Periodo" ON afrisan_actual."MedicionLimite" ("Id_HistorialLimite","PeriodoInicio","PeriodoFin");
CREATE INDEX "IX_MedicionLimite_Operacion" ON afrisan_actual."MedicionLimiteOperaciones" ("Id_Operacion");
CREATE TABLE afrisan_actual."RevisionExceso" (
  "Id_RevisionExceso" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_Medicion" bigint NOT NULL UNIQUE REFERENCES afrisan_actual."MedicionLimite" ON DELETE RESTRICT,
  "Conclusion" text NOT NULL CHECK (length(btrim("Conclusion")) >= 5),
  "Id_Revisor" integer NOT NULL REFERENCES afrisan_actual."Usuario" ON DELETE RESTRICT,
  "FechaRevision" timestamptz NOT NULL DEFAULT now()
);
COMMENT ON TABLE afrisan_actual."RevisionExceso" IS
  'Conclusión expresa de Jefatura sobre un exceso declarado. Superar un límite no atribuye culpa ni demuestra una pérdida física.';

-- VALORACIÓN: cantidades y conceptos explícitos; no se derivan del reemplazo.
CREATE TABLE afrisan_actual."PreciosRefrigerantes" (
  "Id_Precio" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_Refrigerante" bigint NOT NULL REFERENCES afrisan_actual."Refrigerantes" ON DELETE RESTRICT,
  "PrecioGrRefrigerante" numeric(18,8) NOT NULL CHECK ("PrecioGrRefrigerante" >= 0),
  "Moneda" varchar(3) NOT NULL CHECK ("Moneda" ~ '^[A-Z]{3}$'),
  "VigenteDesde" timestamptz NOT NULL,
  "Id_Registrador" integer NOT NULL REFERENCES afrisan_actual."Usuario" ON DELETE RESTRICT,
  "FechaRegistro" timestamptz NOT NULL DEFAULT now(),
  UNIQUE ("Id_Refrigerante","Moneda","VigenteDesde")
);
CREATE TABLE afrisan_actual."Costos" (
  "Id_Costos" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_Operacion" uuid NOT NULL REFERENCES afrisan_actual."OperacionTerreno" ON DELETE RESTRICT,
  "NumeroLinea" integer NOT NULL CHECK ("NumeroLinea" > 0),
  "Concepto" varchar(20) NOT NULL CHECK ("Concepto" IN ('Refrigerante','Envase','ManoObra','Repuesto','Traslado','Otro')),
  "Descripcion" text NOT NULL CHECK (btrim("Descripcion") <> ''),
  "Cantidad" numeric(18,6) NOT NULL CHECK ("Cantidad" > 0),
  "Unidad" varchar(40) NOT NULL CHECK (btrim("Unidad") <> ''),
  "TarifaUnitaria" numeric(18,8) NOT NULL CHECK ("TarifaUnitaria" >= 0),
  "Id_Precio" bigint REFERENCES afrisan_actual."PreciosRefrigerantes" ON DELETE RESTRICT,
  "Moneda" varchar(3) NOT NULL CHECK ("Moneda" ~ '^[A-Z]{3}$'),
  "PrecioTotal" numeric(30,8) GENERATED ALWAYS AS ("Cantidad" * "TarifaUnitaria") STORED,
  "Id_Registrador" integer NOT NULL REFERENCES afrisan_actual."Usuario" ON DELETE RESTRICT,
  "FechaValoracion" timestamptz NOT NULL DEFAULT now(),
  UNIQUE ("Id_Operacion","NumeroLinea"),
  CHECK ("Id_Precio" IS NULL OR ("Concepto" = 'Refrigerante' AND "Unidad" = 'g'))
);
COMMENT ON TABLE afrisan_actual."Costos" IS
  'Valoración por líneas de una operación física. Cantidad y tarifa son explícitas; instalar/reemplazar un cilindro no significa consumir ni facturar todo su contenido. La API determina la base de cobro acordada y conserva esta tarifa aplicada.';

CREATE TABLE afrisan_actual."EstimacionEmisiones" (
  "Id_Estimacion" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Id_Operacion" uuid NOT NULL UNIQUE REFERENCES afrisan_actual."OperacionTerreno" ON DELETE RESTRICT,
  "Id_Refrigerante" bigint NOT NULL REFERENCES afrisan_actual."Refrigerantes" ON DELETE RESTRICT,
  "GrEmitidos" bigint NOT NULL CHECK ("GrEmitidos" >= 0),
  "FactorGWPAplicado" numeric(18,6) NOT NULL CHECK ("FactorGWPAplicado" >= 0),
  "FuenteGWPAplicada" text NOT NULL CHECK (btrim("FuenteGWPAplicada") <> ''),
  "MetodologiaEstimacion" text NOT NULL CHECK (btrim("MetodologiaEstimacion") <> ''),
  "EmisionesCO2eTon" numeric(38,8) GENERATED ALWAYS AS
    ("GrEmitidos"::numeric * "FactorGWPAplicado" / 1000000) STORED,
  "Id_Registrador" integer NOT NULL REFERENCES afrisan_actual."Usuario" ON DELETE RESTRICT,
  "FechaRegistro" timestamptz NOT NULL DEFAULT now()
);
COMMENT ON TABLE afrisan_actual."EstimacionEmisiones" IS
  'Estimación explícita con masa emitida, factor, fuente y metodología. No se genera a partir de kilos despachados/instalados. Ausencia de fila significa estimación desconocida, nunca cero automático.';

-- DISPOSICIÓN: lote entregado primero; certificado recibido después.
CREATE TABLE afrisan_actual."CertificadosDisposicion" (
  "Id_Certificado" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Estado" varchar(25) NOT NULL DEFAULT 'PendienteCertificado'
    CHECK ("Estado" IN ('PendienteCertificado','Certificado')),
  "EmpresaEmisora" varchar(150) NOT NULL CHECK (btrim("EmpresaEmisora") <> ''),
  "Folio" varchar(120),
  "FechaEntrega" date NOT NULL,
  "FechaEmision" date,
  "Documento" text,
  "Id_UsuarioRegistro" integer NOT NULL REFERENCES afrisan_actual."Usuario" ON DELETE RESTRICT,
  "FechaRegistro" timestamptz NOT NULL DEFAULT now(),
  "Id_UsuarioCertifica" integer REFERENCES afrisan_actual."Usuario" ON DELETE RESTRICT,
  "FechaRecepcionCertificado" timestamptz,
  UNIQUE ("EmpresaEmisora","Folio"),
  CHECK ("FechaEmision" IS NULL OR "FechaEmision" >= "FechaEntrega"),
  CHECK (("Estado" = 'Certificado') =
    (coalesce(btrim("Folio"),'') <> '' AND "FechaEmision" IS NOT NULL AND
     coalesce(btrim("Documento"),'') <> '' AND "Id_UsuarioCertifica" IS NOT NULL AND "FechaRecepcionCertificado" IS NOT NULL))
);
CREATE TABLE afrisan_actual."CertificadosCilindros" (
  "Id_Certificado" bigint NOT NULL REFERENCES afrisan_actual."CertificadosDisposicion" ON DELETE RESTRICT,
  "Id_Cilindro" bigint NOT NULL UNIQUE REFERENCES afrisan_actual."Cilindros" ON DELETE RESTRICT,
  PRIMARY KEY ("Id_Certificado","Id_Cilindro")
);
COMMENT ON TABLE afrisan_actual."CertificadosCilindros" IS
  'Cilindros en estado Chatarra de un lote pendiente de certificado. No exige intervenciones ni cargas de gas. El certificado y sus enlaces se conservan; no se asignan emisiones automáticamente.';

-- Estos módulos de soporte no amplían permisos operativos sin acuerdo.
-- Administrador registra tarifas/valoraciones/estimaciones/certificados por ahora.
-- La API podrá conceder las tareas a otros roles solo al definir esa política.
CREATE FUNCTION afrisan_actual.validar_soporte() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog, afrisan_actual AS $$
DECLARE v_actor integer; v_limite afrisan_actual."LimiteSucursalHistorial"%ROWTYPE;
  v_medicion afrisan_actual."MedicionLimite"%ROWTYPE; v_precio afrisan_actual."PreciosRefrigerantes"%ROWTYPE;
  v_gas bigint; v_sucursal integer; v_cert afrisan_actual."CertificadosDisposicion"%ROWTYPE; v_estado text;
BEGIN
  IF TG_OP <> 'INSERT' THEN RAISE EXCEPTION 'Este registro histórico no se modifica ni elimina'; END IF;
  IF TG_TABLE_NAME = 'MedicionLimite' THEN
    v_actor := afrisan_actual.actor(ARRAY['Bodega','Autorizador','Jefatura']);
    SELECT * INTO STRICT v_limite FROM afrisan_actual."LimiteSucursalHistorial"
      WHERE "Id_HistorialLimite" = NEW."Id_HistorialLimite";
    IF v_limite."EstadoDefinicion" <> 'Definido' THEN RAISE EXCEPTION 'No se evalúa un límite cuyo criterio sigue pendiente'; END IF;
    NEW."Id_Registrador" := v_actor; NEW."FechaRegistro" := clock_timestamp();
  ELSIF TG_TABLE_NAME = 'MedicionLimiteOperaciones' THEN
    PERFORM afrisan_actual.actor(ARRAY['Bodega','Autorizador','Jefatura']);
    SELECT * INTO STRICT v_medicion FROM afrisan_actual."MedicionLimite" WHERE "Id_Medicion" = NEW."Id_Medicion";
    SELECT "Id_Sucursal" INTO STRICT v_sucursal FROM afrisan_actual."LimiteSucursalHistorial"
      WHERE "Id_HistorialLimite" = v_medicion."Id_HistorialLimite";
    IF NOT EXISTS (SELECT 1 FROM afrisan_actual."OperacionTerreno" op
      JOIN afrisan_actual."OrdenTrabajo" ot ON ot."Id_OrdenTrabajo" = op."Id_OrdenTrabajo"
      WHERE op."Id_Operacion" = NEW."Id_Operacion" AND op."Estado" = 'Registrada' AND ot."Id_Sucursal" = v_sucursal) THEN
      RAISE EXCEPTION 'La operación debe estar registrada y pertenecer a la sucursal del cómputo';
    END IF;
  ELSIF TG_TABLE_NAME = 'RevisionExceso' THEN
    v_actor := afrisan_actual.actor(ARRAY['Jefatura']);
    SELECT * INTO STRICT v_medicion FROM afrisan_actual."MedicionLimite" WHERE "Id_Medicion" = NEW."Id_Medicion";
    SELECT * INTO STRICT v_limite FROM afrisan_actual."LimiteSucursalHistorial"
      WHERE "Id_HistorialLimite" = v_medicion."Id_HistorialLimite";
    IF v_medicion."GrComputados" <= v_limite."GrLimite" THEN RAISE EXCEPTION 'La medición no registra un exceso sobre el límite aplicado'; END IF;
    NEW."Id_Revisor" := v_actor; NEW."FechaRevision" := clock_timestamp();
  ELSIF TG_TABLE_NAME = 'PreciosRefrigerantes' THEN
    NEW."Id_Registrador" := afrisan_actual.actor(ARRAY['Administrador']); NEW."FechaRegistro" := clock_timestamp();
  ELSIF TG_TABLE_NAME IN ('Costos','EstimacionEmisiones') THEN
    v_actor := afrisan_actual.actor(ARRAY['Administrador']);
    SELECT c."Id_Refrigerante" INTO v_gas FROM afrisan_actual."OperacionTerreno" op
      JOIN afrisan_actual."Cilindros" c ON c."Id_Cilindro" = op."Id_Instalado"
      WHERE op."Id_Operacion" = NEW."Id_Operacion" AND op."Estado" = 'Registrada';
    IF v_gas IS NULL THEN RAISE EXCEPTION 'Solo se valora o estima una operación física registrada'; END IF;
    NEW."Id_Registrador" := v_actor;
    IF TG_TABLE_NAME = 'Costos' THEN
      NEW."FechaValoracion" := clock_timestamp();
      IF NEW."Id_Precio" IS NOT NULL THEN
        SELECT * INTO STRICT v_precio FROM afrisan_actual."PreciosRefrigerantes" WHERE "Id_Precio" = NEW."Id_Precio";
        IF v_precio."Id_Refrigerante" <> v_gas OR NEW."TarifaUnitaria" <> v_precio."PrecioGrRefrigerante"
          OR NEW."Moneda" <> v_precio."Moneda" THEN RAISE EXCEPTION 'La tarifa aplicada no coincide con el refrigerante, valor o moneda del precio referenciado'; END IF;
      END IF;
    ELSE
      IF NEW."Id_Refrigerante" <> v_gas THEN RAISE EXCEPTION 'El refrigerante estimado no coincide con el de la operación'; END IF;
      NEW."FechaRegistro" := clock_timestamp();
    END IF;
  ELSIF TG_TABLE_NAME = 'CertificadosCilindros' THEN
    PERFORM afrisan_actual.actor(ARRAY['Administrador']);
    SELECT * INTO STRICT v_cert FROM afrisan_actual."CertificadosDisposicion" WHERE "Id_Certificado" = NEW."Id_Certificado" FOR UPDATE;
    IF v_cert."Estado" <> 'PendienteCertificado' THEN RAISE EXCEPTION 'El lote certificado ya está cerrado'; END IF;
    SELECT "Estado" INTO STRICT v_estado FROM afrisan_actual."Cilindros" WHERE "Id_Cilindro" = NEW."Id_Cilindro" FOR UPDATE;
    IF v_estado <> 'Chatarra' THEN RAISE EXCEPTION 'Solo se incluye un cilindro clasificado como Chatarra'; END IF;
  ELSE RAISE EXCEPTION 'Tabla de soporte no reconocida';
  END IF;
  RETURN NEW;
END $$;

CREATE FUNCTION afrisan_actual.validar_certificado() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog, afrisan_actual AS $$
DECLARE v_actor integer;
BEGIN
  IF TG_OP = 'DELETE' THEN RAISE EXCEPTION 'No se elimina el historial de disposición'; END IF;
  v_actor := afrisan_actual.actor(ARRAY['Administrador']);
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
    IF NOT EXISTS (SELECT 1 FROM afrisan_actual."CertificadosCilindros" WHERE "Id_Certificado" = OLD."Id_Certificado") THEN
      RAISE EXCEPTION 'El certificado debe identificar al menos un cilindro entregado';
    END IF;
    PERFORM c."Id_Cilindro" FROM afrisan_actual."Cilindros" c
      JOIN afrisan_actual."CertificadosCilindros" l ON l."Id_Cilindro" = c."Id_Cilindro"
      WHERE l."Id_Certificado" = OLD."Id_Certificado" FOR UPDATE OF c;
    IF EXISTS (SELECT 1 FROM afrisan_actual."Cilindros" c
      JOIN afrisan_actual."CertificadosCilindros" l ON l."Id_Cilindro" = c."Id_Cilindro"
      WHERE l."Id_Certificado" = OLD."Id_Certificado" AND c."Estado" <> 'Chatarra') THEN
      RAISE EXCEPTION 'El lote ya no corresponde a cilindros clasificados como Chatarra';
    END IF;
    NEW."Id_UsuarioCertifica" := v_actor; NEW."FechaRecepcionCertificado" := clock_timestamp();
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER "TR_Certificado_Validar" BEFORE INSERT OR UPDATE OR DELETE
ON afrisan_actual."CertificadosDisposicion" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.validar_certificado();

DO $$ DECLARE t text; BEGIN
  FOREACH t IN ARRAY ARRAY['MedicionLimite','MedicionLimiteOperaciones','RevisionExceso',
    'PreciosRefrigerantes','Costos','EstimacionEmisiones','CertificadosCilindros'] LOOP
    EXECUTE format('CREATE TRIGGER %I BEFORE INSERT OR UPDATE OR DELETE ON afrisan_actual.%I FOR EACH ROW EXECUTE FUNCTION afrisan_actual.validar_soporte()', 'TR_' || t || '_Validar', t);
  END LOOP;
  FOREACH t IN ARRAY ARRAY['LimiteSucursal','LimiteSucursalHistorial','MedicionLimite','MedicionLimiteOperaciones',
    'RevisionExceso','PreciosRefrigerantes','Costos','EstimacionEmisiones','CertificadosDisposicion','CertificadosCilindros'] LOOP
    EXECUTE format('ALTER TABLE afrisan_actual.%I ENABLE ROW LEVEL SECURITY', t);
    EXECUTE format('REVOKE ALL ON afrisan_actual.%I FROM PUBLIC', t);
  END LOOP;
END $$;

-- Vistas para un lector BI de servidor confiable, con acceso expresamente otorgado
-- por el administrador. No crear GRANT a anon/authenticated ni publicar Data API.
CREATE VIEW afrisan_bi_actual."LimitesVigentes" AS
SELECT DISTINCT ON ("Id_Limite") "Id_Limite","Id_Sucursal","Periodicidad","EstadoDefinicion","GrLimite","CriterioComputo",
  "VigenteDesde","BloqueadoBodega","Version","FechaRegistro" AS "FechaActualizacion"
FROM afrisan_actual."LimiteSucursalHistorial"
WHERE "VigenteDesde" <= current_timestamp
ORDER BY "Id_Limite","VigenteDesde" DESC,"Version" DESC;
CREATE VIEW afrisan_bi_actual."ExcesosRegistrados" AS
SELECT m."Id_Medicion",h."Id_Sucursal",h."Periodicidad",m."PeriodoInicio",m."PeriodoFin",
  h."Version" AS "VersionLimiteAplicada",h."GrLimite",m."GrComputados",
  greatest(m."GrComputados" - h."GrLimite",0) AS "GrExcesoDeclarado",
  h."CriterioComputo",m."MetodologiaComputo",m."FechaRegistro",
  r."Conclusion",r."FechaRevision"
FROM afrisan_actual."MedicionLimite" m
JOIN afrisan_actual."LimiteSucursalHistorial" h ON h."Id_HistorialLimite" = m."Id_HistorialLimite"
LEFT JOIN afrisan_actual."RevisionExceso" r ON r."Id_Medicion" = m."Id_Medicion";
CREATE VIEW afrisan_bi_actual."CostosOperaciones" AS
SELECT c."Id_Costos",c."Id_Operacion",op."Id_OrdenTrabajo",ot."Id_Sucursal",c."NumeroLinea",c."Concepto",
  c."Descripcion",c."Cantidad",c."Unidad",c."TarifaUnitaria",c."Moneda",c."PrecioTotal",c."FechaValoracion"
FROM afrisan_actual."Costos" c JOIN afrisan_actual."OperacionTerreno" op ON op."Id_Operacion" = c."Id_Operacion"
JOIN afrisan_actual."OrdenTrabajo" ot ON ot."Id_OrdenTrabajo" = op."Id_OrdenTrabajo";
CREATE VIEW afrisan_bi_actual."EmisionesEstimadas" AS
SELECT op."Id_Operacion",op."Id_OrdenTrabajo",ot."Id_Sucursal",e."Id_Refrigerante",e."GrEmitidos",
  e."FactorGWPAplicado",e."FuenteGWPAplicada",e."MetodologiaEstimacion",e."EmisionesCO2eTon",
  e."FechaRegistro",(e."Id_Estimacion" IS NOT NULL) AS "TieneEstimacion"
FROM afrisan_actual."OperacionTerreno" op JOIN afrisan_actual."OrdenTrabajo" ot ON ot."Id_OrdenTrabajo" = op."Id_OrdenTrabajo"
LEFT JOIN afrisan_actual."EstimacionEmisiones" e ON e."Id_Operacion" = op."Id_Operacion"
WHERE op."Estado" = 'Registrada';
CREATE VIEW afrisan_bi_actual."DisposicionesCertificadas" AS
SELECT d."Id_Certificado",d."Estado",d."EmpresaEmisora",d."Folio",d."FechaEntrega",d."FechaEmision",c."Id_Cilindro"
FROM afrisan_actual."CertificadosDisposicion" d
JOIN afrisan_actual."CertificadosCilindros" c ON c."Id_Certificado" = d."Id_Certificado";

REVOKE ALL ON ALL TABLES IN SCHEMA afrisan_bi_actual FROM PUBLIC;
REVOKE EXECUTE ON FUNCTION afrisan_actual.validar_limite_sucursal(),
  afrisan_actual.historiar_limite_sucursal(), afrisan_actual.proteger_historial_limite(),
  afrisan_actual.validar_soporte(), afrisan_actual.validar_certificado() FROM PUBLIC;

-- COMPONENTE: 03_auditoria_y_acceso.sql
CREATE TABLE afrisan_actual."Auditoria" (
  "Id_Auditoria" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  "Fecha" timestamptz NOT NULL DEFAULT clock_timestamp(), "Id_Usuario" integer REFERENCES afrisan_actual."Usuario",
  "UsuarioBD" text NOT NULL DEFAULT session_user, "Tabla" text NOT NULL,
  "Accion" varchar(10) NOT NULL CHECK ("Accion" IN ('INSERT','UPDATE','DELETE')),
  "ValoresAnteriores" jsonb,"ValoresNuevos" jsonb
);
CREATE FUNCTION afrisan_actual.auditar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
DECLARE anterior jsonb; nuevo jsonb;
BEGIN
  IF TG_OP<>'INSERT' THEN anterior:=to_jsonb(OLD); END IF;
  IF TG_OP<>'DELETE' THEN nuevo:=to_jsonb(NEW); END IF;
  IF TG_TABLE_NAME='Usuario' THEN
    anterior:=anterior-ARRAY['Clave_Usuario','VersionSeguridad']; nuevo:=nuevo-ARRAY['Clave_Usuario','VersionSeguridad'];
  END IF;
  INSERT INTO afrisan_actual."Auditoria"("Id_Usuario","Tabla","Accion","ValoresAnteriores","ValoresNuevos")
  VALUES(nullif(current_setting('afrisan_actual.id_usuario',true),'')::integer,TG_TABLE_NAME,TG_OP,anterior,nuevo);
  RETURN NULL;
END $$;
CREATE TRIGGER "Auditoria_Inmutable" BEFORE UPDATE OR DELETE ON afrisan_actual."Auditoria" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.inmutable();

-- Baja administrativa con motivo. No supone que exista certificado todavía.
-- Es una herramienta de administración; el responsable de negocio debe ratificarse.
CREATE TABLE afrisan_actual."BajaCilindro" (
  "Id_Baja" uuid PRIMARY KEY DEFAULT gen_random_uuid(),"Id_Cilindro" bigint NOT NULL UNIQUE REFERENCES afrisan_actual."Cilindros",
  "Motivo" text NOT NULL CHECK(length(btrim("Motivo"))>=5),
  "EstadoDestino" varchar(20) NOT NULL CHECK("EstadoDestino" IN ('Chatarra','Inactivo')),
  "Id_Usuario" integer NOT NULL REFERENCES afrisan_actual."Usuario","Fecha" timestamptz NOT NULL DEFAULT clock_timestamp()
);
CREATE FUNCTION afrisan_actual.baja_validar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
DECLARE c afrisan_actual."Cilindros";
BEGIN
  NEW."Id_Usuario":=afrisan_actual.actor(ARRAY['Administrador']);
  SELECT * INTO STRICT c FROM afrisan_actual."Cilindros" WHERE "Id_Cilindro"=NEW."Id_Cilindro" FOR UPDATE;
  IF c."Ubicacion"<>'Bodega' OR EXISTS(SELECT 1 FROM afrisan_actual."CustodiaActual" WHERE "Id_Cilindro"=c."Id_Cilindro") OR EXISTS(SELECT 1 FROM afrisan_actual."RetiroPrevisto" WHERE "Id_Cilindro"=c."Id_Cilindro" AND "Estado"='Reservado') THEN RAISE EXCEPTION 'No retirar de servicio un envase instalado, reservado o en custodia'; END IF;
  NEW."Fecha":=clock_timestamp(); RETURN NEW;
END $$;
CREATE TRIGGER "Baja_Validar" BEFORE INSERT ON afrisan_actual."BajaCilindro" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.baja_validar();
CREATE TRIGGER "Baja_Inmutable" BEFORE UPDATE OR DELETE ON afrisan_actual."BajaCilindro" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.inmutable();
CREATE FUNCTION afrisan_actual.baja_aplicar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
BEGIN
  UPDATE afrisan_actual."Cilindros" SET "Ubicacion"='FueraServicio',"Estado"=NEW."EstadoDestino","Id_Sucursal"=NULL,"Id_Tecnico"=NULL WHERE "Id_Cilindro"=NEW."Id_Cilindro";
  RETURN NULL;
END $$;
CREATE TRIGGER "Baja_Aplicar" AFTER INSERT ON afrisan_actual."BajaCilindro" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.baja_aplicar();
CREATE FUNCTION afrisan_actual.certificado_aplicar() RETURNS trigger LANGUAGE plpgsql
SET search_path=pg_catalog,afrisan_actual AS $$
BEGIN
  IF NEW."Estado"='Certificado' AND OLD."Estado"='PendienteCertificado' THEN
    UPDATE afrisan_actual."Cilindros" c SET "Estado"='DisposicionFinalCertificada'
    FROM afrisan_actual."CertificadosCilindros" l WHERE l."Id_Certificado"=NEW."Id_Certificado" AND l."Id_Cilindro"=c."Id_Cilindro";
  END IF; RETURN NULL;
END $$;
CREATE TRIGGER "Certificado_Aplicar" AFTER UPDATE ON afrisan_actual."CertificadosDisposicion" FOR EACH ROW EXECUTE FUNCTION afrisan_actual.certificado_aplicar();

CREATE VIEW afrisan_bi_actual."PedidoYEjecucion" AS
SELECT d."Id_OrdenTrabajo",o."Id_Sucursal",d."Id_Refrigerante",d."GrSolicitados",d."GrAutorizados",
 (SELECT sum(p."PesoBrutoGr"-c."TaraGr") FROM afrisan_actual."Despacho" e JOIN afrisan_actual."Cilindros" c USING("Id_Cilindro")
  JOIN afrisan_actual."Pesaje" p ON p."Id_Pesaje"=e."Id_Pesaje" WHERE e."Id_OrdenTrabajo"=d."Id_OrdenTrabajo" AND c."Id_Refrigerante"=d."Id_Refrigerante") AS "GrContenidoDespachado",
 (SELECT sum(p."PesoBrutoGr"-c."TaraGr") FROM afrisan_actual."OperacionTerreno" op JOIN afrisan_actual."Cilindros" c ON c."Id_Cilindro"=op."Id_Instalado"
  JOIN afrisan_actual."Pesaje" p ON p."Id_Pesaje"=op."Id_PesajeInstalado" WHERE op."Id_OrdenTrabajo"=d."Id_OrdenTrabajo" AND op."Estado"='Registrada' AND c."Id_Refrigerante"=d."Id_Refrigerante") AS "GrContenidoInstalado"
FROM afrisan_actual."DetalleOT" d JOIN afrisan_actual."OrdenTrabajo" o USING("Id_OrdenTrabajo");
COMMENT ON VIEW afrisan_bi_actual."PedidoYEjecucion" IS 'Masas de etapas diferentes. No sumar despacho e instalación ni interpretar esas masas como consumo/emisiones. Sin registros permanece NULL.';

-- Bloqueos, auditoría e índices de consulta. No se instala ninguna cuenta.
DO $$ DECLARE t record; BEGIN
  FOR t IN SELECT tablename FROM pg_tables WHERE schemaname='afrisan_actual' AND tablename<>'Auditoria' LOOP
    EXECUTE format('CREATE TRIGGER %I AFTER INSERT OR UPDATE OR DELETE ON afrisan_actual.%I FOR EACH ROW EXECUTE FUNCTION afrisan_actual.auditar()','Auditar_'||t.tablename,t.tablename);
  END LOOP;
  FOR t IN SELECT tablename FROM pg_tables WHERE schemaname='afrisan_actual' LOOP
    EXECUTE format('ALTER TABLE afrisan_actual.%I ENABLE ROW LEVEL SECURITY',t.tablename);
  END LOOP;
END $$;
CREATE INDEX "IX_Pesaje_Cilindro_Fecha" ON afrisan_actual."Pesaje"("Id_Cilindro","Fecha");
CREATE INDEX "IX_Despacho_OT" ON afrisan_actual."Despacho"("Id_OrdenTrabajo");
CREATE INDEX "IX_Operacion_OT" ON afrisan_actual."OperacionTerreno"("Id_OrdenTrabajo");
CREATE INDEX "IX_Recepcion_OT" ON afrisan_actual."RecepcionBodega"("Id_OrdenTrabajo");
CREATE INDEX "IX_Incidencia_OT" ON afrisan_actual."Incidencia"("Id_OrdenTrabajo");
CREATE INDEX "IX_Evento_OT" ON afrisan_actual."EventoOT"("Id_OrdenTrabajo","Fecha");
CREATE INDEX "IX_Auditoria_Tabla_Fecha" ON afrisan_actual."Auditoria"("Tabla","Fecha");

REVOKE ALL ON ALL TABLES IN SCHEMA afrisan_actual,afrisan_bi_actual FROM PUBLIC;
REVOKE ALL ON ALL SEQUENCES IN SCHEMA afrisan_actual FROM PUBLIC;
REVOKE EXECUTE ON ALL FUNCTIONS IN SCHEMA afrisan_actual FROM PUBLIC;
ALTER DEFAULT PRIVILEGES IN SCHEMA afrisan_actual REVOKE EXECUTE ON FUNCTIONS FROM PUBLIC;
ALTER DEFAULT PRIVILEGES IN SCHEMA afrisan_actual REVOKE ALL ON TABLES FROM PUBLIC;
DO $$ DECLARE r text; BEGIN
  FOREACH r IN ARRAY ARRAY['anon','authenticated'] LOOP
    IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname=r) THEN
      EXECUTE format('REVOKE ALL ON SCHEMA afrisan_actual,afrisan_bi_actual FROM %I',r);
      EXECUTE format('REVOKE ALL ON ALL TABLES IN SCHEMA afrisan_actual,afrisan_bi_actual FROM %I',r);
      EXECUTE format('REVOKE ALL ON ALL SEQUENCES IN SCHEMA afrisan_actual FROM %I',r);
      EXECUTE format('REVOKE EXECUTE ON ALL FUNCTIONS IN SCHEMA afrisan_actual FROM %I',r);
    END IF;
  END LOOP;
END $$;
COMMENT ON SCHEMA afrisan_actual IS 'Instalación vacía del contexto actual. Acceso solo desde backend de confianza. Sin políticas de navegador ni credenciales precargadas.';
COMMIT;
