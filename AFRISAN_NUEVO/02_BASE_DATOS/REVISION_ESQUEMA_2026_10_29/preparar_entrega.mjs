// Genera la corrección desde el instalador vigente; no se conecta a una base de datos.
import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {createHash} from 'node:crypto';

const directory=path.dirname(fileURLToPath(import.meta.url));
const canonicalPath=path.resolve(directory,'../04_Crear_Afrisan_Contexto_Actual.sql');
const sql=await fs.readFile(canonicalPath,'utf8');
const original=await fs.readFile(path.join(directory,'00_Esquema_Original_Recibido.sql'));
const header=`-- AFRISAN | CORRECCIÓN DEL ESQUEMA RECIBIDO | 29/10/2026
-- Sustituye el modelo de cargas a equipos por el reemplazo físico de cilindros.
-- Derivado de 04_Crear_Afrisan_Contexto_Actual.sql, conservando su contenido completo.
-- INSTALACIÓN NUEVA AISLADA: no es una migración de los datos de afrisan.
-- Ejecutar solo 01_AFRISAN_Corregida.sql completo, sin el original ni otros instaladores.
-- Crea afrisan_actual y afrisan_bi_actual; se detiene si ya existen.
-- Sin usuarios, roles ni ejemplos precargados. Ver README.md antes de instalar.

`;
const corrected=header+sql;
await fs.writeFile(path.join(directory,'01_AFRISAN_Corregida.sql'),corrected,'utf8');
const hash=value=>createHash('sha256').update(value).digest('hex');
await fs.writeFile(path.join(directory,'manifiesto.json'),JSON.stringify({
  fecha_revision:'2026-10-29',
  original:{archivo:'00_Esquema_Original_Recibido.sql',sha256:hash(original)},
  fuente:{archivo:path.basename(canonicalPath),sha256:hash(sql)},
  entrega:{archivo:'01_AFRISAN_Corregida.sql',sha256:hash(corrected)},
  nota:'La entrega conserva íntegro el instalador vigente, precedido por comentarios de identificación. No ejecuta ni migra bases existentes.'
},null,2)+'\n','utf8');
console.log('Entrega SQL y manifiesto generados.');
