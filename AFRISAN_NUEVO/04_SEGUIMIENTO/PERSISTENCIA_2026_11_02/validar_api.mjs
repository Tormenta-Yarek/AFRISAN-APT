// API real sobre PostgreSQL embebido aislado. No usa Supabase ni sus credenciales.
import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath,pathToFileURL} from 'node:url';
import {spawn} from 'node:child_process';
import {randomBytes,randomUUID} from 'node:crypto';
const directory=path.dirname(fileURLToPath(import.meta.url));
const root=path.resolve(directory,'../../..');
const modules=path.join(root,'.local/validacion_sql_contexto/node_modules/@electric-sql');
const {PGlite}=await import(pathToFileURL(path.join(modules,'pglite/dist/index.js')));
const {PGLiteSocketServer}=await import(pathToFileURL(path.join(modules,'pglite-socket/dist/index.js')));
const db=await PGlite.create();
const socket=new PGLiteSocketServer({db,host:'127.0.0.1',port:55432,maxConnections:1});
const dll=process.env.AFRISAN_API_DLL||path.join(root,'AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Api/bin/Debug/net8.0/Afrisan.Nuevo.Api.dll');
const pruebaStorage=process.env.AFRISAN_TEST_STORAGE==='1';
const pruebaBi=process.env.AFRISAN_TEST_POWERBI==='1';
if(pruebaStorage&&pruebaBi)throw Error('Ejecutar Storage y Power BI por separado para conservar sus informes.');
const validarBi=pruebaBi?(await import('../POWER_BI_2026_11_02/validar_powerbi.mjs')).validarBi:null;
const base='http://127.0.0.1:5199/api/v1/';
const checks=[];
const report={fecha_utc:new Date().toISOString(),tipo:'API .NET 8 + PostgreSQL embebido aislado por TCP',comprobaciones:checks};
const profile=path.join(root,'.local/perfil-pruebas-'+randomUUID());await fs.mkdir(profile,{recursive:true});
const environment={...process.env,APPDATA:profile,LOCALAPPDATA:profile,Logging__EventLog__LogLevel__Default:'None',ASPNETCORE_ENVIRONMENT:'Development',ASPNETCORE_URLS:'http://127.0.0.1:5199',
 ConnectionStrings__AfrisanRuntime:'Host=127.0.0.1;Port=55432;Database=postgres;Username=afrisan_api;Password=prueba-local;SSL Mode=Disable;Pooling=false',
 Persistencia__Modo:'PostgreSQL',Persistencia__PruebasLocales:'true',Persistencia__JwtKey:randomBytes(64).toString('base64'),
 Persistencia__FotosPath:path.join(root,'.local/evidencias-prueba-'+randomUUID())};
