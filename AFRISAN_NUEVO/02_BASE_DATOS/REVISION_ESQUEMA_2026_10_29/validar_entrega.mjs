// Ejecuta las regresiones existentes contra el archivo exacto que se entrega.
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const directory=path.dirname(fileURLToPath(import.meta.url));
process.env.AFRISAN_SQL_PATH=path.join(directory,'01_AFRISAN_Corregida.sql');
process.env.AFRISAN_REPORT_PATH=path.join(directory,'resultados_validacion.json');
await import('../../04_SEGUIMIENTO/VALIDACION_SQL_CONTEXTO_2026_10_25/validar_sql.mjs');
