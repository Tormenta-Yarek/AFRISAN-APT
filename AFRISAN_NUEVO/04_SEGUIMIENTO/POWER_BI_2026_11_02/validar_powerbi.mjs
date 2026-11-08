// Extensión de la prueba de API real: solo PostgreSQL embebido y cuentas sintéticas.
import {inflateRawSync} from 'node:zlib';
import fs from 'node:fs/promises';
import {randomUUID} from 'node:crypto';
let incidenciaBi;
function csv(text){
 const rows=[];let row=[],value='',quoted=false;
 for(let i=0;i<text.length;i++){
  const ch=text[i];
  if(ch==='"'){if(quoted&&text[i+1]==='"'){value+='"';i++;}else quoted=!quoted;}
  else if(!quoted&&ch===','){row.push(value);value='';}
  else if(!quoted&&(ch==='\n'||ch==='\r')){if(ch==='\r'&&text[i+1]==='\n')i++;row.push(value);if(row.some(Boolean))rows.push(row);row=[];value='';}
  else value+=ch;
 }
 if(value||row.length){row.push(value);rows.push(row);}
 const [head,...body]=rows;return body.map(r=>Object.fromEntries(head.map((k,i)=>[k,r[i]])));
}

function unzip(bytes){
 const result={};let end=bytes.length-22;
 while(end>=0&&bytes.readUInt32LE(end)!==0x06054b50)end--;
 if(end<0)throw Error('ZIP sin directorio');
 let at=bytes.readUInt32LE(end+16);const entries=bytes.readUInt16LE(end+10);
 for(let i=0;i<entries;i++){
  if(bytes.readUInt32LE(at)!==0x02014b50)throw Error('Directorio ZIP inválido');
  const method=bytes.readUInt16LE(at+10),size=bytes.readUInt32LE(at+20),n=bytes.readUInt16LE(at+28),extra=bytes.readUInt16LE(at+30),comment=bytes.readUInt16LE(at+32),offset=bytes.readUInt32LE(at+42);
  const name=bytes.subarray(at+46,at+46+n).toString('utf8');
  const start=offset+30+bytes.readUInt16LE(offset+26)+bytes.readUInt16LE(offset+28);
  const data=bytes.subarray(start,start+size);
  result[name]=(method===8?inflateRawSync(data):data).toString('utf8').replace(/^\uFEFF/,'');at+=46+n+extra+comment;
 }
 return result;
}

