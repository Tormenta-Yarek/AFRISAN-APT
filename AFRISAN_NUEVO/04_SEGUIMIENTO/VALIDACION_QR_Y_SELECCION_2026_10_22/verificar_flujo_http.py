import json,uuid,base64,io,urllib.request,urllib.error
from pathlib import Path
from PIL import Image
BASE='http://localhost:5181/api/v1/'
out=Path('.local/verificacion_qr');out.mkdir(parents=True,exist_ok=True)
results=[]
def request(method,path,data=None,role='Bodega',status=200):
 body=json.dumps(data).encode() if data is not None else None
 headers={'Content-Type':'application/json'}
 if role:headers['Authorization']='Bearer '+tokens[role]
 req=urllib.request.Request(BASE+path,body,headers,method=method)
 try:
  with urllib.request.urlopen(req) as res:code=res.status;raw=res.read()
 except urllib.error.HTTPError as ex:code=ex.code;raw=ex.read()
 assert code==status,(path,code,status,raw.decode(errors='replace')[:350])
 if not raw:return None
 return json.loads(raw)
def check(ok,label):
 assert ok,label;results.append(label);print('OK:',label)
tokens={}
for role in ['Bodega','Autorizador','Tecnico','Jefatura']:
 tokens[role]=request('POST','sesiones',{'email':role.lower()+'@afrisan.example','password':'DemoAfrisan2026!'},None)['token']
inst=request('GET','entorno',role=None)['instancia']
gas=request('POST','refrigerantes',{'codigo':'PRUEBA-QR-'+uuid.uuid4().hex[:6],'nombre':'Datos simulados para verificar el flujo'},status=201)['id']
def cylinder(location='Bodega',branch=None):
 return request('POST','cilindros',{'refrigeranteId':gas,'capacidadGr':10000,'taraGr':1000,'pesoInicialGr':7000,'ubicacionInicial':location,'sucursalInicial':branch},status=201)
old=cylinder('Sucursal','Sucursal de prueba QR');new=cylinder();extra=cylinder();other=cylinder('Sucursal','Otra sucursal de prueba')
def ot(retire=[],branch='Sucursal de prueba QR'):
 return request('POST','ordenes-trabajo',{'idOperacion':str(uuid.uuid4()),'sucursal':branch,'direccion':'Dirección simulada','descripcion':'Verificación de reemplazo','tecnicoEmail':'tecnico@afrisan.example','detalle':[{'refrigeranteId':gas,'grSolicitados':6000,'grAutorizados':6000}],'cilindrosARetirar':retire},'Autorizador',201)
