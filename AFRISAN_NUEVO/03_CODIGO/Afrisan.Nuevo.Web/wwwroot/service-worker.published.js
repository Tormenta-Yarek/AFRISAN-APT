// Solo archivos estáticos de la aplicación. Nunca cachear respuestas de la API.
self.importScripts('./service-worker-assets.js');
const prefix = 'afrisan-nuevo-static-';
const cacheName = prefix + self.assetsManifest.version;
self.addEventListener('install', event => event.waitUntil((async () => {
    const cache = await caches.open(cacheName);
    const assets = self.assetsManifest.assets.filter(a => !/service-worker\.js$/.test(a.url));
    await cache.addAll(assets.map(a => new Request(a.url, { integrity: a.hash, cache: 'no-cache' })));
    await cache.add(new Request('./index.html', { cache: 'no-cache' }));
    await self.skipWaiting();
})()));
self.addEventListener('activate', event => event.waitUntil((async () => {
    for (const key of await caches.keys()) if (key.startsWith(prefix) && key !== cacheName) await caches.delete(key);
    await self.clients.claim();
})()));
self.addEventListener('fetch', event => {
    if (event.request.method !== 'GET' || new URL(event.request.url).origin !== self.location.origin) return;
    event.respondWith((async () => {
        const cache = await caches.open(cacheName);
        if (event.request.mode === 'navigate') return (await cache.match('./index.html')) || fetch(event.request);
        return (await cache.match(event.request)) || fetch(event.request);
    })());
});
