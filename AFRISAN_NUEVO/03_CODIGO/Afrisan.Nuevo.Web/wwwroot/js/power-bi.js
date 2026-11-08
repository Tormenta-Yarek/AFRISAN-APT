window.afrisanBi = {
    descargar: async (nombre, referencia) => {
        const contenido = await referencia.arrayBuffer();
        const url = URL.createObjectURL(new Blob([contenido], { type: 'application/zip' }));
        const enlace = document.createElement('a');
        enlace.href = url;
        enlace.download = nombre;
        document.body.appendChild(enlace);
        enlace.click();
        enlace.remove();
        setTimeout(() => URL.revokeObjectURL(url), 30000);
    }
};
