# Validación de Storage privado

Fecha: **02/11/2026**. API .NET 8 + PostgreSQL embebido PGlite 0.5.11 y doble
HTTP local del servicio Storage. No usa Supabase, credenciales ni User Secrets
reales. El arnés de migración inyecta un escritor de configuración aislado.

Resultado: **55 comprobaciones correctas, cero fallos**. Compilación sin errores
ni advertencias. Incluye rechazo de claves públicas y repetición de migración
sin duplicar o sobrescribir evidencias.

`resultados_storage.json` conserva las comprobaciones de persistencia y añade:

- Caída de Storage durante la copia: no se activa ni guarda el proveedor.
- Copia de fotos históricas y verificación por descarga antes de activar.
- Prueba de acceso público sin enviar la clave del servidor.
- Eliminación exclusiva del objeto temporal de diagnóstico.
- Lectura de fotos antiguas desde la nube después de retirar los originales
  locales a una carpeta de respaldo dentro del directorio de pruebas.
- Acceso de un técnico ajeno rechazado después del cambio a nube.
- Bucket público y contenido con hash alterado rechazados.
- Fallo de carga: no se confirma despacho/pesaje.
- Reintentos sin duplicar, nuevas fotos sin copia local y conservación tras
  reiniciar la API.

Para repetir, compilar `Migracion.Pruebas/Migracion.Pruebas.csproj` en
`.local/storage-tests` y configurar:

```powershell
$env:AFRISAN_API_DLL='D:\Afrisan\.local\storage-tests\Afrisan.Nuevo.Api.dll'
$env:AFRISAN_TEST_STORAGE='1'
node AFRISAN_NUEVO/04_SEGUIMIENTO/PERSISTENCIA_2026_11_02/validar_api.mjs
```

El mock sirve en 127.0.0.1:55433 y PostgreSQL en 127.0.0.1:55432. Solo se admite
HTTP sin TLS en Development con pruebas locales explícitas. El arnés rechaza
conexiones de base que no sean el loopback y puerto de pruebas.
## Verificación remota posterior

El usuario configuró la clave secreta en terminal con entrada oculta. La
configuración completó la prueba de carga, descarga, hash y rechazo público,
eliminando su objeto temporal. Se verificó después el bucket real privado,
con límite de 4 MiB y únicamente JPEG/PNG/WebP. La API se reinició y
`/health/ready` respondió `fotografias=SupabaseStorage`; web HTTP 200.
No se guardan claves en esta evidencia ni en el código del navegador.
La prueba física de cámara y dispositivos sigue pendiente.