try:
 request('POST','ordenes-trabajo',{'idOperacion':str(uuid.uuid4()),'sucursal':'Sucursal de prueba QR','direccion':'Prueba','descripcion':'Prueba','tecnicoEmail':'tecnico@afrisan.example','detalle':[{'refrigeranteId':gas,'grSolicitados':1,'grAutorizados':1}],'cilindrosARetirar':[new['codigo']]},'Autorizador',409)
 check(True,'API rechaza cilindro de Bodega como retiro de sucursal')
 a=ot([old['codigo']]);root='ordenes-trabajo/'+a['id']
 request('POST',root+'/entregas',{'codigo':new['codigo']},'Tecnico',403);check(True,'Técnico no puede hacer entrega de Bodega')
 request('POST',root+'/entregas',{'codigo':new['codigo']});check(True,'Entrega de Bodega registrada')
 pngs=[]
 for color in ['red','blue']:
  buf=io.BytesIO();Image.new('RGB',(5,5),color).save(buf,format='PNG');pngs.append(base64.b64encode(buf.getvalue()).decode())
 def saveop(root,oldcode,newcode,kind='Reemplazo'):
  return request('PUT',root+'/operaciones',{'id':str(uuid.uuid4()),'instancia':inst,'version':0,'tipo':kind,'codigoRetirado':oldcode,'codigoInstalado':newcode,'pesoRetiradoGr':2000 if oldcode else None,'pesoInstaladoGr':7000},'Tecnico')
 def photos_confirm(root,op):
  ids=[]
  for idx,group in enumerate(['Retirado','Instalado'] if op['tipo']=='Reemplazo' else ['Instalado']):
   fid=str(uuid.uuid4());ids.append(fid)
   op=request('POST',root+'/operaciones/'+op['id']+'/fotos',{'id':fid,'version':op['version'],'grupo':group,'nombre':'prueba.png','contentType':'image/png','base64':pngs[idx]},'Tecnico')
  return request('POST',root+'/operaciones/'+op['id']+'/confirmar?version='+str(op['version']),role='Tecnico')
 op=saveop(root,old['codigo'],new['codigo']);oproot=root+'/operaciones/'+op['id']
 request('POST',oproot+'/confirmar?version=1',role='Tecnico',status=400);check(True,'Confirmación sin fotos rechazada')
 fid=str(uuid.uuid4());op=request('POST',oproot+'/fotos',{'id':fid,'version':1,'grupo':'Retirado','nombre':'prueba.png','contentType':'image/png','base64':pngs[0]},'Tecnico')
 request('DELETE',oproot+'/fotos/'+fid+'?version=1',role='Tecnico',status=409);check(True,'Borrado de foto con versión antigua rechazado')
 request('POST',oproot+'/confirmar?version=1',role='Tecnico',status=409);check(True,'Confirmación con versión antigua rechazada')
 op=request('POST',oproot+'/fotos',{'id':str(uuid.uuid4()),'version':op['version'],'grupo':'Instalado','nombre':'prueba.png','contentType':'image/png','base64':pngs[1]},'Tecnico')
 op=request('POST',oproot+'/confirmar?version='+str(op['version']),role='Tecnico');check(op['estado']=='Registrada','Reemplazo con sus fotos confirmado')
 check(request('GET','cilindros/'+new['codigo'])['ubicacion']=='Sucursal','Reemplazo instalado en sucursal')
 check(request('GET','cilindros/'+old['codigo'])['ubicacion']=='Tecnico','Retirado no suma Bodega antes de recepción')
 draft=request('PUT',root+'/operaciones',{'id':str(uuid.uuid4()),'instancia':inst,'tipo':'Agregado','version':0},'Tecnico')
 request('POST',root+'/operaciones/'+draft['id']+'/descartar?version='+str(draft['version']),role='Tecnico');check(True,'Borrador extra descartado con historial')
 request('PUT',root+'/operaciones',{'id':draft['id'],'instancia':inst,'tipo':'Agregado','version':draft['version']},'Tecnico',409);check(True,'Un borrador descartado no se vuelve a guardar ni resucita')
 request('POST',root+'/recepciones',{'codigo':old['codigo'],'pesoBrutoGr':2000});request('POST',root+'/termino-tecnico',{},'Tecnico');check(True,'Recepción y término permiten ignorar borrador descartado')
 # Una segunda OT retira el instalado mientras la primera sigue en revisión.
 second=ot([new['codigo']]);r2='ordenes-trabajo/'+second['id'];request('POST',r2+'/entregas',{'codigo':extra['codigo']})
 photos_confirm(r2,saveop(r2,new['codigo'],extra['codigo']))
 closed=request('POST',root+'/cerrar',role='Autorizador');check(closed['estado']=='Cerrada','OT anterior cierra aunque su instalado esté ahora retirado por otra OT')
 # Devolución sin uso y nueva salida a otra OT del mismo técnico.
 spare=cylinder();a2=ot();ra='ordenes-trabajo/'+a2['id'];request('POST',ra+'/entregas',{'codigo':spare['codigo']});request('POST',ra+'/recepciones',{'codigo':spare['codigo'],'pesoBrutoGr':7000})
 request('POST',ra+'/entregas',{'codigo':spare['codigo']});check(request('GET','cilindros/'+spare['codigo'])['ubicacion']=='Tecnico','Puede volver a entregar a la misma OT un envase devuelto sin uso')
 request('POST',ra+'/recepciones',{'codigo':spare['codigo'],'pesoBrutoGr':7000});b2=ot();rb='ordenes-trabajo/'+b2['id'];request('POST',rb+'/entregas',{'codigo':spare['codigo']})
 request('POST',ra+'/recepciones',{'codigo':spare['codigo'],'pesoBrutoGr':7000},status=409);check(True,'La OT anterior no puede recibir un envase entregado por otra OT')
 request('POST',ra+'/entregas',{'codigo':extra['codigo']},status=409);check(True,'Un cilindro instalado no está disponible para entrega')
 with urllib.request.urlopen(urllib.request.Request(BASE+'cilindros/'+new['codigo']+'/qr',headers={'Authorization':'Bearer '+tokens['Autorizador']})) as res:(out/'qr-real.png').write_bytes(res.read())
 Image.new('RGB',(100,100),'white').save(out/'sin-qr.png')
 (out/'datos_ui.json').write_text(json.dumps({'gasId':gas,'codigo':new['codigo'],'otraSucursal':other['codigo'],'sucursal':'Otra sucursal de prueba','rol':'Autorizador','otTerminada':a['id']},ensure_ascii=False),encoding='utf-8')
 check(True,'QR PNG de la API descargado para probar su lectura')
finally:
 (out/'resultados.json').write_text(json.dumps({'fecha':'2026-10-22','tipo':'Regresión HTTP local con datos simulados','casos_correctos':results},ensure_ascii=False,indent=2),encoding='utf-8')
