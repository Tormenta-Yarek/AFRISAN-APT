"""Empaqueta los componentes en una instalación SQL única y transaccional."""
from pathlib import Path

root = Path(__file__).resolve().parent
parts = []
for name in ("01_operacion.sql", "02_soporte.sql", "03_auditoria_y_acceso.sql"):
    lines = (root / name).read_text(encoding="utf-8-sig").splitlines()
    parts.append("-- COMPONENTE: " + name + "\n" + "\n".join(line for line in lines if line.strip() not in ("BEGIN;", "COMMIT;")))
header = """-- AFRISAN | ESQUEMA ÚNICO SIN EMISIONES | 02/11/2026
-- INSTALACIÓN VACÍA. Ejecutar únicamente este archivo completo.
-- Crea solo afrisan, con tablas y vistas operativas. No altera otros esquemas.
-- Se detiene si afrisan ya existe. No instala cuentas ni ejemplos.
-- La API actual sigue en memoria: este SQL no conecta ni adapta automáticamente EF Core.
-- Fotos: metadatos de objetos privados; la API verifica el archivo real en Storage.
-- Gramos enteros, fechas timestamptz, precios NUMERIC. Sin módulo de emisiones.
-- La medición de límites se declara con criterio explícito; no inventa su fórmula.
-- Requiere PostgreSQL 15+; ver README y validación local antes de usar en Supabase.
BEGIN;
"""
body = "\n\n".join(parts)
target = root.parent / "05_Crear_Afrisan_Sin_Emisiones.sql"
target.write_text(header + body + "\nCOMMIT;\n", encoding="utf-8")
print(target)

# Reemplazo solicitado expresamente por Dante el 02/11/2026: base vacía.
# No se ejecuta desde este generador; solo produce el archivo revisable.
replacement = """-- AFRISAN | REEMPLAZO COMPLETO AUTORIZADO | 02/11/2026
-- ATENCIÓN: borra TODOS los datos y objetos de los cuatro esquemas indicados.
-- Ejecutar este archivo completo, UNA SOLA VEZ, en SQL Editor de Supabase.
-- No ejecutar además el archivo 05. No ejecutarlo contra datos a conservar.
-- Los cambios se confirman juntos: eliminación y creación en una transacción.
-- public, auth y storage no son destinos de eliminación.
BEGIN;
SET LOCAL lock_timeout = '10s';
DROP SCHEMA IF EXISTS afrisan_bi_actual CASCADE;
DROP SCHEMA IF EXISTS afrisan_bi CASCADE;
DROP SCHEMA IF EXISTS afrisan_actual CASCADE;
DROP SCHEMA IF EXISTS afrisan CASCADE;
"""
target = root.parent / "06_Reemplazar_Esquemas_Por_Afrisan.sql"
target.write_text(replacement + body + "\nCOMMIT;\n", encoding="utf-8")
print(target)
