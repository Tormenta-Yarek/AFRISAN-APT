// Desarrollo: no cachear el ensamblado mientras cambia el código.
// Los borradores/fotos sí se guardan en IndexedDB mientras la app está abierta.
// dotnet publish sustituye este archivo por service-worker.published.js.
self.addEventListener('fetch', () => {});
