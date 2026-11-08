-- AFRISAN | Actualización aditiva para la API persistente | 02/11/2026
-- Aplicar sobre el esquema instalado. No elimina tablas ni datos. No repetir 06.
BEGIN;
ALTER TABLE afrisan."OrdenTrabajo" ADD COLUMN IF NOT EXISTS "HuellaCreacion" text;
ALTER TABLE afrisan."OperacionTerreno"
  ADD COLUMN IF NOT EXISTS "DatosBorrador" jsonb NOT NULL DEFAULT '{}',
  ADD COLUMN IF NOT EXISTS "FotosBorrador" jsonb NOT NULL DEFAULT '[]';
-- Permite almacenar/editar fotos pendientes sin modificar Pesaje/Evidencia confirmados.
-- El usuario de conexión es exclusivo del backend, nunca del navegador.
DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='afrisan_api') THEN
    CREATE ROLE afrisan_api NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
  END IF;
END $$;
GRANT USAGE ON SCHEMA afrisan TO afrisan_api;
GRANT SELECT, INSERT ON afrisan."Rol",afrisan."Cliente",afrisan."Sucursales",
  afrisan."Refrigerantes",afrisan."DetalleOT",afrisan."Pesaje",afrisan."EvidenciaPesaje",
  afrisan."RecepcionBodega",afrisan."RevisionOT",afrisan."HistorialCilindro",
  afrisan."EventoOT",afrisan."Auditoria" TO afrisan_api;
GRANT SELECT, INSERT, UPDATE ON afrisan."Usuario",afrisan."Cilindros",afrisan."OrdenTrabajo",
  afrisan."RetiroPrevisto",afrisan."Despacho",afrisan."OperacionTerreno",afrisan."Incidencia" TO afrisan_api;
GRANT SELECT, INSERT, DELETE ON afrisan."CustodiaActual" TO afrisan_api;
-- SELECT FOR UPDATE de evidencia_validar requiere UPDATE; el trigger inmutable
-- sigue rechazando toda modificación o eliminación de pesajes confirmados.
GRANT UPDATE ON afrisan."Pesaje" TO afrisan_api;
DO $$ DECLARE s record; BEGIN
  FOR s IN
    SELECT seq.relname FROM pg_class seq
    JOIN pg_depend d ON d.objid=seq.oid AND d.classid='pg_class'::regclass
    JOIN pg_class t ON t.oid=d.refobjid
    JOIN pg_namespace n ON n.oid=t.relnamespace
    WHERE seq.relkind='S' AND n.nspname='afrisan' AND d.deptype IN ('a','i')
      AND t.relname IN ('Rol','Usuario','Cliente','Sucursales','Refrigerantes','Cilindros','OrdenTrabajo','HistorialCilindro','EventoOT','Auditoria')
  LOOP EXECUTE format('GRANT USAGE,SELECT ON SEQUENCE afrisan.%I TO afrisan_api',s.relname); END LOOP;
END $$;
GRANT EXECUTE ON FUNCTION afrisan.actor(text[]),afrisan.ot_actor(bigint,text[],boolean),
  afrisan.peso_valido(bigint,bigint),afrisan.sin_pendientes(bigint),
  afrisan.retirar_cilindro(uuid,bigint,bigint,bigint,jsonb) TO afrisan_api;
DO $$ DECLARE t text; BEGIN
  FOREACH t IN ARRAY ARRAY['Rol','Usuario','Cliente','Sucursales','Refrigerantes','Cilindros',
    'OrdenTrabajo','DetalleOT','RetiroPrevisto','Pesaje','EvidenciaPesaje','Despacho',
    'OperacionTerreno','CustodiaActual','RecepcionBodega','RevisionOT','Incidencia',
    'HistorialCilindro','EventoOT','Auditoria'] LOOP
    IF NOT EXISTS(SELECT 1 FROM pg_policies WHERE schemaname='afrisan' AND tablename=t AND policyname='BackendConfiable') THEN
      EXECUTE format('CREATE POLICY "BackendConfiable" ON afrisan.%I TO afrisan_api USING (true) WITH CHECK (true)',t);
    END IF;
  END LOOP;
END $$;
-- Estas políticas autorizan al servidor de confianza. Roles de negocio y alcance
-- por OT se verifican en la API y en los triggers con afrisan.id_usuario.
-- anon/authenticated siguen sin acceso. Los hechos confirmados siguen inmutables.
COMMIT;
