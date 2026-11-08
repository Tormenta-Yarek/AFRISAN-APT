// Ejecuta el paquete en PostgreSQL embebido aislado. No usa conexiones ni secretos.
import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath,pathToFileURL} from 'node:url';
import {randomUUID,createHash} from 'node:crypto';
const directory=path.dirname(fileURLToPath(import.meta.url));
const root=path.resolve(directory,'../../..');
const sqlPath=process.env.AFRISAN_SQL_PATH || path.join(root,'AFRISAN_NUEVO/02_BASE_DATOS/04_Crear_Afrisan_Contexto_Actual.sql');
const reportPath=process.env.AFRISAN_REPORT_PATH || path.join(directory,'resultados.json');
const modulePath=process.env.PGLITE_MODULE_PATH || path.join(root,'.local/validacion_sql_contexto/node_modules/@electric-sql/pglite/dist/index.js');
const {PGlite}=await import(pathToFileURL(modulePath).href);
const db=new PGlite();
const results=[];
const schema=process.env.AFRISAN_SCHEMA || 'afrisan_actual';
if(!['afrisan_actual','afrisan'].includes(schema)) throw Error('Esquema de prueba no admitido');
const sinEmisiones=schema==='afrisan';
const anterior=sinEmisiones?'afrisan_anterior_prueba':'afrisan';
const report={tipo:'PostgreSQL embebido PGlite, aislado y sin Supabase',fecha_inicio_utc:new Date().toISOString(),archivo_sql:path.relative(root,sqlPath),estado:'EN_CURSO',comprobaciones:results};
const q=(tx,sql,args=[])=>tx.query(sinEmisiones?sql.replaceAll('afrisan_bi_actual','afrisan').replaceAll('afrisan_actual','afrisan'):sql,args);
const first=async(tx,sql,args=[]) => (await q(tx,sql,args)).rows[0];
const table=n=>`${schema}."${n}"`;
async function check(ok,name,details){results.push({resultado:ok?'OK':'FALLO',comprobacion:name,...(details?{detalle:details}:{})}); if(!ok)throw Error(name); console.log('OK:',name);}
async function as(actor,fn){return db.transaction(async tx=>{await q(tx,"select set_config('afrisan_actual.id_usuario',$1,true)",[String(actor)]);return fn(tx);});}
async function fail(actor,name,fn){let error;try{await as(actor,fn);}catch(e){error=e;}await check(!!error,name,error?.message);}
async function insert(tx,name,fields){const keys=Object.keys(fields);return first(tx,`insert into ${table(name)} (${keys.map(x=>`"${x}"`).join(',')}) values (${keys.map((_,i)=>'$'+(i+1)).join(',')}) returning *`,Object.values(fields));}
const uu=()=>randomUUID();
const fotos=(name)=>[{id:uu(),bucket:'pesajes-privados',ruta:`pruebas/${uu()}/${name}.png`,nombre:name+'.png',mime:'image/png',bytes:100,sha256:uu().replaceAll('-','')+uu().replaceAll('-','')}];
async function photo(tx,id,name){const f=fotos(name)[0];return insert(tx,'EvidenciaPesaje',{Id_Evidencia:f.id,Id_Pesaje:id,BucketPrivado:f.bucket,RutaObjeto:f.ruta,NombreArchivo:f.nombre,ContentType:f.mime,Bytes:f.bytes,SHA256:f.sha256,Id_Usuario:1});}
async function weigh(tx,c,ot,type,weight){return insert(tx,'Pesaje',{Id_Cilindro:c,Id_OrdenTrabajo:ot,Tipo:type,PesoBrutoGr:weight,Id_Usuario:1});}
async function confirm(tx,id){return first(tx,`update ${table('OperacionTerreno')} set "Estado"='Registrada',"Version"="Version"+1 where "Id_Operacion"=$1 returning *`,[id]);}
async function receive(tx,ot,c,operation,unused,weight){const p=await weigh(tx,c,ot,'Recepcion',weight);return insert(tx,'RecepcionBodega',{Id_OrdenTrabajo:ot,Id_Cilindro:c,Id_Operacion:operation,Id_DespachoSinUso:unused,Id_Pesaje:p.Id_Pesaje,Id_Bodeguero:1});}
async function revision(tx,ot,stage){return insert(tx,'RevisionOT',{Id_OrdenTrabajo:ot,Etapa:stage,Id_Usuario:1,ConfirmoRevision:true});}
const coreCases=[];
try{
  await db.exec(`create schema ${anterior}; create table ${anterior}."DatoAnterior" (id integer primary key); insert into ${anterior}."DatoAnterior" values(42); create role anon; create role authenticated;`);
  const sql=await fs.readFile(sqlPath,'utf8');
  report.sha256_sql=createHash('sha256').update(sql).digest('hex');
  await db.exec(sql);
  report.motor=(await first(db,'select version() as version')).version;
  const tables=(await q(db,"select tablename from pg_tables where schemaname=$1 order by tablename",[schema])).rows;
  report.tablas=tables.length;
  for(const t of tables){await check(Number((await first(db,`select count(*) as n from ${table(t.tablename)}`)).n)===0,'Tabla vacía al instalar: '+t.tablename);}
  await check(Number((await first(db,`select id from ${anterior}."DatoAnterior"`)).id)===42,'La instalación nueva conserva los esquemas ajenos');
  if(sinEmisiones){
    await check(tables.length===30,'Esquema único con 30 tablas operativas');
    await check(Number((await first(db,"select count(*) as n from pg_views where schemaname=$1",[schema])).n)===8,'Ocho vistas operativas en el mismo esquema');
    await check((await first(db,"select to_regclass('afrisan.\"EstimacionEmisiones\"') is null and to_regclass('afrisan.\"EmisionesEstimadas\"') is null as ok")).ok,'Sin tabla ni vista de emisiones');
    await check(Number((await first(db,"select count(*) as n from information_schema.columns where table_schema=$1 and (column_name ilike '%gwp%' or column_name ilike '%co2%' or column_name='GrEmitidos')",[schema])).n)===0,'Sin columnas de GWP, CO2e ni masa emitida');
  }
  try{await db.exec(sql);throw Error('La reinstalación fue aceptada');}catch(e){if(e.message==='La reinstalación fue aceptada')throw e;await db.exec('rollback');await check(/already exists/.test(e.message),'Reinstalar se detiene sin reemplazar datos',e.message);}
  await db.exec('set role anon');
  let denied=false;try{await q(db,`select * from ${table('Usuario')}`);}catch{denied=true;}finally{await db.exec('reset role');}
  await check(denied,'anon no puede leer usuarios');
  await check((await first(db,"select count(*) as n from pg_tables where schemaname=$1 and rowsecurity=false",[schema])).n===0,'Todas las tablas tienen RLS habilitado');
  await db.exec(`insert into ${table('Rol')}("Nombre_Rol") values ('Administrador'),('Bodega'),('Tecnico'),('Autorizador'),('Jefatura');`);
  const users={};
  for(const role of ['Administrador','Bodega','Tecnico','Autorizador','Jefatura']){
    const rol=await first(db,`select "Id_Rol" from ${table('Rol')} where "Nombre_Rol"=$1`,[role]);
    users[role]=(await insert(db,'Usuario',{Nombre:'Prueba',Apellido:role,Correo_Usuario:role.toLowerCase()+'@example.invalid',Clave_Usuario:'HASH_SINTETICO_NO_USAR_EN_PRODUCCION_000000',Id_Rol:rol.Id_Rol})).Id_Usuario;
  }
  const other=(await insert(db,'Usuario',{Nombre:'Otro',Apellido:'Tecnico',Correo_Usuario:'otro@example.invalid',Clave_Usuario:'HASH_SINTETICO_NO_USAR_EN_PRODUCCION_000000',Id_Rol:(await first(db,`select "Id_Rol" from ${table('Rol')} where "Nombre_Rol"='Tecnico'`)).Id_Rol})).Id_Usuario;
  await check(!!users.Autorizador,'Rol Autorizador admitido');
  const client=await insert(db,'Cliente',{RutCliente:'PRUEBA-1',NombreEmpresa:'Cliente sintético'});
  for(let i=1;i<=3;i++){
    const tag='Caso '+i;
    const branch=await insert(db,'Sucursales',{Id_Cliente:client.Id_Cliente,NombreSucursal:'Sucursal prueba '+i,Direccion:'Dirección simulada '+i});
    const gas=await as(users.Bodega,tx=>insert(tx,'Refrigerantes',{CodigoRefrigerante:'GAS-PRUEBA-'+i,NombreRefrigerante:'Gas sintético '+i}));
    const cylinders=[];
    for(let j=0;j<3;j++){
      const c=await as(users.Bodega,tx=>insert(tx,'Cilindros',{CilindroQR:'SQL-'+i+'-'+j,NumeroSerie:'SER-'+i+'-'+j,Id_Refrigerante:gas.Id_Refrigerante,Capacidadgr:10000+i*1000+j*2000,TaraGr:1000+i*100+j*200,Ubicacion:j===0?'Sucursal':'Bodega',Id_Sucursal:j===0?branch.Id_Sucursal:null,Id_Registrador:users.Bodega}));
      await as(users.Bodega,tx=>weigh(tx,c.Id_Cilindro,null,'Inicial',3000+i*100+j*2000));
      cylinders.push(c);
    }
    const [old,installed,spare]=cylinders;
    const ot=await as(users.Autorizador,async tx=>{
      const o=await insert(tx,'OrdenTrabajo',{CodigoOT:'SQL-OT-'+i,Id_Sucursal:branch.Id_Sucursal,Id_Tecnico:users.Tecnico,Id_Autorizador:users.Autorizador,Descripcion:'Reemplazo sintético '+i});
      await insert(tx,'DetalleOT',{Id_OrdenTrabajo:o.Id_OrdenTrabajo,Id_Refrigerante:gas.Id_Refrigerante,GrSolicitados:9000+i*1000,GrAutorizados:8000+i*1000});
      await insert(tx,'RetiroPrevisto',{Id_OrdenTrabajo:o.Id_OrdenTrabajo,Id_Cilindro:old.Id_Cilindro});return o;
    });
    const id=ot.Id_OrdenTrabajo;
    const dispatches=[];
    for(const c of [installed,spare]){
      const request={id:uu(),c:c.Id_Cilindro,peso:5000+i*100,fotos:fotos('salida-'+c.Id_Cilindro)};
      const send=(tx,req=request)=>first(tx,'select * from afrisan_actual.retirar_cilindro($1,$2,$3,$4,$5::jsonb)',[req.id,id,req.c,req.peso,JSON.stringify(req.fotos)]);
      await fail(users.Bodega,tag+': Bodega no suplanta el retiro del Técnico',tx=>send(tx));
      await fail(other,tag+': Técnico ajeno no despacha',tx=>send(tx));
      await fail(users.Tecnico,tag+': despacho sin foto rechazado',tx=>send(tx,{...request,fotos:[]}));
      await fail(users.Tecnico,tag+': despacho con peso inválido rechazado',tx=>send(tx,{...request,peso:1}));
      const d=await as(users.Tecnico,tx=>send(tx));
      const repeated=await as(users.Tecnico,tx=>send(tx));
      await check(d.Id_Despacho===repeated.Id_Despacho,tag+': reintento idéntico no duplica despacho');
      await fail(users.Tecnico,tag+': mismo UUID con otro peso rechazado',tx=>send(tx,{...request,peso:request.peso-1}));
      await fail(users.Tecnico,tag+': cilindro ya en custodia no se despacha otra vez',tx=>send(tx,{...request,id:uu()}));
      dispatches.push(d);
    }
    const current=await first(db,`select * from ${table('Cilindros')} where "Id_Cilindro"=$1`,[installed.Id_Cilindro]);
    await check(current.Ubicacion==='Tecnico' && current.Estado==='EnProcesoDespacho',tag+': custodia y estado de despacho automáticos');
    const op=await as(users.Tecnico,async tx=>{
      const pn=await weigh(tx,installed.Id_Cilindro,id,'Instalado',4900+i*100);
      const pv=await weigh(tx,old.Id_Cilindro,id,'Retirado',Number(old.TaraGr)+i*100);
      return insert(tx,'OperacionTerreno',{Id_Operacion:uu(),Id_OrdenTrabajo:id,Tipo:'Reemplazo',Id_Instalado:installed.Id_Cilindro,Id_Retirado:old.Id_Cilindro,Id_Despacho:dispatches[0].Id_Despacho,Id_PesajeInstalado:pn.Id_Pesaje,Id_PesajeRetirado:pv.Id_Pesaje,Id_Tecnico:users.Tecnico});
    });
    await fail(users.Tecnico,tag+': no confirmar cambio sin fotos',tx=>confirm(tx,op.Id_Operacion));
    await fail(users.Tecnico,tag+': no anticipar devolución en borrador',tx=>q(tx,`update ${table('OperacionTerreno')} set "FechaEntregaInformada"=now(),"Id_EntregaInformadaPor"=$2,"Version"="Version"+1 where "Id_Operacion"=$1`,[op.Id_Operacion,users.Tecnico]));
    await as(users.Tecnico,async tx=>{await photo(tx,op.Id_PesajeInstalado,'instalado');await photo(tx,op.Id_PesajeRetirado,'retirado');await confirm(tx,op.Id_Operacion);});
    const locations=(await q(db,`select "Id_Cilindro","Ubicacion" from ${table('Cilindros')} where "Id_Cilindro"=any($1::bigint[])`,[[old.Id_Cilindro,installed.Id_Cilindro]])).rows;
    await check(locations.find(c=>c.Id_Cilindro===old.Id_Cilindro).Ubicacion==='Tecnico'&&locations.find(c=>c.Id_Cilindro===installed.Id_Cilindro).Ubicacion==='Sucursal',tag+': retirado e instalado cambian a sus ubicaciones correctas');
    await fail(users.Tecnico,tag+': foto posterior a confirmación rechazada',tx=>photo(tx,op.Id_PesajeRetirado,'posterior'));
    await fail(users.Bodega,tag+': recibir exige devolución informada',tx=>receive(tx,id,old.Id_Cilindro,op.Id_Operacion,null,1500));
    await as(users.Tecnico,tx=>q(tx,`update ${table('OperacionTerreno')} set "FechaEntregaInformada"=now(),"Id_EntregaInformadaPor"=$2,"Version"="Version"+1 where "Id_Operacion"=$1`,[op.Id_Operacion,users.Tecnico]));
    await as(users.Tecnico,tx=>q(tx,`update ${table('OrdenTrabajo')} set "Estado"='PendienteBodega' where "Id_OrdenTrabajo"=$1`,[id]));
    await fail(users.Autorizador,tag+': Autorizador no revisa antes de Bodega',tx=>revision(tx,id,'Autorizador'));
    await fail(users.Bodega,tag+': Bodega no aprueba retirados pendientes',tx=>revision(tx,id,'Bodega'));
    const incident=await as(users.Tecnico,tx=>insert(tx,'Incidencia',{Id_OrdenTrabajo:id,Id_Operacion:op.Id_Operacion,Tipo:'Incidencia',Motivo:'Observación simulada para revisar',Id_Usuario:users.Tecnico}));
    await fail(users.Tecnico,tag+': no fabricar metadatos de revisión con respuesta nula',tx=>insert(tx,'Incidencia',{Id_OrdenTrabajo:id,Tipo:'Incidencia',Motivo:'Intento simulado de revisión incompleta',Id_Usuario:users.Tecnico,Id_Revisor:users.Autorizador,FechaRevision:'2026-10-25T00:00:00Z'}));
    const receiveWeight=Number(old.TaraGr)+i*100+(i===2?50:0);
    await as(users.Bodega,tx=>receive(tx,id,old.Id_Cilindro,op.Id_Operacion,null,receiveWeight));
    await check(!(await first(db,`select 1 as x from ${table('Despacho')} where "Id_Cilindro"=$1 and "Id_OrdenTrabajo"=$2`,[old.Id_Cilindro,id])),tag+': recibe retirado que nunca fue despachado en esta OT');
    await fail(users.Bodega,tag+': Bodega no aprueba reserva sin devolver',tx=>revision(tx,id,'Bodega'));
    await as(users.Bodega,tx=>receive(tx,id,spare.Id_Cilindro,null,dispatches[1].Id_Despacho,5000+i*100));
    await as(users.Bodega,tx=>revision(tx,id,'Bodega'));
    await fail(users.Autorizador,tag+': revisión espera incidencias resueltas',tx=>revision(tx,id,'Autorizador'));
    await as(users.Autorizador,tx=>q(tx,`update ${table('Incidencia')} set "Respuesta"='Fotografías y pesos revisados' where "Id_Incidencia"=$1`,[incident.Id_Incidencia]));
    await fail(users.Autorizador,tag+': cerrar exige revisión final del Autorizador',tx=>q(tx,`update ${table('OrdenTrabajo')} set "Estado"='Cerrada' where "Id_OrdenTrabajo"=$1`,[id]));
    await as(users.Autorizador,tx=>revision(tx,id,'Autorizador'));
    await as(users.Autorizador,tx=>q(tx,`update ${table('OrdenTrabajo')} set "Estado"='Cerrada' where "Id_OrdenTrabajo"=$1`,[id]));
    const closed=await first(db,`select * from ${table('OrdenTrabajo')} where "Id_OrdenTrabajo"=$1`,[id]);
    await check(closed.Estado==='Cerrada',tag+': cierre con las dos revisiones');
    const difference=await first(db,'select * from afrisan_bi_actual."PesajesRecepcion" where "Id_Operacion"=$1',[op.Id_Operacion]);
    await check(Number(difference.DiferenciaGr)===(i===2?50:0),tag+': ambos pesos conservados y diferencia visible');
    await fail(users.Bodega,tag+': no reescribir el peso confirmado',tx=>q(tx,`update ${table('Pesaje')} set "PesoBrutoGr"="PesoBrutoGr"+1 where "Id_Pesaje"=$1`,[op.Id_PesajeRetirado]));
    await fail(users.Autorizador,tag+': revisiones inmutables',tx=>q(tx,`delete from ${table('RevisionOT')} where "Id_OrdenTrabajo"=$1`,[id]));
    coreCases.push({ot:id,op:op.Id_Operacion,branch:branch.Id_Sucursal,gas:gas.Id_Refrigerante,old:old.Id_Cilindro,installed:installed.Id_Cilindro,spare:spare.Id_Cilindro});
  }
  // Agregado sin retiro ficticio, ejecución parcial y liberación de reserva.
  const reusable=coreCases[2];
  const partial=await as(users.Autorizador,async tx=>{
    const o=await insert(tx,'OrdenTrabajo',{CodigoOT:'SQL-OT-AGREGADO',Id_Sucursal:reusable.branch,Id_Tecnico:users.Tecnico,Id_Autorizador:users.Autorizador,Descripcion:'Agregado parcial de prueba'});
    await insert(tx,'DetalleOT',{Id_OrdenTrabajo:o.Id_OrdenTrabajo,Id_Refrigerante:reusable.gas,GrSolicitados:1000,GrAutorizados:1000});
    await insert(tx,'RetiroPrevisto',{Id_OrdenTrabajo:o.Id_OrdenTrabajo,Id_Cilindro:reusable.installed});return o;
  });
  const extra=await as(users.Tecnico,tx=>first(tx,'select * from afrisan_actual.retirar_cilindro($1,$2,$3,$4,$5::jsonb)',[uu(),partial.Id_OrdenTrabajo,reusable.spare,2000,JSON.stringify(fotos('agregado'))]));
  await as(users.Tecnico,async tx=>{
    const p=await weigh(tx,reusable.spare,partial.Id_OrdenTrabajo,'Instalado',1900);await photo(tx,p.Id_Pesaje,'agregado-instalado');
    const op=await insert(tx,'OperacionTerreno',{Id_Operacion:uu(),Id_OrdenTrabajo:partial.Id_OrdenTrabajo,Tipo:'Agregado',Id_Instalado:reusable.spare,Id_Despacho:extra.Id_Despacho,Id_PesajeInstalado:p.Id_Pesaje,Id_Tecnico:users.Tecnico});
    await confirm(tx,op.Id_Operacion);
  });
  await fail(users.Tecnico,'Ejecución parcial exige explicación',tx=>q(tx,`update ${table('OrdenTrabajo')} set "Estado"='PendienteBodega' where "Id_OrdenTrabajo"=$1`,[partial.Id_OrdenTrabajo]));
  await as(users.Tecnico,tx=>q(tx,`update ${table('OrdenTrabajo')} set "Estado"='PendienteBodega',"ObservacionTermino"='Solo se realizó agregado; el retiro queda sin ejecutar' where "Id_OrdenTrabajo"=$1`,[partial.Id_OrdenTrabajo]));
  await as(users.Bodega,tx=>revision(tx,partial.Id_OrdenTrabajo,'Bodega'));
  await as(users.Autorizador,async tx=>{await revision(tx,partial.Id_OrdenTrabajo,'Autorizador');await q(tx,`update ${table('OrdenTrabajo')} set "Estado"='Cerrada' where "Id_OrdenTrabajo"=$1`,[partial.Id_OrdenTrabajo]);});
  await check((await first(db,`select "Estado" from ${table('RetiroPrevisto')} where "Id_OrdenTrabajo"=$1`,[partial.Id_OrdenTrabajo])).Estado==='NoEjecutado','Cierre parcial libera la reserva sin fingir retiro físico');
  const next=await as(users.Autorizador,async tx=>{
    const o=await insert(tx,'OrdenTrabajo',{CodigoOT:'SQL-OT-REUTILIZACION',Id_Sucursal:reusable.branch,Id_Tecnico:users.Tecnico,Id_Autorizador:users.Autorizador,Descripcion:'Reutilización del cilindro devuelto de una OT anterior'});
    await insert(tx,'DetalleOT',{Id_OrdenTrabajo:o.Id_OrdenTrabajo,Id_Refrigerante:reusable.gas,GrSolicitados:1000,GrAutorizados:1000});
    await insert(tx,'RetiroPrevisto',{Id_OrdenTrabajo:o.Id_OrdenTrabajo,Id_Cilindro:reusable.installed});return o;
  });
  const reused=await as(users.Tecnico,tx=>first(tx,'select * from afrisan_actual.retirar_cilindro($1,$2,$3,$4,$5::jsonb)',[uu(),next.Id_OrdenTrabajo,reusable.old,1600,JSON.stringify(fotos('reutilizacion'))]));
  const reusedOperation=await as(users.Tecnico,async tx=>{
    const pn=await weigh(tx,reusable.old,next.Id_OrdenTrabajo,'Instalado',1550),pv=await weigh(tx,reusable.installed,next.Id_OrdenTrabajo,'Retirado',1700);
    await photo(tx,pn.Id_Pesaje,'reutilizado-instalado');await photo(tx,pv.Id_Pesaje,'reutilizado-retirado');
    const op=await insert(tx,'OperacionTerreno',{Id_Operacion:uu(),Id_OrdenTrabajo:next.Id_OrdenTrabajo,Tipo:'Reemplazo',Id_Instalado:reusable.old,Id_Retirado:reusable.installed,Id_Despacho:reused.Id_Despacho,Id_PesajeInstalado:pn.Id_Pesaje,Id_PesajeRetirado:pv.Id_Pesaje,Id_Tecnico:users.Tecnico});
    return confirm(tx,op.Id_Operacion);
  });
  await q(db,'select afrisan_actual.sin_pendientes($1)',[reusable.ot]);
  await check((await first(db,`select "Estado" from ${table('OrdenTrabajo')} where "Id_OrdenTrabajo"=$1`,[reusable.ot])).Estado==='Cerrada','Reutilización posterior no invalida el cierre de la OT anterior');
  await as(users.Tecnico,async tx=>{
    await q(tx,`update ${table('OperacionTerreno')} set "FechaEntregaInformada"=now(),"Id_EntregaInformadaPor"=$2,"Version"="Version"+1 where "Id_Operacion"=$1`,[reusedOperation.Id_Operacion,users.Tecnico]);
    await q(tx,`update ${table('OrdenTrabajo')} set "Estado"='PendienteBodega' where "Id_OrdenTrabajo"=$1`,[next.Id_OrdenTrabajo]);
  });
  await as(users.Bodega,async tx=>{await receive(tx,next.Id_OrdenTrabajo,reusable.installed,reusedOperation.Id_Operacion,null,1700);await revision(tx,next.Id_OrdenTrabajo,'Bodega');});
  await as(users.Autorizador,async tx=>{await revision(tx,next.Id_OrdenTrabajo,'Autorizador');await q(tx,`update ${table('OrdenTrabajo')} set "Estado"='Cerrada' where "Id_OrdenTrabajo"=$1`,[next.Id_OrdenTrabajo]);});
  await check((await first(db,`select "Estado" from ${table('OrdenTrabajo')} where "Id_OrdenTrabajo"=$1`,[next.Id_OrdenTrabajo])).Estado==='Cerrada','Reemplazo de cilindro reutilizado completa su propio recorrido');
  // Jerarquía de límites y conservación de la versión aplicada.
  const data=coreCases[0];
  const limit=await as(users.Bodega,tx=>insert(tx,'LimiteSucursal',{Id_Sucursal:data.branch,Periodicidad:'Semana',EstadoDefinicion:'Definido',GrLimite:10000,CriterioComputo:'Criterio sintético explícito de prueba; no política operativa',Id_UltimoEditor:users.Bodega}));
  await as(users.Jefatura,tx=>q(tx,`update ${table('LimiteSucursal')} set "GrLimite"=11000 where "Id_Limite"=$1`,[limit.Id_Limite]));
  await fail(users.Bodega,'Límite bloqueado por Jefatura no editable por Bodega',tx=>q(tx,`update ${table('LimiteSucursal')} set "GrLimite"=12000 where "Id_Limite"=$1`,[limit.Id_Limite]));
  await fail(users.Autorizador,'Autorizador no desbloquea decisión de Jefatura',tx=>q(tx,`update ${table('LimiteSucursal')} set "BloqueadoBodega"=false where "Id_Limite"=$1`,[limit.Id_Limite]));
  await as(users.Jefatura,tx=>q(tx,`update ${table('LimiteSucursal')} set "BloqueadoBodega"=false where "Id_Limite"=$1`,[limit.Id_Limite]));
  await as(users.Bodega,tx=>q(tx,`update ${table('LimiteSucursal')} set "GrLimite"=12500 where "Id_Limite"=$1`,[limit.Id_Limite]));
  const snapshots=(await q(db,`select * from ${table('LimiteSucursalHistorial')} where "Id_Limite"=$1 order by "Version"`,[limit.Id_Limite])).rows;
  await check(snapshots.length===4&&Number(snapshots[0].GrLimite)===10000,'Límites conservan cuatro versiones y el valor histórico');
  const measured=await as(users.Bodega,tx=>insert(tx,'MedicionLimite',{Id_HistorialLimite:snapshots[0].Id_HistorialLimite,PeriodoInicio:'2026-10-19',PeriodoFin:'2026-10-25',GrComputados:11000,MetodologiaComputo:'Dato sintético declarado, sin fórmula automática',Id_Registrador:users.Bodega}));
  await as(users.Bodega,tx=>insert(tx,'MedicionLimiteOperaciones',{Id_Medicion:measured.Id_Medicion,Id_Operacion:data.op}));
  await as(users.Jefatura,tx=>insert(tx,'RevisionExceso',{Id_Medicion:measured.Id_Medicion,Conclusion:'Exceso sintético investigado; no atribuye pérdida',Id_Revisor:users.Jefatura}));
  const excess=await first(db,'select * from afrisan_bi_actual."ExcesosRegistrados" where "Id_Medicion"=$1',[measured.Id_Medicion]);
  await check(Number(excess.GrExcesoDeclarado)===1000,'Exceso compara contra el límite histórico aplicado');
  const price=await as(users.Administrador,tx=>insert(tx,'PreciosRefrigerantes',{Id_Refrigerante:data.gas,PrecioGrRefrigerante:'2',Moneda:'CLP',VigenteDesde:'2026-10-01',Id_Registrador:users.Administrador}));
  const cost=await as(users.Administrador,tx=>insert(tx,'Costos',{Id_Operacion:data.op,NumeroLinea:1,Concepto:'Refrigerante',Descripcion:'Valoración sintética expresamente declarada',Cantidad:100,Unidad:'g',TarifaUnitaria:2,Id_Precio:price.Id_Precio,Moneda:'CLP',Id_Registrador:users.Administrador}));
  await check(Number(cost.PrecioTotal)===200,'Costo usa cantidad y tarifa explícitas');
  if(!sinEmisiones){
    await check(Number((await first(db,`select count(*) as n from ${table('EstimacionEmisiones')}`)).n)===0,'Instalar cilindros no crea emisiones ficticias');
    const emission=await as(users.Administrador,tx=>insert(tx,'EstimacionEmisiones',{Id_Operacion:data.op,Id_Refrigerante:data.gas,GrEmitidos:1000,FactorGWPAplicado:2,FuenteGWPAplicada:'Factor sintético de prueba',MetodologiaEstimacion:'Masa de prueba declarada, no contenido instalado',Id_Registrador:users.Administrador}));
    await check(Number(emission.EmisionesCO2eTon)===0.002,'CO2e calculado solo sobre masa emitida declarada');
  }
  await as(users.Administrador,tx=>insert(tx,'BajaCilindro',{Id_Cilindro:data.spare,Motivo:'Baja sintética para probar certificado',EstadoDestino:'Chatarra',Id_Usuario:users.Administrador}));
  const cert=await as(users.Administrador,tx=>insert(tx,'CertificadosDisposicion',{EmpresaEmisora:'Gestor sintético',FechaEntrega:'2026-10-24',Id_UsuarioRegistro:users.Administrador}));
  await as(users.Administrador,tx=>insert(tx,'CertificadosCilindros',{Id_Certificado:cert.Id_Certificado,Id_Cilindro:data.spare}));
  await as(users.Administrador,tx=>q(tx,`update ${table('CertificadosDisposicion')} set "Estado"='Certificado',"Folio"='PRUEBA-1',"FechaEmision"='2026-10-25',"Documento"='privado/certificado-sintetico.pdf' where "Id_Certificado"=$1`,[cert.Id_Certificado]));
  await check((await first(db,`select "Estado" from ${table('Cilindros')} where "Id_Cilindro"=$1`,[data.spare])).Estado==='DisposicionFinalCertificada','Lote certificado deja el cilindro fuera de servicio');
  await check(Number((await first(db,`select count(*) as n from ${table('Auditoria')}`)).n)>0,'Auditoría registra operaciones');
  await check(Number((await first(db,`select count(*) as n from ${table('Auditoria')} where "Tabla"='Usuario' and (("ValoresNuevos" ? 'Clave_Usuario') or ("ValoresNuevos" ? 'VersionSeguridad'))`)).n)===0,'Auditoría excluye hash de contraseña y versión de seguridad');
  await fail(users.Administrador,'Auditoría no se borra',tx=>q(tx,`delete from ${table('Auditoria')}`));
  report.estado='CORRECTO';report.casos=coreCases;
}catch(e){report.estado='FALLO';report.error={mensaje:e.message,detalle:e.detail,contexto:e.where};console.error(report.error);process.exitCode=1;}
finally{report.cantidad_comprobaciones=results.length;report.fecha_fin_utc=new Date().toISOString();await fs.writeFile(reportPath,JSON.stringify(report,(_,v)=>typeof v==='bigint'?v.toString():v,2));await db.close();console.log('Resultado:',report.estado,'Comprobaciones:',results.length);}