export async function validarBi(etapa,c){
 const {api,check,admin,bodega,autorizador,tecnico,jefatura,base,db,environment,start,stop,child}=c;
 const resumen=()=>api('power-bi/resumen',admin);
 const getZip=async()=>{
  const response=await fetch(base+'power-bi/paquete',{headers:{Authorization:'Bearer '+admin}});
  check(response.ok&&response.headers.get('content-type')==='application/zip'&&response.headers.get('cache-control')==='no-store','Paquete privado descargado sin caché');
  return unzip(Buffer.from(await response.arrayBuffer()));
 };
 if(etapa==='vacio'){
  for(const ruta of ['configuracion','resumen','paquete']){
   await api('power-bi/'+ruta,null,undefined,'GET',401);
   for(const token of [bodega,autorizador,tecnico])await api('power-bi/'+ruta,token,undefined,'GET',403);
  }
  const config=await api('power-bi/configuracion',jefatura);
  check(!config.configurado&&config.embedUrl===null,'Sin informe publicado no se simula configuración');
  const empty=await resumen();check(empty.cilindros===0&&empty.kgInstalados===0&&empty.ordenesAbiertas===0,'Base vacía produce indicadores vacíos reales');
  check((await api('power-bi/resumen',jefatura)).cilindros===0,'Jefatura tiene acceso al resumen');
  const files=await getZip();const manifest=JSON.parse(files['Manifiesto.json']);
  check(Object.values(manifest.filas).every(n=>n===0)&&files['Inventario.csv'].split(/\r?\n/).filter(Boolean).length===1,'CSV vacío conserva cabeceras importables');
  await api('power-bi/resumen?desde=2026-11-03&hasta=2026-11-02',admin,undefined,'GET',400);
  await api('power-bi/resumen?sucursalId=-1',admin,undefined,'GET',400);
  await api('power-bi/resumen?desde=no-es-fecha',admin,undefined,'GET',400);
 }
 if(etapa==='borrador'){
  const r=await resumen();check(r.cilindros===3&&r.ordenesAbiertas===1&&r.kgInstalados===0&&r.retiradosPendientes===0,'Borradores y despachos no se cuentan como instalaciones');
 }
 if(etapa==='confirmado'){
  const r=await resumen();check(r.kgInstalados===4.6&&r.retiradosPendientes===1,'Kg netos confirmados y retiro pendiente contados una sola vez');
  check(r.instaladosPorRefrigerante.length===1&&r.instaladosPorRefrigerante[0].valor===4.6,'Agrupación por refrigerante conserva kilos sin multiplicar detalles');
  const suc=r.sucursales[0].id,gas=r.refrigerantes[0].id,tec=r.tecnicos[0].id;
  check((await api(`power-bi/resumen?sucursalId=${suc}&refrigeranteId=${gas}&tecnicoId=${tec}`,admin)).kgInstalados===4.6,'Filtros de sucursal, refrigerante y técnico combinados');
  const wrong=await api('power-bi/resumen?sucursalId=2147483647',admin);
  check(wrong.kgInstalados===0&&wrong.ordenesAbiertas===0,'Sucursal ajena al conjunto no mezcla datos');
  const past=await api('power-bi/resumen?desde=2000-01-01&hasta=2000-01-31',admin);
  check(past.cilindros===3&&past.ordenesAbiertas===0&&past.kgInstalados===0,'Inventario es actual y no finge un histórico por fecha');
  const ot=(await api('ordenes-trabajo',autorizador))[0];const id=randomUUID();
  incidenciaBi={ot:ot.id,id};
  await api('ordenes-trabajo/'+ot.id+'/incidencias',tecnico,{id,tipo:'Incidencia',motivo:'Texto sensible que no debe exportarse'});
  check((await resumen()).incidenciasAbiertas===1,'Incidencia pendiente aparece en el indicador');
  const files=await getZip();check(!JSON.stringify(files).includes('Texto sensible'),'Paquete omite motivos libres de incidencias');
 }
 if(etapa==='bodega'){
  await api('ordenes-trabajo/'+incidenciaBi.ot+'/incidencias/'+incidenciaBi.id+'/resolver',autorizador,{respuesta:'Revisión de prueba completada'});
  check((await resumen()).incidenciasAbiertas===0,'Revisión de incidencia actualiza el indicador');
 }
 if(etapa==='cerrado'){
  const r=await resumen();check(r.ordenesAbiertas===0&&r.ordenesCerradas===1&&r.retiradosPendientes===0&&r.kgInstalados===4.6,'Recepción y cierre cambian pendientes sin duplicar kilos');
  const auditoriaAntes=(await db.query('SELECT count(*)::int n FROM afrisan."Auditoria"')).rows[0].n;
  const files=await getZip();const manifest=JSON.parse(files['Manifiesto.json']);
  check(manifest.filas.Ordenes===1&&manifest.filas.DetalleOT===1&&manifest.filas.Operaciones===1&&manifest.filas.Despachos===2,'Paquete conserva granos separados de OT, detalle, operación y despacho');
  const opCsv=csv(files['Operaciones.csv'])[0];
  check(Number(opCsv.KgInstalados)===4.6&&Number(opCsv.KgRecepcion)===0.75&&opCsv.PendienteRecepcion==='false','CSV mantiene pesajes de instalación y recepción diferenciados');
  check(['Afrisan.pq','Medidas.dax','Tema_AFRISAN.json','LEEME.md'].every(k=>files[k]?.length>0),'Paquete incluye Power Query, DAX, tema y guía');
  check(files['Afrisan.pq'].includes('Table.ReplaceValue')&&JSON.parse(files['Tema_AFRISAN.json']).name==='AFRISAN operativo','Plantilla contempla nulos y tema JSON válido');
  check(!/sb_secret_|sb_publishable_|Clave_Usuario|Correo_Usuario|SHA256|BucketPrivado|RutaObjeto|Password=|Bearer /i.test(JSON.stringify(files)),'Exportación no contiene credenciales, correos ni referencias privadas de fotos');
  const auditoriaDespues=(await db.query('SELECT count(*)::int n FROM afrisan."Auditoria"')).rows[0].n;
  check(auditoriaAntes===auditoriaDespues,'Consultas BI no escriben hechos ni auditoría');
  await fs.writeFile(new URL('./paquete_muestra.json',import.meta.url),JSON.stringify({archivos:Object.keys(files),manifiesto:manifest},null,2));
  await stop();environment.PowerBi__ReportId='11111111-2222-3333-4444-555555555555';environment.PowerBi__TenantId='66666666-7777-8888-9999-aaaaaaaaaaaa';await start();
  const config=await api('power-bi/configuracion',admin);const url=new URL(config.embedUrl);
  check(config.configurado&&url.origin==='https://app.powerbi.com'&&url.pathname==='/reportEmbed'&&url.searchParams.get('reportId')===environment.PowerBi__ReportId&&url.searchParams.get('ctid')===environment.PowerBi__TenantId,'Inserción segura construida solo desde GUID y dominio Microsoft fijo');
  check((await resumen()).kgInstalados===4.6,'Indicadores persisten después de reiniciar');
  environment.PowerBi__ReportId='https://app.powerbi.com/view?r=publico';
  const invalid=child();invalid.stdin.end();
  const exit=await new Promise(resolve=>invalid.once('exit',resolve));
  check(exit!==0,'Se rechaza URL pública o arbitraria como identificador de informe');
  delete environment.PowerBi__ReportId;delete environment.PowerBi__TenantId;
 }
}
