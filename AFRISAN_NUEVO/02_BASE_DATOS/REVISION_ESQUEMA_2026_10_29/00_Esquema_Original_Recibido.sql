-- WARNING: This schema is for context only and is not meant to be run.
-- Table order and constraints may not be valid for execution.

CREATE TABLE afrisan.Rol (
  Id_Rol smallint GENERATED ALWAYS AS IDENTITY NOT NULL,
  Nombre_Rol character varying NOT NULL UNIQUE CHECK ("Nombre_Rol"::text = ANY (ARRAY['Administrador'::character varying, 'Jefatura'::character varying, 'Bodega'::character varying, 'Tecnico'::character varying]::text[])),
  CONSTRAINT Rol_pkey PRIMARY KEY (Id_Rol)
);
CREATE TABLE afrisan.Usuario (
  Id_Usuario integer GENERATED ALWAYS AS IDENTITY NOT NULL,
  Nombre character varying NOT NULL CHECK (btrim("Nombre"::text) <> ''::text),
  Apellido character varying NOT NULL CHECK (btrim("Apellido"::text) <> ''::text),
  Correo_Usuario character varying NOT NULL CHECK (btrim("Correo_Usuario"::text) <> ''::text),
  Clave_Usuario text NOT NULL CHECK (length("Clave_Usuario") >= 32),
  Activo boolean NOT NULL DEFAULT true,
  Id_Rol smallint NOT NULL,
  FechaCreacion timestamp with time zone NOT NULL DEFAULT now(),
  IntentosFallidos integer NOT NULL DEFAULT 0 CHECK ("IntentosFallidos" >= 0),
  BloqueadoHasta timestamp with time zone,
  VersionSeguridad uuid NOT NULL DEFAULT gen_random_uuid(),
  CONSTRAINT Usuario_pkey PRIMARY KEY (Id_Usuario),
  CONSTRAINT Usuario_Id_Rol_fkey FOREIGN KEY (Id_Rol) REFERENCES afrisan.Rol(Id_Rol)
);
CREATE TABLE afrisan.Cliente (
  Id_Cliente integer GENERATED ALWAYS AS IDENTITY NOT NULL,
  RutCliente character varying NOT NULL UNIQUE CHECK (btrim("RutCliente"::text) <> ''::text),
  NombreEmpresa character varying NOT NULL CHECK (btrim("NombreEmpresa"::text) <> ''::text),
  Activo boolean NOT NULL DEFAULT true,
  CONSTRAINT Cliente_pkey PRIMARY KEY (Id_Cliente)
);
CREATE TABLE afrisan.Sucursales (
  Id_Sucursal integer GENERATED ALWAYS AS IDENTITY NOT NULL,
  Id_Cliente integer NOT NULL,
  NombreSucursal character varying NOT NULL CHECK (btrim("NombreSucursal"::text) <> ''::text),
  Direccion character varying,
  Activo boolean NOT NULL DEFAULT true,
  CONSTRAINT Sucursales_pkey PRIMARY KEY (Id_Sucursal),
  CONSTRAINT Sucursales_Id_Cliente_fkey FOREIGN KEY (Id_Cliente) REFERENCES afrisan.Cliente(Id_Cliente)
);
CREATE TABLE afrisan.Refrigerantes (
  Id_Refrigerante bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  CodigoRefrigerante character varying NOT NULL CHECK (btrim("CodigoRefrigerante"::text) <> ''::text),
  NombreRefrigerante character varying,
  FactorGWP numeric CHECK ("FactorGWP" >= 0::numeric),
  FuenteGWP text,
  MetodologiaGWP character varying,
  Activo boolean NOT NULL DEFAULT true,
  CONSTRAINT Refrigerantes_pkey PRIMARY KEY (Id_Refrigerante)
);
CREATE TABLE afrisan.Cilindros (
  Id_Cilindro bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  Capacidadgr bigint NOT NULL CHECK ("Capacidadgr" > 0),
  TaraGr bigint NOT NULL CHECK ("TaraGr" > 0),
  Id_Refrigerante bigint NOT NULL,
  CilindroQR character varying NOT NULL UNIQUE CHECK (btrim("CilindroQR"::text) <> ''::text),
  NumeroSerie character varying UNIQUE,
  Estado character varying NOT NULL DEFAULT 'Disponible'::character varying CHECK ("Estado"::text = ANY (ARRAY['Disponible'::character varying, 'Despachado'::character varying, 'RetornadoConDescalce'::character varying, 'Chatarra'::character varying, 'DisposicionFinalCertificada'::character varying, 'Inactivo'::character varying]::text[])),
  FechaRegistro timestamp with time zone NOT NULL DEFAULT now(),
  CONSTRAINT Cilindros_pkey PRIMARY KEY (Id_Cilindro),
  CONSTRAINT Cilindros_Id_Refrigerante_fkey FOREIGN KEY (Id_Refrigerante) REFERENCES afrisan.Refrigerantes(Id_Refrigerante)
);
CREATE TABLE afrisan.Equipos (
  Id_Equipo bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  Id_Sucursal integer NOT NULL,
  CodigoEquipo character varying NOT NULL CHECK (btrim("CodigoEquipo"::text) <> ''::text),
  NombreEquipo character varying NOT NULL,
  CapacidadNominalGr bigint NOT NULL CHECK ("CapacidadNominalGr" > 0),
  Id_Refrigerante bigint NOT NULL,
  Activo boolean NOT NULL DEFAULT true,
  CONSTRAINT Equipos_pkey PRIMARY KEY (Id_Equipo),
  CONSTRAINT Equipos_Id_Sucursal_fkey FOREIGN KEY (Id_Sucursal) REFERENCES afrisan.Sucursales(Id_Sucursal),
  CONSTRAINT Equipos_Id_Refrigerante_fkey FOREIGN KEY (Id_Refrigerante) REFERENCES afrisan.Refrigerantes(Id_Refrigerante)
);
CREATE TABLE afrisan.CausasFalla (
  Id_CausaFalla integer GENERATED ALWAYS AS IDENTITY NOT NULL,
  Codigo character varying NOT NULL UNIQUE,
  Descripcion character varying NOT NULL,
  Activo boolean NOT NULL DEFAULT true,
  CONSTRAINT CausasFalla_pkey PRIMARY KEY (Id_CausaFalla)
);
CREATE TABLE afrisan.OrdenTrabajo (
  Id_OrdenTrabajo bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  CodigoOT character varying NOT NULL UNIQUE CHECK (btrim("CodigoOT"::text) <> ''::text),
  Origen character varying NOT NULL DEFAULT 'Local'::character varying CHECK ("Origen"::text = ANY (ARRAY['Local'::character varying, 'Defontana'::character varying]::text[])),
  CodigoExterno character varying,
  Id_Autorizador integer,
  Id_Sucursal integer NOT NULL,
  Descripcion text,
  Estado character varying NOT NULL DEFAULT 'Abierta'::character varying CHECK ("Estado"::text = ANY (ARRAY['Abierta'::character varying, 'EnCurso'::character varying, 'Cerrada'::character varying, 'Anulada'::character varying]::text[])),
  FechaCreacion timestamp with time zone NOT NULL DEFAULT now(),
  FechaSalida timestamp with time zone,
  FechaFinalizada timestamp with time zone,
  CONSTRAINT OrdenTrabajo_pkey PRIMARY KEY (Id_OrdenTrabajo),
  CONSTRAINT OrdenTrabajo_Id_Autorizador_fkey FOREIGN KEY (Id_Autorizador) REFERENCES afrisan.Usuario(Id_Usuario),
  CONSTRAINT OrdenTrabajo_Id_Sucursal_fkey FOREIGN KEY (Id_Sucursal) REFERENCES afrisan.Sucursales(Id_Sucursal)
);
CREATE TABLE afrisan.Movimientos (
  Id_Movimiento bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  Id_Operacion uuid NOT NULL DEFAULT gen_random_uuid() UNIQUE,
  Id_OrdenTrabajo bigint NOT NULL,
  Id_Cilindro bigint NOT NULL,
  Id_Usuario integer NOT NULL,
  Id_Tecnico integer NOT NULL,
  Salida boolean NOT NULL,
  PesoGrBruto bigint NOT NULL CHECK ("PesoGrBruto" >= 0),
  FechaMovimiento timestamp with time zone NOT NULL DEFAULT now(),
  FotoPesaje text,
  Id_Despacho bigint,
  CustodiaAbierta boolean NOT NULL DEFAULT false,
  Observaciones text,
  CONSTRAINT Movimientos_pkey PRIMARY KEY (Id_Movimiento),
  CONSTRAINT Movimientos_Id_OrdenTrabajo_fkey FOREIGN KEY (Id_OrdenTrabajo) REFERENCES afrisan.OrdenTrabajo(Id_OrdenTrabajo),
  CONSTRAINT Movimientos_Id_Cilindro_fkey FOREIGN KEY (Id_Cilindro) REFERENCES afrisan.Cilindros(Id_Cilindro),
  CONSTRAINT Movimientos_Id_Usuario_fkey FOREIGN KEY (Id_Usuario) REFERENCES afrisan.Usuario(Id_Usuario),
  CONSTRAINT Movimientos_Id_Tecnico_fkey FOREIGN KEY (Id_Tecnico) REFERENCES afrisan.Usuario(Id_Usuario),
  CONSTRAINT Movimientos_Id_Despacho_Id_Cilindro_Id_OrdenTrabajo_Id_Tec_fkey FOREIGN KEY (Id_Despacho) REFERENCES afrisan.Movimientos(Id_Movimiento),
  CONSTRAINT Movimientos_Id_Despacho_Id_Cilindro_Id_OrdenTrabajo_Id_Tec_fkey FOREIGN KEY (Id_OrdenTrabajo) REFERENCES afrisan.Movimientos(Id_OrdenTrabajo),
  CONSTRAINT Movimientos_Id_Despacho_Id_Cilindro_Id_OrdenTrabajo_Id_Tec_fkey FOREIGN KEY (Id_Cilindro) REFERENCES afrisan.Movimientos(Id_Cilindro),
  CONSTRAINT Movimientos_Id_Despacho_Id_Cilindro_Id_OrdenTrabajo_Id_Tec_fkey FOREIGN KEY (Id_Tecnico) REFERENCES afrisan.Movimientos(Id_Tecnico)
);
CREATE TABLE afrisan.Intervencion (
  Id_Intervencion bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  Id_Operacion uuid NOT NULL DEFAULT gen_random_uuid() UNIQUE,
  Id_OrdenTrabajo bigint NOT NULL,
  Id_Sucursal integer NOT NULL,
  Id_Equipo bigint NOT NULL,
  Id_Cilindro bigint NOT NULL,
  Id_Refrigerante bigint NOT NULL,
  Id_Despacho bigint NOT NULL,
  Id_Tecnico integer NOT NULL,
  Id_CausaFalla integer NOT NULL,
  FechaIntervencion timestamp with time zone NOT NULL,
  GrUtilizados bigint NOT NULL CHECK ("GrUtilizados" > 0),
  PesoRestanteGrBruto bigint CHECK ("PesoRestanteGrBruto" >= 0),
  FotoBalanza text,
  CapacidadNominalGrAplicada bigint NOT NULL CHECK ("CapacidadNominalGrAplicada" > 0),
  EstadoSemaforo character varying NOT NULL DEFAULT 'Pendiente'::character varying CHECK ("EstadoSemaforo"::text = ANY (ARRAY['Pendiente'::character varying, 'Verde'::character varying, 'Amarillo'::character varying, 'Rojo'::character varying]::text[])),
  PlanCorreccion text,
  Estado character varying NOT NULL DEFAULT 'Registrada'::character varying CHECK ("Estado"::text = ANY (ARRAY['Registrada'::character varying, 'Revisada'::character varying, 'Cerrada'::character varying]::text[])),
  Id_Revisor integer,
  FechaRevision timestamp with time zone,
  FechaCierre timestamp with time zone,
  GrEmitidos bigint CHECK ("GrEmitidos" >= 0),
  FactorGWPAplicado numeric CHECK ("FactorGWPAplicado" >= 0::numeric),
  FuenteGWPAplicada text,
  MetodologiaGWPAplicada character varying,
  EmisionesCO2eTon numeric DEFAULT ((("GrEmitidos")::numeric * "FactorGWPAplicado") / (1000000)::numeric),
  Dispositivo character varying,
  FechaCreacionLocal timestamp with time zone,
  FechaRecepcion timestamp with time zone NOT NULL DEFAULT now(),
  Observaciones text,
  CONSTRAINT Intervencion_pkey PRIMARY KEY (Id_Intervencion),
  CONSTRAINT Intervencion_Id_Tecnico_fkey FOREIGN KEY (Id_Tecnico) REFERENCES afrisan.Usuario(Id_Usuario),
  CONSTRAINT Intervencion_Id_CausaFalla_fkey FOREIGN KEY (Id_CausaFalla) REFERENCES afrisan.CausasFalla(Id_CausaFalla),
  CONSTRAINT Intervencion_Id_Revisor_fkey FOREIGN KEY (Id_Revisor) REFERENCES afrisan.Usuario(Id_Usuario),
  CONSTRAINT Intervencion_Id_OrdenTrabajo_Id_Sucursal_fkey FOREIGN KEY (Id_OrdenTrabajo) REFERENCES afrisan.OrdenTrabajo(Id_OrdenTrabajo),
  CONSTRAINT Intervencion_Id_OrdenTrabajo_Id_Sucursal_fkey FOREIGN KEY (Id_Sucursal) REFERENCES afrisan.OrdenTrabajo(Id_Sucursal),
  CONSTRAINT Intervencion_Id_Equipo_Id_Sucursal_Id_Refrigerante_fkey FOREIGN KEY (Id_Equipo) REFERENCES afrisan.Equipos(Id_Equipo),
  CONSTRAINT Intervencion_Id_Equipo_Id_Sucursal_Id_Refrigerante_fkey FOREIGN KEY (Id_Sucursal) REFERENCES afrisan.Equipos(Id_Sucursal),
  CONSTRAINT Intervencion_Id_Equipo_Id_Sucursal_Id_Refrigerante_fkey FOREIGN KEY (Id_Refrigerante) REFERENCES afrisan.Equipos(Id_Refrigerante),
  CONSTRAINT Intervencion_Id_Cilindro_Id_Refrigerante_fkey FOREIGN KEY (Id_Cilindro) REFERENCES afrisan.Cilindros(Id_Cilindro),
  CONSTRAINT Intervencion_Id_Cilindro_Id_Refrigerante_fkey FOREIGN KEY (Id_Refrigerante) REFERENCES afrisan.Cilindros(Id_Refrigerante),
  CONSTRAINT Intervencion_Id_Despacho_Id_Cilindro_Id_OrdenTrabajo_Id_Te_fkey FOREIGN KEY (Id_Despacho) REFERENCES afrisan.Movimientos(Id_Movimiento),
  CONSTRAINT Intervencion_Id_Despacho_Id_Cilindro_Id_OrdenTrabajo_Id_Te_fkey FOREIGN KEY (Id_OrdenTrabajo) REFERENCES afrisan.Movimientos(Id_OrdenTrabajo),
  CONSTRAINT Intervencion_Id_Despacho_Id_Cilindro_Id_OrdenTrabajo_Id_Te_fkey FOREIGN KEY (Id_Cilindro) REFERENCES afrisan.Movimientos(Id_Cilindro),
  CONSTRAINT Intervencion_Id_Despacho_Id_Cilindro_Id_OrdenTrabajo_Id_Te_fkey FOREIGN KEY (Id_Tecnico) REFERENCES afrisan.Movimientos(Id_Tecnico)
);
CREATE TABLE afrisan.PreciosRefrigerantes (
  Id_Precio bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  Id_Refrigerante bigint NOT NULL,
  PrecioGrRefrigerante numeric NOT NULL CHECK ("PrecioGrRefrigerante" >= 0::numeric),
  Moneda character varying NOT NULL DEFAULT 'CLP'::character varying CHECK ("Moneda"::text ~ '^[A-Z]{3}$'::text),
  VigenteDesde timestamp with time zone NOT NULL,
  CONSTRAINT PreciosRefrigerantes_pkey PRIMARY KEY (Id_Precio),
  CONSTRAINT PreciosRefrigerantes_Id_Refrigerante_fkey FOREIGN KEY (Id_Refrigerante) REFERENCES afrisan.Refrigerantes(Id_Refrigerante)
);
CREATE TABLE afrisan.Costos (
  Id_Costos bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  Id_Intervencion bigint NOT NULL UNIQUE,
  Id_Refrigerante bigint NOT NULL,
  Id_Cilindro bigint NOT NULL,
  GrUtilizados bigint NOT NULL CHECK ("GrUtilizados" > 0),
  PrecioGrRefrigerante numeric NOT NULL CHECK ("PrecioGrRefrigerante" >= 0::numeric),
  PrecioCilindro numeric NOT NULL DEFAULT 0 CHECK ("PrecioCilindro" >= 0::numeric),
  Moneda character varying NOT NULL DEFAULT 'CLP'::character varying CHECK ("Moneda"::text ~ '^[A-Z]{3}$'::text),
  FechaValoracion timestamp with time zone NOT NULL DEFAULT now(),
  PrecioTotal numeric DEFAULT ((("GrUtilizados")::numeric * "PrecioGrRefrigerante") + "PrecioCilindro"),
  CONSTRAINT Costos_pkey PRIMARY KEY (Id_Costos),
  CONSTRAINT Costos_Id_Intervencion_Id_Cilindro_Id_Refrigerante_GrUtili_fkey FOREIGN KEY (Id_Intervencion) REFERENCES afrisan.Intervencion(Id_Intervencion),
  CONSTRAINT Costos_Id_Intervencion_Id_Cilindro_Id_Refrigerante_GrUtili_fkey FOREIGN KEY (Id_Cilindro) REFERENCES afrisan.Intervencion(Id_Cilindro),
  CONSTRAINT Costos_Id_Intervencion_Id_Cilindro_Id_Refrigerante_GrUtili_fkey FOREIGN KEY (Id_Refrigerante) REFERENCES afrisan.Intervencion(Id_Refrigerante),
  CONSTRAINT Costos_Id_Intervencion_Id_Cilindro_Id_Refrigerante_GrUtili_fkey FOREIGN KEY (GrUtilizados) REFERENCES afrisan.Intervencion(GrUtilizados)
);
CREATE TABLE afrisan.CertificadosDisposicion (
  Id_Certificado bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  Folio character varying NOT NULL,
  EmpresaEmisora character varying NOT NULL DEFAULT 'Regener'::character varying,
  FechaEntrega date NOT NULL,
  FechaEmision date NOT NULL,
  Documento text NOT NULL CHECK (btrim("Documento") <> ''::text),
  Id_Usuario integer NOT NULL,
  FechaRegistro timestamp with time zone NOT NULL DEFAULT now(),
  CONSTRAINT CertificadosDisposicion_pkey PRIMARY KEY (Id_Certificado),
  CONSTRAINT CertificadosDisposicion_Id_Usuario_fkey FOREIGN KEY (Id_Usuario) REFERENCES afrisan.Usuario(Id_Usuario)
);
CREATE TABLE afrisan.CertificadosCilindros (
  Id_Certificado bigint NOT NULL,
  Id_Cilindro bigint NOT NULL UNIQUE,
  CONSTRAINT CertificadosCilindros_pkey PRIMARY KEY (Id_Certificado, Id_Cilindro),
  CONSTRAINT CertificadosCilindros_Id_Certificado_fkey FOREIGN KEY (Id_Certificado) REFERENCES afrisan.CertificadosDisposicion(Id_Certificado),
  CONSTRAINT CertificadosCilindros_Id_Cilindro_fkey FOREIGN KEY (Id_Cilindro) REFERENCES afrisan.Cilindros(Id_Cilindro)
);
CREATE TABLE afrisan.Auditoria (
  Id_Auditoria bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  Fecha timestamp with time zone NOT NULL DEFAULT now(),
  Id_Usuario integer,
  UsuarioBD text NOT NULL DEFAULT SESSION_USER,
  Tabla text NOT NULL,
  Id_Registro text NOT NULL,
  Accion character varying NOT NULL CHECK ("Accion"::text = ANY (ARRAY['INSERT'::character varying, 'UPDATE'::character varying, 'DELETE'::character varying]::text[])),
  ValoresAnteriores jsonb,
  ValoresNuevos jsonb,
  CONSTRAINT Auditoria_pkey PRIMARY KEY (Id_Auditoria),
  CONSTRAINT Auditoria_Id_Usuario_fkey FOREIGN KEY (Id_Usuario) REFERENCES afrisan.Usuario(Id_Usuario)
);