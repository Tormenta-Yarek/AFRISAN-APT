# G01: revisión de contratos y persistencia

Resultado posterior de implementación: el proveedor PostgreSQL, usuarios,
catálogos y flujo operativo ya están programados y probados contra una base
aislada, incluidos reinicios. [Estado de activación y alcance](../03_CODIGO/PERSISTENCIA.md).
El SQL 07 ya se aplicó remotamente después de la autorización explícita de Dante;
la conexión restringida está verificada y el administrador inicial ya fue creado.
La revisión inicial siguiente se conserva
como antecedente de las diferencias que se resolvieron.

Fecha: **02/11/2026**. Revisión local del código y del instalador 05.
Dante informó que ejecutó el reemplazo de la base. El acceso autenticado desde
este equipo ya quedó verificado: TLS con CA oficial, 30 tablas y ocho vistas
esperadas en `afrisan`, con RLS habilitado. La configuración está en User Secrets;
el fallo `UntrustedRoot` se resolvió configurando la CA pública oficial.
Evidencia: [SUP-01 verificado](SUPABASE_2026_11_02.md).
La API normal continúa en memoria; esta revisión no habilita escrituras remotas.

## Correspondencia del flujo

| API / dato actual | Persistencia en afrisan | Ajuste necesario |
| --- | --- | --- |
| RefrigeranteResponse.Id | Refrigerantes.Id_Refrigerante | Identidad de PostgreSQL; código normalizado y único. Sin campos de emisiones. |
| CilindroResponse.Id y Codigo | Cilindros.Id_Cilindro y CilindroQR | No usar contadores en memoria para generar códigos durables. |
| SucursalInicial / Sucursal / Direccion como texto | Cliente y Sucursales; referencias Id_Sucursal | Incorporar selección por ID y gestión de clientes/sucursales. Un nombre no identifica de forma única una sucursal. |
| PesajeInicial / UltimoPesaje | Pesaje.Tipo y Cilindros.Id_UltimoPesaje | Mantener lecturas separadas; alta y pesaje inicial en una transacción. |
| CrearOtRequest.TecnicoEmail | Usuario.Id_Usuario / Rol | Resolver un usuario persistido activo; no usar SesionesDemo como directorio. |
| OtResponse.Id UUID | OrdenTrabajo.Id_Publico | Conservar UUID en rutas; resolver PK bigint internamente. |
| Detalle y CilindrosARetirar | DetalleOT y RetiroPrevisto | Guardar junto a la OT y comprobar reservas concurrentes. |
| EntregarCilindroRequest.IdOperacion | Despacho.Id_Despacho | Mantener UUID en reintentos y usar retirar_cilindro con foto verificada. |
| GuardarOperacionRequest.Id y Version | OperacionTerreno.Id_Operacion y Version | Insertar borrador con versión 1; actualizar con comparación de versión. |
| PesoRetiradoGr / PesoInstaladoGr | Pesaje de Retirado / Instalado | Referenciar el cilindro y la OT; no sobrescribir pesajes confirmados. |
| Fotos en Base64 / byte[] | EvidenciaPesaje + objeto privado en Storage | Verificar archivo real, MIME, tamaño y hash; guardar metadatos y acceso privado. |
| EntregaInformada | FechaEntregaInformada e Id_EntregaInformadaPor | Registrar mediante actor autenticado; la recepción requiere ese antecedente. |
| RecibidoBodega y PesoRecepcionGr | RecepcionBodega y Pesaje de Recepcion | Conservar la diferencia con terreno; no sustituir la lectura anterior. |
| AprobacionBodega / RevisionAutorizador | RevisionOT por etapa | Mantener Bodega antes de Autorizador y separar la revisión del cierre. |
| Incidencias | Incidencia | UUID estable, autor/revisor persistidos y resolución sin reescribir el hecho. |
| Historial | EventoOT, HistorialCilindro y Auditoria | Usar los registros durables y los triggers; evitar duplicar eventos automáticos. |

Fuentes: `03_CODIGO/Afrisan.Nuevo.Contratos/Inventario.cs`,
`TrabajoTecnico.cs`, `Afrisan.Nuevo.Api/InventarioStore.cs`, `TrabajoStore.cs`,
`SesionesDemo.cs`, `InventarioEndpoints.cs`, `TrabajoEndpoints.cs` y
`02_BASE_DATOS/contexto_actual/*.sql`.

