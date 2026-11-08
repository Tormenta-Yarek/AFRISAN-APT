// Solo bases embebidas de prueba: no conecta a Supabase ni lee credenciales.
import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { createHash } from 'node:crypto';
const directory = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(directory, '../../..');
const sqlDir = path.dirname(directory);
const modulePath = process.env.PGLITE_MODULE_PATH || path.join(root, '.local/validacion_sql_contexto/node_modules/@electric-sql/pglite/dist/index.js');
const { PGlite } = await import(pathToFileURL(modulePath).href);
const resetPath = path.join(sqlDir, '06_Reemplazar_Esquemas_Por_Afrisan.sql');
const resetSql = await fs.readFile(resetPath, 'utf8');
const checks = [];
function check(ok, name) {
  checks.push({ resultado: ok ? 'OK' : 'FALLO', comprobacion: name });
  if (!ok) throw Error(name);
  console.log('OK:', name);
}
async function scalar(db, sql) { return Object.values((await db.query(sql)).rows[0])[0]; }
const report = { fecha_utc: new Date().toISOString(), sha256_sql: createHash('sha256').update(resetSql).digest('hex'), comprobaciones: checks };
const db = new PGlite();
try {
  // Probar el reemplazo sobre la instalación anterior real y datos ficticios.
  await db.exec(await fs.readFile(path.join(sqlDir, '04_Crear_Afrisan_Contexto_Actual.sql'), 'utf8'));
  for (const schema of ['afrisan', 'afrisan_bi', 'auth', 'storage']) await db.exec(`CREATE SCHEMA ${schema}`);
  for (const schema of ['afrisan', 'afrisan_bi', 'afrisan_actual', 'afrisan_bi_actual', 'public', 'auth', 'storage'])
    await db.exec(`CREATE TABLE ${schema}."DatoPrueba"(id integer); INSERT INTO ${schema}."DatoPrueba" VALUES (42)`);
  await db.exec(resetSql);
  check(Number(await scalar(db, "SELECT count(*) FROM pg_tables WHERE schemaname='afrisan'")) === 30, 'Reemplazo crea 30 tablas en afrisan');
  check(Number(await scalar(db, "SELECT count(*) FROM pg_views WHERE schemaname='afrisan'")) === 8, 'Reemplazo crea ocho vistas en afrisan');
  check(Number(await scalar(db, "SELECT count(*) FROM pg_namespace WHERE nspname IN ('afrisan_actual','afrisan_bi','afrisan_bi_actual')")) === 0, 'Se retiran los otros tres esquemas AFRISAN');
  check(await scalar(db, `SELECT to_regclass('afrisan."DatoPrueba"') IS NULL`), 'Se borra la base afrisan anterior autorizada');
  for (const schema of ['public', 'auth', 'storage'])
    check(Number(await scalar(db, `SELECT id FROM ${schema}."DatoPrueba"`)) === 42, `Se conserva el dato ajeno en ${schema}`);
  for (const { tablename } of (await db.query("SELECT tablename FROM pg_tables WHERE schemaname='afrisan'")).rows)
    check(Number(await scalar(db, `SELECT count(*) FROM afrisan."${tablename}"`)) === 0, `Instalación vacía: ${tablename}`);
  check(Number(await scalar(db, "SELECT count(*) FROM information_schema.columns WHERE table_schema='afrisan' AND (column_name ILIKE '%gwp%' OR column_name ILIKE '%co2%' OR column_name='GrEmitidos')")) === 0, 'Reemplazo elimina las columnas ambientales');
  check(await scalar(db, `SELECT to_regclass('afrisan."EstimacionEmisiones"') IS NULL AND to_regclass('afrisan."EmisionesEstimadas"') IS NULL`), 'Reemplazo elimina tabla y vista ambientales');
  // Un fallo posterior a los DROP debe restaurar los esquemas y sus datos.
  const rollbackDb = new PGlite();
  try {
    await rollbackDb.exec('CREATE SCHEMA afrisan; CREATE TABLE afrisan."DatoPrueba"(id integer); INSERT INTO afrisan."DatoPrueba" VALUES(73)');
    let failed = false;
    try { await rollbackDb.exec(resetSql.replace('\nCOMMIT;', '\nSELECT 1 / 0;\nCOMMIT;')); }
    catch { failed = true; await rollbackDb.exec('ROLLBACK'); }
    check(failed && Number(await scalar(rollbackDb, 'SELECT id FROM afrisan."DatoPrueba"')) === 73, 'Un fallo revierte también la eliminación de esquemas');
  } finally { await rollbackDb.close(); }
  report.estado = 'CORRECTO';
} catch (error) { report.estado = 'FALLO'; report.error = error.message; process.exitCode = 1; }
finally {
  report.cantidad_comprobaciones = checks.length;
  await fs.writeFile(path.join(directory, 'resultados_reemplazo.json'), JSON.stringify(report, null, 2));
  await db.close();
}
if (process.exitCode) throw Error(report.error);
process.env.AFRISAN_SCHEMA = 'afrisan';
process.env.AFRISAN_SQL_PATH = path.join(sqlDir, '05_Crear_Afrisan_Sin_Emisiones.sql');
process.env.AFRISAN_REPORT_PATH = path.join(directory, 'resultados_flujo.json');
await import('../../04_SEGUIMIENTO/VALIDACION_SQL_CONTEXTO_2026_10_25/validar_sql.mjs');