let cloud;
if(pruebaStorage){environment.Storage__Url='http://127.0.0.1:55433';const {crearStorageSimulado}=await import('../STORAGE_2026_11_02/storage_simulado.mjs');cloud=await crearStorageSimulado('sb_secret_'+randomBytes(32).toString('hex'));}
let processApi,logs='';
function check(ok,name){checks.push({resultado:ok?'OK':'FALLO',comprobacion:name});if(!ok)throw Error(name);console.log('OK:',name);}
const delay=ms=>new Promise(resolve=>setTimeout(resolve,ms));
function child(args=[]){const p=spawn('dotnet',[dll,...args],{cwd:root,env:environment,windowsHide:true,stdio:['pipe','pipe','pipe']});p.stdout.on('data',x=>logs+=x);p.stderr.on('data',x=>logs+=x);return p;}
async function stop(){if(processApi&&!processApi.killed&&processApi.exitCode===null){const exited=new Promise(r=>processApi.once('exit',r));processApi.kill();await exited;}processApi=null;}
async function start(){processApi=child();for(let i=0;i<100;i++){if(processApi.exitCode!==null)throw Error('API no inició: '+logs.slice(-6000));try{const r=await fetch('http://127.0.0.1:5199/health/ready');if(r.ok)return;}catch{}await delay(100);}throw Error('API timeout: '+logs.slice(-6000));}
async function api(route,token,body,method=body===undefined?'GET':'POST',expected){
 const response=await fetch(base+route,{method,headers:{...(token?{Authorization:'Bearer '+token}:{}),...(body===undefined?{}:{'Content-Type':'application/json'})},body:body===undefined?undefined:JSON.stringify(body)});
 const text=await response.text();let result;try{result=JSON.parse(text);}catch{result=text;}
 // El socket PGlite de una conexión necesita liberar el cierre TCP entre llamadas.
 if(pruebaBi)await delay(30);
 if(expected!==undefined){check(response.status===expected,`${method} ${route}: HTTP ${expected}`);return result;}
 if(!response.ok)throw Error(`${method} ${route}: ${response.status} ${text}`);return result;
}
const password='Solo-Pruebas-2026-'+randomUUID();
const login=async email=>(await api('sesiones',null,{email,password})).token;
// PNG válido de un píxel, generado para cada pesaje con colores distintos.
const {deflateSync}=await import('node:zlib');
function png(color){const crc=b=>{let n=0xffffffff;for(const x of b){n^=x;for(let i=0;i<8;i++)n=n&1?(n>>>1)^0xedb88320:n>>>1;}return(n^0xffffffff)>>>0;};const chunk=(name,data)=>{const b=Buffer.concat([Buffer.from(name),data]);const out=Buffer.alloc(data.length+12);out.writeUInt32BE(data.length);b.copy(out,4);out.writeUInt32BE(crc(b),out.length-4);return out;};const ihdr=Buffer.alloc(13);ihdr.writeUInt32BE(1);ihdr.writeUInt32BE(1,4);ihdr[8]=8;ihdr[9]=2;return Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',ihdr),chunk('IDAT',deflateSync(Buffer.from([0,color,20,30]))),chunk('IEND',Buffer.alloc(0))]).toString('base64');}
try{
 await db.exec(await fs.readFile(path.join(root,'AFRISAN_NUEVO/02_BASE_DATOS/05_Crear_Afrisan_Sin_Emisiones.sql'),'utf8'));
 await db.exec(await fs.readFile(path.join(root,'AFRISAN_NUEVO/02_BASE_DATOS/07_Habilitar_Persistencia.sql'),'utf8'));
 await db.exec("ALTER ROLE afrisan_api LOGIN PASSWORD 'prueba-local'");
 // El socket embebido no autentica el StartupMessage; imponer el rol de prueba
 // explícitamente para comprobar permisos reales sin ejecutar como propietario.
 await db.exec('SET ROLE afrisan_api');
 await socket.start();
 const bootstrap=child(['--crear-administrador']);bootstrap.stdin.end('Prueba\nAdministrador\nadmin@example.invalid\n'+password+'\n'+password+'\n');
 const exit=await new Promise(r=>bootstrap.once('exit',r));
 if(exit!==0) throw Error('Bootstrap falló: '+logs.slice(-7000));
 check(true,'Administrador inicial creado con contraseña por stdin, sin cuentas demo');
 await start();
 let admin=await login('admin@example.invalid');
 const entorno=await api('entorno');check(entorno.modo==='PostgreSQL'&&entorno.cuentas.length===0&&entorno.passwordDemo==='','Entorno público sin cuentas ni contraseña de demostración');
 await api('usuarios',null,undefined,'GET',401);
 for(const rol of ['Bodega','Autorizador','Tecnico','Jefatura'])await api('usuarios',admin,{nombre:'Prueba',apellido:rol,correo:rol.toLowerCase()+'@example.invalid',rol,password});
 await api('usuarios',admin,{nombre:'Otro',apellido:'Tecnico',correo:'otro@example.invalid',rol:'Tecnico',password});
 const bodega=await login('bodega@example.invalid'),autorizador=await login('autorizador@example.invalid'),tecnico=await login('tecnico@example.invalid'),otro=await login('otro@example.invalid');
 const bi=async(etapa)=>{if(validarBi)await validarBi(etapa,{api,check,base,admin,bodega,autorizador,tecnico,jefatura:await login('jefatura@example.invalid'),db,environment,start,stop,child});};
 await bi('vacio');
 await api('usuarios',bodega,undefined,'GET',403);
 await api('usuarios',tecnico,{nombre:'Sin',apellido:'Permiso',correo:'sin@example.invalid',rol:'Administrador',password},'POST',403);
 const cliente=await api('clientes',admin,{rut:'PRUEBA-1',nombre:'Cliente sintético'});
 const sucursal=await api('sucursales',admin,{clienteId:cliente.id,nombre:'Sucursal sintética',direccion:'Dirección de prueba'});
 const gas=await api('refrigerantes',bodega,{codigo:'PRUEBA-GAS',nombre:'Gas de prueba'});
 const cilindro=async(ubicacion,peso)=>api('cilindros',bodega,{ubicacionInicial:ubicacion,sucursalId:ubicacion==='Sucursal'?sucursal.id:null,refrigeranteId:gas.id,capacidadGr:10000,taraGr:1000,pesoInicialGr:peso});
 const old=await cilindro('Sucursal',2500),installed=await cilindro('Bodega',6000),spare=await cilindro('Bodega',5000);
 const before=(await api('cilindros',bodega)).length;
 await api('cilindros',bodega,{ubicacionInicial:'Bodega',refrigeranteId:gas.id,capacidadGr:10000,taraGr:1000,pesoInicialGr:999},'POST',409);
 check((await api('cilindros',bodega)).length===before,'Peso inválido revierte el alta completa del cilindro');
 const request={idOperacion:randomUUID(),sucursalId:sucursal.id,sucursal:sucursal.nombre,direccion:sucursal.direccion,descripcion:'Reemplazo de prueba',tecnicoEmail:'tecnico@example.invalid',detalle:[{refrigeranteId:gas.id,grSolicitados:10000,grAutorizados:9000}],cilindrosARetirar:[old.codigo]};
 let ot=await api('ordenes-trabajo',autorizador,request);
 check((await api('ordenes-trabajo',autorizador,request)).id===ot.id,'Reintento de creación de OT no duplica');
 await api('ordenes-trabajo',autorizador,{...request,descripcion:'Otro pedido'},'POST',409);
 await api('ordenes-trabajo/'+ot.id,otro,undefined,'GET',403);
 check((await api('ordenes-trabajo',otro)).length===0,'El técnico ajeno no lista OT de otro usuario');
 const route='ordenes-trabajo/'+ot.id;
 const photo=color=>({id:randomUUID(),nombre:'pesaje.png',contentType:'image/png',base64:png(color)});
 const salida={idOperacion:randomUUID(),codigo:installed.codigo,pesoBrutoGr:5800,foto:photo(10)};
 ot=await api(route+'/entregas',tecnico,salida);
 ot=await api(route+'/entregas',tecnico,salida);check(ot.entregas.length===1,'Reintento del retiro no duplica despacho ni foto');
 await api(route+'/entregas',tecnico,{...salida,pesoBrutoGr:5700},'POST',409);
 ot=await api(route+'/entregas',tecnico,{idOperacion:randomUUID(),codigo:spare.codigo,pesoBrutoGr:4800,foto:photo(20)});
 const draft={id:randomUUID(),instancia:entorno.instancia,version:0,tipo:'Reemplazo',codigoRetirado:old.codigo,codigoInstalado:installed.codigo,pesoRetiradoGr:1800,pesoInstaladoGr:5600,observacion:'Pesajes de prueba'};
 let op=await api(route+'/operaciones',tecnico,draft,'PUT');
 await bi('borrador');
 const oper=route+'/operaciones/'+op.id;
 await api(oper+'/confirmar?version='+op.version,tecnico,undefined,'POST',400);
 const retirado={...photo(30),grupo:'Retirado',version:op.version};
 op=await api(oper+'/fotos',tecnico,retirado);
 const initialVersion=op.version;
 let imagenInstalada={...photo(40),grupo:'Instalado',version:op.version};
 op=await api(oper+'/fotos',tecnico,imagenInstalada);
 check((await api(oper+'/fotos',tecnico,imagenInstalada)).version===op.version,'Reintento de foto conserva la versión y no duplica');
 await api(route+'/operaciones',tecnico,{...draft,version:initialVersion,pesoInstaladoGr:5500},'PUT',409);
 op=await api(oper+'/fotos/'+imagenInstalada.id+'?version='+op.version,tecnico,undefined,'DELETE');
 imagenInstalada={...photo(50),grupo:'Instalado',version:op.version};op=await api(oper+'/fotos',tecnico,imagenInstalada);
 op=await api(oper+'/confirmar?version='+op.version,tecnico,undefined,'POST');check(op.estado==='Registrada','Confirmación persiste pesajes, archivos y cambio físico');
 await bi('confirmado');
 await api(oper+'/fotos',tecnico,{...photo(70),grupo:'Instalado',version:op.version},'POST',409);
 await api(route+'/revision-autorizador',autorizador,{confirmoRevision:true},'POST',409);
 await api(oper+'/informar-entrega',tecnico,undefined,'POST');
 await api(route+'/termino-tecnico',tecnico,{observacion:'Trabajo finalizado en prueba'});
 await api(route+'/recepciones',bodega,{codigo:old.codigo,pesoBrutoGr:1750,observacion:'Recepción de prueba'});
 ot=await api(route+'/recepciones',bodega,{codigo:spare.codigo,pesoBrutoGr:4800});
 ot=await api(route+'/aprobar-bodega',bodega,{confirmoRevision:true});
 await bi('bodega');
 ot=await api(route+'/revision-autorizador',autorizador,{confirmoRevision:true});
 ot=await api(route+'/cerrar',autorizador,undefined,'POST');check(ot.estado==='Cerrada','Recorrido Técnico → Bodega → Autorizador cerrado en PostgreSQL');
 await bi('cerrado');
 const photoUrl=oper+'/fotos/'+retirado.id;
 const readPhoto=async token=>{const r=await fetch(base+photoUrl,{headers:{Authorization:'Bearer '+token}});if(!r.ok)throw Error('No se pudo leer la foto: '+r.status);return Buffer.from(await r.arrayBuffer());};
 const bytes=await readPhoto(tecnico);
 await api(photoUrl,otro,undefined,'GET',403);
 await stop();await start();
 const after=await api(route,tecnico);check(after.estado==='Cerrada'&&after.operaciones[0].pesoRecepcionGr===1750,'La OT y los pesos de terreno/recepción sobreviven al reinicio');
 check((await api('entorno')).instancia===entorno.instancia,'Identidad de instalación estable después de reiniciar');
 check((await api('cilindros',bodega)).length===before,'Inventario conservado después de reiniciar');
 check(bytes.equals(await readPhoto(tecnico)),'Fotografía privada íntegra después de reiniciar');
 for(let i=0;i<5;i++)await api('sesiones',null,{email:'otro@example.invalid',password:'incorrecta'},'POST',401);
 await stop();await start();await api('sesiones',null,{email:'otro@example.invalid',password},'POST',401);
 check(true,'Bloqueo por intentos fallidos conservado después de reiniciar');
 if(pruebaStorage){
  await stop();
  const key='sb_secret_'+randomBytes(32).toString('hex');
  // Reiniciar el doble con una clave que solo existe en esta prueba.
  await cloud.close();const {crearStorageSimulado}=await import('../STORAGE_2026_11_02/storage_simulado.mjs');cloud=await crearStorageSimulado(key);
  const configFile=path.join(profile,'storage-prueba.json');environment.AFRISAN_TEST_CONFIG_FILE=configFile;
  async function migrar(secret=key){const p=spawn('dotnet',[path.join(root,'.local/storage-tests/Migracion.Pruebas.dll')],{cwd:root,env:environment,windowsHide:true,stdio:['pipe','pipe','pipe']});p.stdout.on('data',x=>logs+=x);p.stderr.on('data',x=>logs+=x);p.stdin.end(secret+'\n');return new Promise(r=>p.once('exit',r));}
  check(await migrar('sb_publishable_no_es_clave_de_servidor')!==0,'Una clave pública no puede activar Storage privado');
  cloud.control.failUpload=true;check(await migrar()!==0,'Una caída de Storage impide activar la migración');
  check(!await fs.access(configFile).then(()=>true).catch(()=>false),'El fallo de copia no guarda el proveedor nuevo');
  cloud.control.failUpload=false;check(await migrar()===0,'Copias locales y prueba de privacidad verificadas antes de activar');
  const saved=JSON.parse(await fs.readFile(configFile,'utf8'));for(const [k,v]of Object.entries(saved))environment[k.replaceAll(':','__')]=v;
  const objectCount=cloud.objects.size;check(await migrar()===0&&cloud.objects.size===objectCount,'Repetir la migración no duplica ni sobrescribe evidencias');
  check(saved['Storage:Proveedor']==='Supabase','La migración activa el proveedor solo después de verificar');
  check([...cloud.objects.keys()].every(k=>!k.startsWith('diagnostico/')),'El objeto temporal de diagnóstico se elimina sin borrar evidencias');
  check(cloud.control.authLeaks===0,'La prueba anónima no envía la clave del servidor');
  const original=path.resolve(environment.Persistencia__FotosPath),backup=original+'-respaldo';const allowed=path.resolve(root,'.local')+path.sep;
  if(!original.startsWith(allowed)||!backup.startsWith(allowed))throw Error('Ruta de prueba fuera del workspace');
  await fs.rename(original,backup);await start();
  check(bytes.equals(await readPhoto(tecnico)),'Foto histórica leída desde la nube con originales locales retirados');
  await api(photoUrl,otro,undefined,'GET',403);
  cloud.control.public=true;await api(photoUrl,tecnico,undefined,'GET',503);cloud.control.public=false;
  cloud.control.corrupt=true;await api(photoUrl,tecnico,undefined,'GET',503);cloud.control.corrupt=false;
  check(true,'Bucket público y archivo alterado se rechazan sin servir la fotografía');
  const nueva=await api('ordenes-trabajo',autorizador,{...request,idOperacion:randomUUID(),descripcion:'Agregado en nube',cilindrosARetirar:[]});
  const retiroNube={idOperacion:randomUUID(),codigo:spare.codigo,pesoBrutoGr:4700,foto:photo(90)};
  cloud.control.failUpload=true;await api('ordenes-trabajo/'+nueva.id+'/entregas',tecnico,retiroNube,'POST',503);
  check((await api('ordenes-trabajo/'+nueva.id,tecnico)).entregas.length===0,'La caída de Storage no confirma despacho ni pesaje');
  cloud.control.failUpload=false;
  let nuevaOt=await api('ordenes-trabajo/'+nueva.id+'/entregas',tecnico,retiroNube);
  nuevaOt=await api('ordenes-trabajo/'+nueva.id+'/entregas',tecnico,retiroNube);
  check(nuevaOt.entregas.length===1,'Subida en nube y reintento no duplican el despacho');
  check((await fs.readdir(environment.Persistencia__FotosPath)).length===0,'Las nuevas fotos de nube no se guardan en disco');
  await stop();await start();check(bytes.equals(await readPhoto(tecnico)),'Storage conserva las fotografías después de otro reinicio');
 }
 await db.exec('RESET ROLE');
 const scalar=async sql=>Object.values((await db.query(sql)).rows[0])[0];
 check(await scalar("SELECT NOT rolsuper AND NOT rolcreatedb AND NOT rolcreaterole AND NOT rolreplication AND NOT rolbypassrls FROM pg_roles WHERE rolname='afrisan_api'"),'La identidad no es superusuario ni puede crear roles/bases o eludir RLS');
 check(!await scalar("SELECT has_table_privilege('afrisan_api','afrisan.\"Costos\"','SELECT')"),'Sin permisos sobre costos fuera de este incremento');
 check(!await scalar("SELECT has_table_privilege('afrisan_api','afrisan.\"Auditoria\"','DELETE') OR has_table_privilege('afrisan_api','afrisan.\"Auditoria\"','UPDATE')"),'La identidad no puede borrar ni editar auditoría');
 let inmutable=false;
 try {await db.transaction(async tx=>{await tx.exec('SET LOCAL ROLE afrisan_api');await tx.exec('UPDATE afrisan."Pesaje" SET "PesoBrutoGr"="PesoBrutoGr"+1');});}catch(error){inmutable=/no se modifica|no se.*elimina/i.test(error.message);}
 check(inmutable,'El permiso para bloquear Pesaje no permite reescribirlo: el trigger lo rechaza');
 const count=(await db.query("SELECT count(*)::int AS n FROM pg_tables WHERE schemaname='afrisan' AND NOT rowsecurity")).rows[0].n;
 check(count===0,'RLS permanece habilitado después de la actualización');
 report.estado='CORRECTO';
}catch(error){report.estado='FALLO';report.error=error.message;console.error(error.message);process.exitCode=1;}
finally{await stop();await socket.stop();await db.close();if(cloud)await cloud.close();report.cantidad_comprobaciones=checks.length;await fs.writeFile(pruebaStorage?path.join(directory,'../STORAGE_2026_11_02/resultados_storage.json'):pruebaBi?path.join(directory,'../POWER_BI_2026_11_02/resultados_powerbi.json'):path.join(directory,'resultados_api.json'),JSON.stringify(report,null,2));await fs.mkdir(path.join(root,'.local'),{recursive:true});await fs.writeFile(path.join(root,'.local/persistencia-pruebas.log'),logs);console.log('Resultado:',report.estado,'Comprobaciones:',checks.length);}