## Diferencias que deben resolverse en la implementación

1. **Identidad:** el JWT demo usa correo en NameIdentifier; SQL exige un usuario
   entero activo. El proveedor persistente requiere autenticación real y un
   actor de servidor verificado. No aprovisionar usuarios ficticios ni asociar
   automáticamente cuentas demo con registros reales.
2. **Clientes y sucursales:** la base nueva está vacía. Deben existir catálogos
   de clientes/sucursales antes de registrar OT o cilindros instalados. No crear
   un cliente supuesto para aceptar el texto del prototipo.
3. **Idempotencia de OT:** el prototipo guarda IdOperacion y una huella solo en
   un diccionario. El SQL tiene Id_Publico, pero no una huella de creación.
   Definir persistencia de esa clave/huella o reutilizar explícitamente el UUID
   público con comparación completa del pedido. Un UNIQUE aislado no basta.
4. **Versiones:** Version=0 significa alta en el cliente actual; SQL arranca en 1.
   El adaptador debe traducir el alta y devolver la versión guardada. Un conflicto
   se devuelve como 409 conservando el borrador local.
5. **Ámbito offline:** Instancia se genera al arrancar SesionesDemo y el cliente
   conserva borradores por instancia. Para datos durables debe usarse un ámbito
   estable de instalación y usuario, conservado entre reinicios; no eliminar
   simplemente la comprobación que evita reenviar a otra base.
6. **Storage:** subir un objeto y confirmar una transacción PostgreSQL son pasos
   separados. Usar UUID/ruta/hash estables, conservar reintentos y resolver
   objetos huérfanos. No confirmar un retiro u operación con metadatos ficticios.
7. **Permisos PostgreSQL:** el diagnóstico como propietario solo acredita
   transporte y estructura. El backend requiere permisos y políticas explícitos;
   hoy RLS está habilitado sin políticas de navegador. Probar una identidad que
   no sea propietaria antes de declarar aislamiento correcto.

## Transacciones previstas

- Cada escritura establece `afrisan.id_usuario` mediante set_config con ámbito
  local a la transacción; el ID procede de la sesión verificada en el servidor.
- Alta de cilindro: envase, pesaje inicial opcional y efectos de sus triggers.
- Alta de OT: cabecera, detalle, reservas e identificación de reintentos.
- Retiro: invocar `afrisan.retirar_cilindro`, conservando UUID y contenido del
  reintento; el SQL coordina pesaje, evidencia, custodia y estado.
- Borrador/fotos/confirmación: comparar versión y bloquear la operación; las
  fotografías deben estar verificadas antes de confirmar. No modificar hechos
  ya confirmados al reintentar un borrador.
- Recepción, revisiones y cierre: conservar los pasos independientes del flujo;
  cada comando es atómico y responde solo después de confirmar la transacción.
- Evitar eludir restricciones del SQL con actualizaciones directas de estado,
  ubicación o peso. El cierre valida sus propios antecedentes históricos.

## Orden propuesto para G02

1. Verificar conexión TLS y los objetos del esquema nuevo (SUP-01).
2. Definir identidad restringida del backend y autenticación/usuarios reales
   necesarios para las escrituras; coordinar esta parte con G03.
3. Incorporar EF Core/PostgreSQL, modelos del esquema existente y repositorios.
   No usar EnsureCreated ni migraciones automáticas sobre esta base.
4. Incorporar catálogos de clientes/sucursales; persistir refrigerantes,
   cilindros y pesaje inicial, con lecturas desde PostgreSQL.
5. Persistir OT, asignaciones, reservas y el recorrido completo con archivos
   privados; resolver las diferencias anteriores antes de habilitarlo.
6. Adaptar los borradores al ámbito estable y repetir pruebas de reinicio,
   conflictos y permisos con dos usuarios y conexiones distintas.

Resultado local esperado: crear un registro, reiniciar la API y recuperarlo sin
pérdidas; después repetir el recorrido Técnico → Bodega → Autorizador.
La revisión está documentada y SUP-01 verificado; G02 y la validación integral siguen pendientes.
Las emisiones quedan fuera del alcance de esta etapa.
