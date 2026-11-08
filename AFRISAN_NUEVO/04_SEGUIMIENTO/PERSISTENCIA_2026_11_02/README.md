# Pruebas de persistencia y reinicios

Fecha: **02/11/2026**. API .NET 8 compilada y ejecutada contra PGlite 0.5.11
por TCP con pglite-socket 0.2.17. Base, perfil de usuario, firma y fotografías
aislados de la configuración y datos reales.

Resultado final del ensayo: **37 comprobaciones correctas, cero fallos**.

`validar_api.mjs` y `resultados_api.json` comprueban:

- Administrador inicial y cuentas reales de prueba, acceso anónimo rechazado y
  creación de usuarios restringida a Administrador.
- Alta atómica de cilindro con rechazo de peso inválido y reversión completa.
- OT y fotografías con reintentos idempotentes, conflictos de UUID y versión.
- Aislamiento entre técnicos y denegación de fotografías de OT ajenas.
- Borradores, eliminación de fotos pendientes y protección de fotos confirmadas.
- Retiro, reemplazo, devolución sin uso, recepción, aprobación y cierre.
- Inventario, OT, pesos de terreno/recepción y archivos íntegros después de
  reiniciar la API, conservando la sesión y el ámbito de instalación.
- Bloqueo persistido después de cinco intentos fallidos y reinicio.
- RLS y permisos de la identidad restringida; no se ejecuta como propietario.

El adaptador socket no autentica el usuario del StartupMessage de PostgreSQL;
el ensayo establece explícitamente SET ROLE afrisan_api antes de conectar.
Esto verifica permisos SQL y controles de API, no autenticación real de ese
login, negociación TLS ni concurrencia del motor de Supabase. PGlite serializa
las consultas y no sustituye la prueba futura con dos conexiones PostgreSQL reales.

Verificación remota posterior: conexión restringida `afrisan_api` correcta con
TLS; actualización 07 aplicada tras autorización. API local en 5181 responde
`almacenamiento=PostgreSQL`; web en 5286 responde HTTP 200. El entorno público
no expone cuentas ni contraseñas demo. Al registrar esta comprobación todavía
no había usuarios; posteriormente el usuario creó su administrador por terminal.
La comprobación final encontró **un usuario y un administrador activo**, web
HTTP 200 y acceso anónimo al inventario rechazado con HTTP 401. No se guardan
correo ni contraseña del administrador en este informe.

Para repetir, tras compilar la solución y disponer de las dependencias en
`.local/validacion_sql_contexto/node_modules`:

```powershell
node AFRISAN_NUEVO/04_SEGUIMIENTO/PERSISTENCIA_2026_11_02/validar_api.mjs
```

No se conecta a Supabase ni se cargan allí datos de prueba. No acredita aún
ensayo físico de cámara, revisión visual en navegador o aceptación de usuarios.
El SQL 07 se aplicó posteriormente en Supabase, tras autorización explícita de
Dante, y se comprobó la conexión con la identidad restringida real. El diagnóstico
encontró cero usuarios; la creación del administrador requiere entrada del usuario
en terminal, ya completada y verificada posteriormente. No se ejecutó el recorrido con datos de prueba en Supabase.
