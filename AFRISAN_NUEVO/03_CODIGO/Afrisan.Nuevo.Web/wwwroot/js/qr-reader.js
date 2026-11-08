// jsQR 1.4.0 is bundled locally under its MIT license. Images stay on this device.
let session = 0, stream = null, finish = null;
function decode(source, maxSide) {
    const w = source.videoWidth || source.width, h = source.videoHeight || source.height;
    if (!w || !h) return null;
    const scale = Math.min(1, maxSide / Math.max(w, h));
    const canvas = document.createElement('canvas');
    canvas.width = Math.max(1, Math.round(w * scale)); canvas.height = Math.max(1, Math.round(h * scale));
    const ctx = canvas.getContext('2d', { willReadFrequently: true });
    ctx.drawImage(source, 0, 0, canvas.width, canvas.height);
    const pixels = ctx.getImageData(0, 0, canvas.width, canvas.height);
    return globalThis.jsQR(pixels.data, pixels.width, pixels.height, { inversionAttempts: 'attemptBoth' })?.data || null;
}
export async function readFile(id) {
    const input = document.getElementById(id), file = input?.files?.[0];
    if (!file) throw new Error('Selecciona una foto del QR.');
    try {
        if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) throw new Error('Utiliza una imagen PNG, JPEG o WebP.');
        if (file.size > 12 * 1024 * 1024) throw new Error('La imagen supera 12 MiB. Usa una foto más pequeña.');
        const bitmap = await createImageBitmap(file).catch(() => { throw new Error('No se pudo abrir la imagen seleccionada.'); });
        try {
            if (bitmap.width * bitmap.height > 60000000) throw new Error('La resolución de la imagen es demasiado grande.');
            const result = decode(bitmap, 1800) || decode(bitmap, 3200);
            if (!result) throw new Error('No se encontró un QR legible. Incluye toda la etiqueta, evita reflejos y vuelve a intentar.');
            return result;
        } finally { bitmap.close(); }
    } finally { input.value = ''; }
}
export function stopCamera() {
    session++;
    if (stream) { stream.getTracks().forEach(t => t.stop()); stream = null; }
    if (finish) { const done = finish; finish = null; done(null); }
}
export function downloadQr(dataUrl, name) {
    const base64 = dataUrl.split(',')[1];
    const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
    const url = URL.createObjectURL(new Blob([bytes], { type: 'image/png' }));
    const link = document.createElement('a'); link.href = url; link.download = name;
    document.body.append(link); link.click(); link.remove();
    setTimeout(() => URL.revokeObjectURL(url), 30000);
}
export async function startCamera(video) {
    stopCamera(); const current = session;
    if (!globalThis.isSecureContext || !navigator.mediaDevices?.getUserMedia)
        throw new Error('La cámara requiere HTTPS o localhost. Puedes seleccionar una foto del QR.');
    let captured;
    try { captured = await navigator.mediaDevices.getUserMedia({ video: { facingMode: { ideal: 'environment' } }, audio: false }); }
    catch (e) {
        if (e.name === 'NotAllowedError') throw new Error('No se autorizó la cámara. Habilita el permiso o selecciona una foto.');
        throw new Error('No se pudo abrir la cámara. Comprueba que esté disponible o selecciona una foto.');
    }
    if (current !== session) { captured.getTracks().forEach(t => t.stop()); return null; }
    stream = captured; video.srcObject = stream;
    try { await video.play(); }
    catch { stopCamera(); throw new Error('No se pudo mostrar la cámara. Selecciona una foto del QR.'); }
    if (current !== session) { captured.getTracks().forEach(t => t.stop()); video.srcObject = null; return null; }
    return new Promise((resolve, reject) => {
        const started = Date.now();
        finish = value => { video.srcObject = null; resolve(value); };
        function frame() {
            if (current !== session) return;
            try {
                const result = decode(video, 1000);
                if (result) { const done = finish; finish = null; stopCamera(); video.srcObject = null; done(result); return; }
                if (Date.now() - started > 60000) { finish = null; stopCamera(); video.srcObject = null; reject(new Error('No se encontró un QR en un minuto. Puedes volver a intentar o usar una foto.')); return; }
                setTimeout(frame, 350);
            } catch { finish = null; stopCamera(); video.srcObject = null; reject(new Error('No se pudo leer la cámara. Utiliza una foto del QR.')); }
        }
        frame();
    });
}
