// Base privada por origen del navegador. No se envían fotos a terceros.
window.afrisanLocal = (() => {
    const abrir = () => new Promise((resolve, reject) => {
        const req = indexedDB.open('afrisan-nuevo-trabajo', 1);
        req.onupgradeneeded = () => req.result.createObjectStore('registros', { keyPath: 'clave' });
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(new Error('No se pudo abrir el almacenamiento local.'));
    });
    const guardar = async (tipo, usuario, id, valor) => {
        const db = await abrir();
        try {
            await new Promise((resolve, reject) => {
                const tx = db.transaction('registros', 'readwrite');
                tx.objectStore('registros').put({ clave: `${tipo}:${usuario}:${id}`, tipo, usuario, valor });
                tx.oncomplete = resolve;
                tx.onerror = () => reject(new Error('No se pudo conservar el borrador/foto. Revisa el espacio del dispositivo.'));
                tx.onabort = tx.onerror;
            });
        } finally { db.close(); }
    };
    const listar = async (tipo, usuario) => {
        const db = await abrir();
        try {
            return await new Promise((resolve, reject) => {
                const req = db.transaction('registros', 'readonly').objectStore('registros').getAll();
                req.onsuccess = () => resolve(req.result.filter(x => x.tipo === tipo && x.usuario === usuario).map(x => x.valor));
                req.onerror = () => reject(new Error('No se pudieron consultar los registros locales.'));
            });
        } finally { db.close(); }
    };
    const suscripciones = new Map();
    return {
        guardar, listar,
        conectado: () => navigator.onLine,
        suscribir: (id, receptor) => {
            const handler = () => receptor.invokeMethodAsync('ConexionRecuperada').catch(() => {});
            window.addEventListener('online', handler); suscripciones.set(id, handler);
        },
        cancelar: id => { const handler = suscripciones.get(id); if (handler) window.removeEventListener('online', handler); suscripciones.delete(id); }
    };
})();
