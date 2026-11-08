# Fotografías en Supabase Storage privado

Implementación del **02/11/2026**, configurada y verificada contra Supabase real.
El usuario introdujo la clave en la terminal oculta; el bucket privado
`afrisan-evidencias` quedó habilitado. La API reiniciada confirma
`fotografias=SupabaseStorage` y la web responde HTTP 200.
La aplicación conserva su autorización por OT/rol;
el navegador no recibe claves Supabase, rutas públicas ni enlaces firmados.

## Activar

En este equipo la activación ya se completó. Los pasos siguientes permiten
preparar otra instalación o repetir la verificación sin borrar evidencias.

1. Detener la API mientras se copian las fotografías, para impedir nuevas
   escrituras locales durante el cambio de proveedor.
2. En Supabase → Settings → API Keys, copiar una **Secret key** dedicada al backend
   (`sb_secret_...`). No es la contraseña PostgreSQL ni la clave publishable/anon.
3. Ejecutar en una terminal local:

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File "D:\Afrisan\AFRISAN_NUEVO\03_CODIGO\Configurar-Storage.ps1"
```

La clave se introduce en una entrada oculta, nunca por argumentos ni en el chat.
El programa la guarda en User Secrets solo al finalizar las comprobaciones.
Las claves secretas tienen acceso privilegiado al proyecto: deben permanecer
exclusivamente en el servidor. Se recomienda una clave dedicada, revocable.
[Referencia de claves Supabase](https://supabase.com/docs/guides/getting-started/api-keys).

El comando deriva la URL del mismo proyecto que usa PostgreSQL; exige HTTPS.
Crea el bucket `afrisan-evidencias` privado, limitado a 4 MiB y JPEG/PNG/WebP.
Si ya existe con acceso público o límites incompatibles, se detiene.
No agrega políticas de acceso público ni modifica grants de PostgreSQL o Storage
mediante SQL; usa exclusivamente la API oficial de Storage.

## Copia de archivos existentes

Se consultan EvidenciaPesaje y FotosBorrador en una transacción de solo lectura.
Cada fotografía local referenciada se copia conservando ruta y SHA-256, y se
descarga para verificar contenido, tamaño y formato. Si falta un original,
solo se continúa si la copia en Storage ya existe y coincide (reanudación).

Los originales locales **no se borran**. Tampoco se reescriben los pesajes ni
sus metadatos confirmados: `servidor-privado` se conserva como alias histórico
que, después de una migración completa, apunta al bucket privado configurado.
Las fotografías nuevas registran el nombre real `afrisan-evidencias`.

Antes de activar el proveedor se prueba carga/descarga de una imagen temporal,
se comprueba que su ruta pública no permita acceder sin credenciales y se
elimina únicamente ese objeto temporal. Si falla una comprobación no se guarda
el proveedor nuevo. Se pueden reintentar las copias sin sobrescribir archivos.

Reiniciar la API después de una configuración satisfactoria. El estado
`/health/ready` indicará `fotografias=SupabaseStorage`; para comprobar el bucket:

```powershell
dotnet run --project Afrisan.Nuevo.Api --launch-profile http -- --verificar-storage
```

## Uso normal

- Las rutas de carga/lectura de la aplicación permanecen iguales.
- La API valida identidad, OT asignada y etapa de revisión antes de leer fotos.
- Comprueba MIME, firma de formato, tamaño y SHA-256.
- Cargas sin upsert: un archivo existente solo se acepta si coincide el contenido.
- Se rechazan buckets públicos, archivos alterados y respuestas no verificadas.
- Un fallo de Storage no confirma el despacho, el borrador ni el cambio físico.
- En modo Supabase no se escriben nuevas fotos en disco y no se vuelve a disco
  silenciosamente ante un error. Las credenciales solo se envían al proyecto
  configurado; el cliente HTTP no sigue redirecciones.

Las claves nuevas se envían en `apikey`; las JWT service_role heredadas también
usan Authorization Bearer. No se envía el JWT de AFRISAN a Supabase.
Los objetos huérfanos por un fallo entre Storage y PostgreSQL requieren una
limpieza administrativa futura que respete referencias; esta entrega no elimina
evidencias ni objetos ajenos. Los respaldos deben contemplar Storage y PostgreSQL.

## Pruebas

[Pruebas aisladas de migración y fallos](../04_SEGUIMIENTO/STORAGE_2026_11_02/README.md).
El ensayo usa un doble HTTP de Storage y PostgreSQL embebido, sin datos ni claves
reales. La validación física de cámara y de dispositivos sigue siendo independiente.
