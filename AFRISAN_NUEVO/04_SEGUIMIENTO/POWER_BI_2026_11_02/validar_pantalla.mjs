// Navegador Brave aislado. API interceptada con datos sintéticos: sin cuentas reales.
import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {spawn} from 'node:child_process';
import {randomUUID,randomInt} from 'node:crypto';
const dir=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(dir,'../../..');
const profile=path.join(root,'.local','powerbi-navegador-'+randomUUID());
const port=randomInt(9300,9900);
const browser=spawn('C:/Program Files/BraveSoftware/Brave-Browser/Application/brave.exe',[
 '--headless=new','--disable-gpu','--no-first-run','--no-default-browser-check',
 '--remote-debugging-address=127.0.0.1','--remote-debugging-port='+port,'--user-data-dir='+profile,'about:blank'
],{windowsHide:true,stdio:'ignore'});
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
const checks=[];let ws;let serial=0;const pending=new Map();
const check=(ok,nombre)=>{if(!ok)throw Error(nombre);checks.push(nombre);console.log('OK:',nombre);};
let report={estado:'FALLO',tipo:'Blazor real + API interceptada; sesión sintética local',comprobaciones:checks};
try{
 let targets;for(let i=0;i<100;i++){try{targets=await(await fetch(`http://127.0.0.1:${port}/json`)).json();if(targets.some(t=>t.type==='page'))break;}catch{}await sleep(100);}
 if(!targets?.some(t=>t.type==='page'))throw Error('Brave no inició');
 ws=new WebSocket(targets.find(t=>t.type==='page').webSocketDebuggerUrl);
 await new Promise((r,j)=>{ws.onopen=r;ws.onerror=j;});
 const call=(method,params={})=>new Promise((resolve,reject)=>{const id=++serial;const timeout=setTimeout(()=>{pending.delete(id);reject(Error('Tiempo agotado: '+method));},10000);pending.set(id,{resolve:r=>{clearTimeout(timeout);resolve(r);},reject:e=>{clearTimeout(timeout);reject(e);}});ws.send(JSON.stringify({id,method,params}));});
 const evalJs=async expression=>{const r=await call('Runtime.evaluate',{expression,returnByValue:true,awaitPromise:true});if(r.exceptionDetails)throw Error(r.exceptionDetails.text);return r.result.value;};
 let solicitudesResumen=0;const errors=[];
 ws.onmessage=async event=>{
  const m=JSON.parse(event.data);
  if(m.id){const p=pending.get(m.id);pending.delete(m.id);if(m.error)p?.reject(Error(m.error.message));else p?.resolve(m.result);return;}
  if(m.method==='Runtime.exceptionThrown')errors.push(m.params.exceptionDetails.text);
  if(m.method==='Fetch.requestPaused'){
   try{
    const {request,requestId}=m.params;const url=new URL(request.url);let body;
    if(request.method==='OPTIONS'){
     await call('Fetch.fulfillRequest',{requestId,responseCode:204,responseHeaders:[{name:'Access-Control-Allow-Origin',value:'http://localhost:5286'},{name:'Access-Control-Allow-Headers',value:'authorization,content-type'},{name:'Access-Control-Allow-Methods',value:'GET,OPTIONS'}]});return;
    }
    if(url.pathname.endsWith('/entorno'))body={instancia:'bi-ui',modo:'PostgreSQL',passwordDemo:'',cuentas:[]};
    else if(url.pathname.endsWith('/configuracion'))body={configurado:false,embedUrl:null,abrirUrl:null};
    else if(url.pathname.endsWith('/resumen')){
     solicitudesResumen++;
     body={generadoUtc:'2026-11-03T01:30:00Z',desde:'2026-10-04',hasta:'2026-11-02',cilindros:12,sinPesaje:2,ordenesAbiertas:3,ordenesCerradas:7,kgInstalados:24.6,retiradosPendientes:1,incidenciasAbiertas:2,
      inventarioPorEstado:[{nombre:'Disponible',valor:8},{nombre:'Instalado',valor:2},{nombre:'PendientePesaje',valor:2}],
      ordenesPorEstado:[{nombre:'Cerrada',valor:7},{nombre:'EnTerreno',valor:3}],instaladosPorRefrigerante:[{nombre:'R-134a',valor:24.6}],
      sucursales:[{id:1,nombre:'Sucursal Sur'}],refrigerantes:[{id:1,nombre:'R-134a'}],tecnicos:[{id:2,nombre:'Técnico de prueba'}]};
    } else if(url.pathname.endsWith('/paquete')){
     await call('Fetch.fulfillRequest',{requestId,responseCode:200,responseHeaders:[{name:'Content-Type',value:'application/zip'},{name:'Access-Control-Allow-Origin',value:'http://localhost:5286'}],body:'UEsFBgAAAAAAAAAAAAAAAAAAAAAAAA=='});return;
    } else throw Error('Solicitud no prevista: '+url.pathname);
    await call('Fetch.fulfillRequest',{requestId,responseCode:200,responseHeaders:[{name:'Content-Type',value:'application/json'},{name:'Access-Control-Allow-Origin',value:'http://localhost:5286'},{name:'Access-Control-Allow-Headers',value:'authorization,content-type'},{name:'Access-Control-Allow-Methods',value:'GET,OPTIONS'}],body:Buffer.from(JSON.stringify(body)).toString('base64')});
   }catch(e){errors.push(e.message);}
  }
 };
 await call('Page.enable');await call('Runtime.enable');
 await call('Fetch.enable',{patterns:[{urlPattern:'http://localhost:5181/api/v1/*',requestStage:'Request'}]});
 const inicial=await call('Page.addScriptToEvaluateOnNewDocument',{source:`sessionStorage.setItem('afrisan_nuevo.instancia','bi-ui');sessionStorage.setItem('afrisan_nuevo.sesion',JSON.stringify({Token:'solo-mock-ui',Vence:'2099-01-01T00:00:00Z',Nombre:'Revisión Power BI',Rol:'Administrador',Usuario:'prueba@example.invalid'}));`});
 await call('Emulation.setDeviceMetricsOverride',{width:1440,height:1100,deviceScaleFactor:1,mobile:false});
 await call('Page.navigate',{url:'http://localhost:5286/power-bi'});
 let loaded=false;for(let i=0;i<200;i++){if(await evalJs("document.querySelectorAll('.metric').length===5")){loaded=true;break;}await sleep(100);}
 check(loaded,'Pantalla Blazor carga los cinco indicadores con sesión sintética');
 check(await evalJs("document.body.innerText.includes('PENDIENTE DE PUBLICACIÓN') && !document.querySelector('iframe')"),'Sin informe no se carga iframe ni se finge conexión Microsoft');
 check(await evalJs("document.querySelectorAll('.bi-chart meter').length===6"),'Gráficos operativos renderizados');
 check(await evalJs("document.documentElement.scrollWidth <= innerWidth"),'Diseño de escritorio sin desbordamiento horizontal');
 let shot=await call('Page.captureScreenshot',{format:'png',captureBeyondViewport:false});await fs.writeFile(path.join(dir,'pantalla_escritorio.png'),Buffer.from(shot.data,'base64'));
 await evalJs("document.querySelector('#bi-sucursal').value='1'; document.querySelector('#bi-sucursal').dispatchEvent(new Event('change',{bubbles:true})); document.querySelectorAll('.form-actions button')[1].click()");
 for(let i=0;i<100&&solicitudesResumen<2;i++)await sleep(100);
 check(solicitudesResumen>=2,'Aplicar filtros vuelve a consultar los indicadores');
 for(let i=0;i<100;i++){if(await evalJs("!document.querySelector('.page-heading button').disabled"))break;await sleep(100);}
 await call('Browser.setDownloadBehavior',{behavior:'allow',downloadPath:path.join(profile,'descargas')});
 await evalJs("document.querySelector('.page-heading button').click()");
 let download=false;for(let i=0;i<100;i++){if(await evalJs("document.body.innerText.includes('Paquete descargado')")){download=true;break;}await sleep(100);}
 if(!download)throw Error('Descarga no completada: '+await evalJs("document.querySelector('[role=alert]')?.innerText || document.querySelector('.page-heading button').innerText"));
 check(download,'Botón de paquete usa descarga autenticada y puente JS de Blazor');
 await call('Emulation.setDeviceMetricsOverride',{width:390,height:844,deviceScaleFactor:1,mobile:true});await sleep(200);
 check(await evalJs("document.documentElement.scrollWidth <= innerWidth"),'Diseño móvil de 390 px sin desbordamiento horizontal');
 shot=await call('Page.captureScreenshot',{format:'png',captureBeyondViewport:false});await fs.writeFile(path.join(dir,'pantalla_movil.png'),Buffer.from(shot.data,'base64'));
 await evalJs("const s=JSON.parse(sessionStorage.getItem('afrisan_nuevo.sesion'));s.Rol='Bodega';sessionStorage.setItem('afrisan_nuevo.sesion',JSON.stringify(s));");
 await call('Page.removeScriptToEvaluateOnNewDocument',{identifier:inicial.identifier});
 await call('Page.reload');let restringido=false;
 for(let i=0;i<100;i++){if(await evalJs("document.body.innerText.includes('Acceso restringido')")){restringido=true;break;}await sleep(100);}
 check(restringido&&await evalJs("!document.querySelector('nav a[href=\"power-bi\"]')"),'Bodega no ve el menú y la ruta directa está restringida');
 check(errors.length===0,'Sin excepciones JavaScript durante carga, filtros y descarga');
 report.estado='CORRECTO';report.cantidad_comprobaciones=checks.length;
}catch(error){report.error=error.message;console.error(error.message);process.exitCode=1;}
finally{await fs.writeFile(path.join(dir,'resultados_pantalla.json'),JSON.stringify(report,null,2));if(ws?.readyState===WebSocket.OPEN){ws.send(JSON.stringify({id:++serial,method:'Browser.close'}));await sleep(200);ws.close();}browser.kill();console.log('Resultado UI:',report.estado);}
