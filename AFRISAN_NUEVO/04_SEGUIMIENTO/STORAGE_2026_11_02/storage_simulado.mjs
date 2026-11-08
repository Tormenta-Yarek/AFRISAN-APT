import http from 'node:http';
export async function crearStorageSimulado(key){
 const objects=new Map();const control={exists:false,public:false,failUpload:false,corrupt:false,authLeaks:0};
 const server=http.createServer(async(req,res)=>{
  const path=new URL(req.url,'http://localhost').pathname;
  const send=(status,value)=>{res.writeHead(status,{'Content-Type':'application/json'});res.end(JSON.stringify(value));};
  if(path.startsWith('/storage/v1/object/public/')){if(req.headers.apikey||req.headers.authorization)control.authLeaks++;return send(control.public?200:404,{error:'Not found'});}
  if(req.headers.apikey!==key||req.headers.authorization)return send(401,{error:'Unauthorized'});
  const body=[];for await(const part of req)body.push(part);const bytes=Buffer.concat(body);
  if(path==='/storage/v1/bucket/afrisan-evidencias'&&req.method==='GET')return send(control.exists?200:404,{id:'afrisan-evidencias',public:control.public,file_size_limit:4194304,allowed_mime_types:['image/jpeg','image/png','image/webp']});
  if(path==='/storage/v1/bucket'&&req.method==='POST'){const b=JSON.parse(bytes.toString());if(b.public||b.id!=='afrisan-evidencias'||b.file_size_limit!==4194304)return send(400,{error:'Invalid bucket'});control.exists=true;return send(200,b);}
  if(path.startsWith('/storage/v1/object/authenticated/afrisan-evidencias/')&&req.method==='GET'){
   const name=decodeURIComponent(path.slice('/storage/v1/object/authenticated/afrisan-evidencias/'.length));const entry=objects.get(name);if(!entry)return send(404,{error:'Not found'});
   const output=Buffer.from(entry.bytes);if(control.corrupt)output[output.length-1]^=1;res.writeHead(200,{'Content-Type':entry.mime,'Content-Length':output.length});return res.end(output);
  }
  if(path.startsWith('/storage/v1/object/afrisan-evidencias/')&&req.method==='POST'){
   if(control.failUpload)return send(503,{error:'Storage unavailable'});
   if(req.headers['x-upsert']!=='false')return send(400,{error:'Overwrites forbidden'});
   const name=decodeURIComponent(path.slice('/storage/v1/object/afrisan-evidencias/'.length));if(objects.has(name))return send(400,{error:'Duplicate'});
   objects.set(name,{bytes,mime:req.headers['content-type']});return send(200,{Key:'afrisan-evidencias/'+name});
  }
  if(path==='/storage/v1/object/afrisan-evidencias'&&req.method==='DELETE'){
   const {prefixes}=JSON.parse(bytes.toString());if(prefixes.some(x=>!x.startsWith('diagnostico/')))return send(403,{error:'Evidence deletion forbidden'});
   for(const p of prefixes)objects.delete(p);return send(200,[]);
  }
  return send(404,{error:'Not found'});
 });
 await new Promise((resolve,reject)=>{server.once('error',reject);server.listen(55433,'127.0.0.1',resolve);});
 return {objects,control,close:()=>new Promise(resolve=>server.close(resolve))};
}
