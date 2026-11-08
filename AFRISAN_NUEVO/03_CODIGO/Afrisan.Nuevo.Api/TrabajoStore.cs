using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Api;

// Proveedor temporal, compartiendo el bloqueo de inventario para cambios atómicos.
// No hay escritura en Supabase ni archivos públicos de fotografías.
public sealed class TrabajoStore(InventarioStore inventario, SesionesDemo sesiones)
{
    public const int MaxFotoBytes = 4 * 1024 * 1024;
    private readonly Dictionary<Guid, Orden> ordenes = [];
    private readonly Dictionary<Guid, (string Huella, Guid Id)> altas = [];
    private long bytesFotos;

    public OtResponse[] Listar(ClaimsPrincipal user)
    {
        lock (inventario.SyncRoot) return ordenes.Values.Where(o => !user.IsInRole("Tecnico") || o.Tecnico == Actor(user))
            .OrderByDescending(o => o.Fecha).Select(Ver).ToArray();
    }
    public OtResponse Obtener(Guid id, ClaimsPrincipal user)
    {
        lock (inventario.SyncRoot) return Ver(Acceder(id, user));
    }

    public CilindroResponse[] Disponibles(Guid id, ClaimsPrincipal user)
    {
        lock (inventario.SyncRoot)
        {
            var ot = Acceder(id, user);
            if (!EsEditable(ot)) return [];
            return inventario.Cilindros().Where(c => c.Ubicacion == "Bodega" && c.Estado == "Disponible"
                && c.PesajeRegistrado is not null && ot.Detalle.Any(l => l.RefrigeranteId == c.RefrigeranteId)).ToArray();
        }
    }

    public OtResponse Crear(CrearOtRequest dto, ClaimsPrincipal user)
    {
        lock (inventario.SyncRoot)
        {
            if (dto.IdOperacion == Guid.Empty) Fallar("Falta el identificador de operación.");
            var huella = JsonSerializer.Serialize(dto);
            if (altas.TryGetValue(dto.IdOperacion, out var anterior))
            {
                if (anterior.Huella != huella) Fallar("El identificador de creación ya fue utilizado con otros datos.", 409);
                return Ver(ordenes[anterior.Id]);
            }
            if (string.IsNullOrWhiteSpace(dto.Sucursal) || string.IsNullOrWhiteSpace(dto.Direccion) || string.IsNullOrWhiteSpace(dto.Descripcion)) Fallar("Sucursal, dirección y descripción son obligatorias.");
            var tecnico = sesiones.Cuentas.FirstOrDefault(c => c.Rol == "Tecnico" && c.Email.Equals(dto.TecnicoEmail, StringComparison.OrdinalIgnoreCase))
                ?? throw new ReglaInventarioException("Selecciona un técnico de la instancia demo.");
            if (dto.Detalle is null || dto.Detalle.Length == 0 || dto.Detalle.Length > 20 || dto.Detalle.Any(l => l is null || l.GrSolicitados <= 0 || l.GrAutorizados <= 0)
                || dto.Detalle.Select(l => l.RefrigeranteId).Distinct().Count() != dto.Detalle.Length) Fallar("Agrega un detalle válido por refrigerante, sin repetirlo.");
            var catalogo = inventario.Refrigerantes();
            var lineas = dto.Detalle!.Select(l => new LineaOtResponse(l.RefrigeranteId,
                catalogo.FirstOrDefault(r => r.Id == l.RefrigeranteId)?.Codigo ?? throw new ReglaInventarioException("Refrigerante no registrado."),
                l.GrSolicitados, l.GrAutorizados)).ToArray();
            var codigos = (dto.CilindrosARetirar ?? []).Select(c => c?.Trim().ToUpperInvariant() ?? "").ToArray();
            if (codigos.Any(string.IsNullOrWhiteSpace) || codigos.Distinct().Count() != codigos.Length) Fallar("La lista de cilindros a retirar contiene códigos vacíos o repetidos.");
            var retirados = codigos.Select(BuscarCilindro).ToArray();
            foreach (var c in retirados)
            {
                if (!lineas.Any(l => l.RefrigeranteId == c.RefrigeranteId)) Fallar("El cilindro a retirar no corresponde a un refrigerante del pedido.");
                if (c.Ubicacion != "Sucursal" || !string.Equals(c.Sucursal, dto.Sucursal.Trim(), StringComparison.OrdinalIgnoreCase))
                    Fallar($"{c.Codigo} no está instalado en esa sucursal. Los cilindros de Bodega se registran como entrega, no como retiro.", 409);
                if (ordenes.Values.Any(o => o.Estado != "Cerrada" && o.Retirar.Contains(c.Codigo))) Fallar($"{c.Codigo} está solicitado en otra OT abierta.", 409);
            }
            var ot = new Orden { Id = Guid.NewGuid(), Codigo = $"OT-{ordenes.Count + 1:D6}", Sucursal = dto.Sucursal.Trim(),
                Direccion = dto.Direccion.Trim(), Descripcion = dto.Descripcion.Trim(), Tecnico = tecnico.Email,
                Autorizador = Actor(user), Detalle = lineas, Retirar = codigos };
            // Autorizar una OT reserva el retiro; no cambia la ubicación física.
            ordenes.Add(ot.Id, ot); altas.Add(dto.IdOperacion, (huella, ot.Id));
            Anotar(ot, user, "OT autorizada", "Pedido manual de revisión, técnico y cilindros a retirar registrados. No es integración Defontana.");
            return Ver(ot);
        }
    }

    public OtResponse Entregar(Guid id, EntregarCilindroRequest dto, ClaimsPrincipal user)
    {
        lock (inventario.SyncRoot)
        {
            var ot = Acceder(id, user);
            if (dto.IdOperacion == Guid.Empty) Fallar("Falta el identificador del retiro de bodega.");
            var huella = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(dto)));
            if (ot.Despachos.TryGetValue(dto.IdOperacion, out var existente))
            {
                if (existente.Huella != huella) Fallar("El identificador del despacho ya se utilizó con otros datos.", 409);
                return Ver(ot);
            }
            Editable(ot);
            var c = BuscarCilindro(dto.Codigo);
            if (ot.Entregas.Any(e => e.Codigo == c.Codigo && e.Estado == "EnProcesoDespacho")) Fallar("Este cilindro ya está en proceso de despacho para la OT.", 409);
            if (ot.Entregas.Any(e => e.Codigo == c.Codigo && e.Estado == "Instalado")) Fallar("Este cilindro ya se instaló en esta OT.", 409);
            if (c.Ubicacion != "Bodega" || c.Estado != "Disponible" || c.PesajeRegistrado is null) Fallar("Solo se entrega un cilindro disponible, pesado y ubicado en bodega.", 409);
            if (!ot.Detalle.Any(l => l.RefrigeranteId == c.RefrigeranteId)) Fallar("El refrigerante no está en el detalle autorizado de esta OT.");
            ValidarPeso(c, dto.PesoBrutoGr);
            if (dto.PesoBrutoGr == c.TaraGr) Fallar("El cilindro a retirar para instalación debe contener refrigerante.");
            var foto = dto.Foto ?? throw new ReglaInventarioException("Adjunta la foto del peso al retirar el cilindro de bodega.");
            if (foto.Id == Guid.Empty || string.IsNullOrWhiteSpace(foto.Nombre) || foto.Nombre.Length > 150) Fallar("Identificador o nombre de fotografía inválido.");
            var datos = LeerImagen(new() { Id = foto.Id, Nombre = foto.Nombre, ContentType = foto.ContentType, Base64 = foto.Base64 });
            if (bytesFotos + datos.Length > 256L * 1024 * 1024) Fallar("La memoria de evidencias está llena. No se registró el despacho.", 409);
            var fecha = DateTimeOffset.UtcNow;
            var info = new FotoResponse(foto.Id, "Despacho", c.Codigo, Path.GetFileName(foto.Nombre), foto.ContentType, datos.Length, fecha, Actor(user));
            ot.Despachos.Add(dto.IdOperacion, (huella, new(info, datos))); bytesFotos += datos.Length;
            ot.Entregas.Add(new(c.Codigo, c.RefrigeranteCodigo, c.TaraGr, c.CapacidadGr, dto.PesoBrutoGr, Actor(user), fecha,
                Id: dto.IdOperacion, FotoDespacho: info));
            inventario.Ubicar(c.Codigo, "Tecnico", "EnProcesoDespacho", null, ot.Tecnico, Actor(user), $"Retiro de bodega para {ot.Codigo}", dto.PesoBrutoGr);
            if (ot.Estado == "Asignada") ot.Estado = "EnProcesoDespacho";
            Anotar(ot, user, "Retiro de bodega por técnico", $"{c.Codigo}: {dto.PesoBrutoGr} g. Foto {foto.Id}.");
            return Ver(ot);
        }
    }

    public (byte[] Bytes, string Mime) LeerFotoDespacho(Guid id, Guid entrega, ClaimsPrincipal user)
    {
        lock (inventario.SyncRoot)
        {
            var ot = Acceder(id, user); AccesoEvidencias(ot, user);
            var despacho = ot.Despachos.GetValueOrDefault(entrega).Foto
                ?? throw new ReglaInventarioException("Foto de despacho no encontrada.", 404);
            return (despacho.Bytes, despacho.Info.ContentType);
        }
    }

    public OperacionResponse Guardar(Guid id, GuardarOperacionRequest dto, ClaimsPrincipal user)
    {
        lock (inventario.SyncRoot)
        {
            var ot = Acceder(id, user); Editable(ot);
            if (dto.Instancia != sesiones.Instancia) Fallar("El borrador corresponde a otra ejecución de la API. Se conserva localmente; no se reasigna a otra OT.", 409);
            if (dto.Id == Guid.Empty || dto.Tipo is not ("Reemplazo" or "Agregado")) Fallar("Identificador o tipo de operación inválido.");
            if (dto.Tipo == "Agregado" && (!string.IsNullOrWhiteSpace(dto.CodigoRetirado) || dto.PesoRetiradoGr is not null)) Fallar("Un agregado no lleva cilindro retirado.");
            var limpio = dto with { CodigoRetirado = LimpiarCodigo(dto.CodigoRetirado), CodigoInstalado = LimpiarCodigo(dto.CodigoInstalado) };
            if (limpio.CodigoInstalado is not null && !ot.Entregas.Any(e => e.Codigo == limpio.CodigoInstalado)) Fallar("El cilindro a instalar no fue entregado para esta OT.");
            if (limpio.CodigoRetirado is not null && !ot.Retirar.Contains(limpio.CodigoRetirado)) Fallar("El cilindro a retirar no está en el pedido autorizado.");
            if (!ot.Operaciones.TryGetValue(dto.Id, out var op))
            {
                if (dto.Version != 0) Fallar("No existe esa versión de borrador.", 409);
                op = new Operacion { Datos = limpio, Actualizada = DateTimeOffset.UtcNow };
                ot.Operaciones.Add(dto.Id, op);
            }
            else
            {
                if (op.Estado == "Descartada") Fallar("Este borrador fue descartado. Crea una operación nueva.", 409);
                if ((op.Datos with { Version = 0 }) == (limpio with { Version = 0 })) return VerOperacion(op);
                if (op.Estado != "Borrador") Fallar("La operación ya fue confirmada. Solicita rectificación al Autorizador.", 409);
                if (dto.Version != op.Version) Fallar("Otra pestaña modificó el borrador. Consulta la versión actual antes de resolver el conflicto.", 409);
                if (op.Fotos.Values.Any(f => f.Info.Grupo == "Retirado") && limpio.CodigoRetirado != op.Datos.CodigoRetirado
                    || op.Fotos.Values.Any(f => f.Info.Grupo == "Instalado") && limpio.CodigoInstalado != op.Datos.CodigoInstalado)
                    Fallar("El cilindro tiene fotos asociadas. Retira las fotos del borrador antes de cambiar su código.", 409);
                op.Datos = limpio;
            }
            op.Version++; op.Actualizada = DateTimeOffset.UtcNow;
            Anotar(ot, user, "Borrador guardado", dto.Id.ToString());
            return VerOperacion(op);
        }
    }

    public OperacionResponse Foto(Guid id, Guid operacionId, SubirFotoRequest dto, ClaimsPrincipal user)
    {
        lock (inventario.SyncRoot)
        {
            var ot = Acceder(id, user); Editable(ot); var op = OperacionDe(ot, operacionId);
            if (dto.Id == Guid.Empty || dto.Grupo is not ("Retirado" or "Instalado")) Fallar("Identificador o grupo de foto inválido.");
            var codigo = dto.Grupo == "Retirado" ? op.Datos.CodigoRetirado : op.Datos.CodigoInstalado;
            if (codigo is null) Fallar("Selecciona el cilindro antes de adjuntar sus fotos.");
            var datos = LeerImagen(dto);
            if (op.Fotos.TryGetValue(dto.Id, out var existente))
            {
                if (existente.Info.Grupo != dto.Grupo || existente.Info.CodigoCilindro != codigo || !existente.Bytes.AsSpan().SequenceEqual(datos)) Fallar("El identificador de foto ya está usado con otro archivo.", 409);
                return VerOperacion(op);
            }
            if (op.Estado != "Borrador") Fallar("Las evidencias confirmadas no se reemplazan.", 409);
            if (dto.Version != op.Version) Fallar("El borrador cambió antes de adjuntar la foto. Consulta su versión actual.", 409);
            if (op.Fotos.Values.Any(f => f.Bytes.AsSpan().SequenceEqual(datos))) Fallar("La misma fotografía ya está adjunta a esta operación. Cada envase debe tener su propia evidencia.", 409);
            if (op.Fotos.Values.Count(f => f.Info.Grupo == dto.Grupo) >= 8) Fallar("Máximo 8 fotos por cada grupo de pesaje en esta revisión.");
            if (bytesFotos + datos.Length > 256L * 1024 * 1024) Fallar("La memoria de evidencias de esta instancia está llena. No se guardó la foto.", 409);
            var info = new FotoResponse(dto.Id, dto.Grupo, codigo!, Path.GetFileName(dto.Nombre), dto.ContentType, datos.Length, DateTimeOffset.UtcNow, Actor(user));
            op.Fotos.Add(dto.Id, new(info, datos)); bytesFotos += datos.Length; op.Version++; op.Actualizada = DateTimeOffset.UtcNow;
            Anotar(ot, user, "Evidencia adjunta", $"{dto.Grupo}: {codigo} / {dto.Id}");
            return VerOperacion(op);
        }
    }

    public OperacionResponse QuitarFoto(Guid id, Guid operacionId, Guid fotoId, ClaimsPrincipal user, int? version = null)
    {
        lock (inventario.SyncRoot)
        {
            var ot = Acceder(id, user); Editable(ot); var op = OperacionDe(ot, operacionId);
            if (op.Estado != "Borrador") Fallar("No se eliminan evidencias de trabajos confirmados.", 409);
            if (version.HasValue && version != op.Version) Fallar("El borrador cambió antes de retirar la foto. No se sobrescribieron datos.", 409);
            if (op.Fotos.Remove(fotoId, out var foto)) { bytesFotos -= foto.Bytes.Length; op.Version++; Anotar(ot, user, "Foto retirada del borrador", fotoId.ToString()); }
            return VerOperacion(op);
        }
    }
    public (byte[] Bytes, string Mime) LeerFoto(Guid id, Guid operacionId, Guid fotoId, ClaimsPrincipal user)
    {
        lock (inventario.SyncRoot)
        {
            var ot = Acceder(id, user); AccesoEvidencias(ot, user);
            var op = OperacionDe(ot, operacionId);
            var foto = op.Fotos.GetValueOrDefault(fotoId) ?? throw new ReglaInventarioException("Foto no encontrada.", 404);
            return (foto.Bytes, foto.Info.ContentType);
        }
    }

    public OperacionResponse Confirmar(Guid id, Guid operacionId, ClaimsPrincipal user, int? version = null)
    {
        lock (inventario.SyncRoot)
        {
            var ot = Acceder(id, user); var op = OperacionDe(ot, operacionId);
            if (op.Estado == "Registrada") return VerOperacion(op);
            Editable(ot);
            if (op.Estado != "Borrador") Fallar("El borrador fue descartado.", 409);
            if (version.HasValue && version != op.Version) Fallar("El borrador cambió antes de confirmar. Revísalo otra vez.", 409);
            var d = op.Datos;
            if (d.CodigoInstalado is null || d.PesoInstaladoGr is null) Fallar("Falta cilindro instalado y su peso bruto.");
            var nuevo = BuscarCilindro(d.CodigoInstalado!);
            if (!ot.Entregas.Any(e => e.Codigo == nuevo.Codigo && e.Estado == "EnProcesoDespacho") || nuevo.Ubicacion != "Tecnico" || nuevo.Tecnico != ot.Tecnico) Fallar("El cilindro no está en la entrega/custodia de esta OT.", 409);
            ValidarPeso(nuevo, d.PesoInstaladoGr!.Value);
            if (!op.Fotos.Values.Any(f => f.Info.Grupo == "Instalado")) Fallar("Adjunta al menos una foto del pesaje del cilindro instalado.");
            CilindroResponse? viejo = null;
            if (d.Tipo == "Reemplazo")
            {
                if (d.CodigoRetirado is null || d.PesoRetiradoGr is null) Fallar("El reemplazo exige cilindro retirado y su peso bruto.");
                viejo = BuscarCilindro(d.CodigoRetirado!);
                if (viejo.Codigo == nuevo.Codigo || !ot.Retirar.Contains(viejo.Codigo) || viejo.Ubicacion != "Sucursal" || !string.Equals(viejo.Sucursal, ot.Sucursal, StringComparison.OrdinalIgnoreCase)) Fallar("El cilindro retirado no corresponde a esta sucursal/pedido.", 409);
                if (viejo.RefrigeranteId != nuevo.RefrigeranteId) Fallar("El reemplazo debe conservar el tipo de refrigerante del envase retirado.");
                ValidarPeso(viejo, d.PesoRetiradoGr!.Value);
                if (!op.Fotos.Values.Any(f => f.Info.Grupo == "Retirado")) Fallar("Adjunta al menos una foto del pesaje del cilindro retirado.");
            }
            if (ot.Operaciones.Values.Any(o => o != op && o.Estado == "Registrada" &&
                (o.Datos.CodigoInstalado == nuevo.Codigo || viejo is not null && o.Datos.CodigoRetirado == viejo.Codigo))) Fallar("Uno de los cilindros ya se utilizó en otra operación de esta OT.", 409);
            // Todas las comprobaciones preceden a las escrituras bajo el bloqueo común.
            var entrega = ot.Entregas.FindLastIndex(e => e.Codigo == nuevo.Codigo && e.Estado == "EnProcesoDespacho");
            ot.Entregas[entrega] = ot.Entregas[entrega] with { Estado = "Instalado" };
            inventario.Ubicar(nuevo.Codigo, "Sucursal", "Instalado", ot.Sucursal, null, Actor(user), $"Instalación física en {ot.Codigo}", d.PesoInstaladoGr);
            if (viejo is not null) inventario.Ubicar(viejo.Codigo, "Tecnico", "RetiradoPendienteRecepcion", null, ot.Tecnico, Actor(user), $"Retiro físico de {ot.Sucursal}", d.PesoRetiradoGr);
            op.Estado = "Registrada"; op.Version++; op.Actualizada = DateTimeOffset.UtcNow;
            ot.Estado = "EnTerreno";
            Anotar(ot, user, "Trabajo físico registrado", $"{d.Tipo}: {d.CodigoRetirado ?? "sin retiro"} → {nuevo.Codigo}");
            return VerOperacion(op);
        }
    }

    public OtResponse InformarEntrega(Guid id, Guid operacionId, ClaimsPrincipal user)
    {
        lock (inventario.SyncRoot)
        {
            var ot = Acceder(id, user); Abierta(ot); var op = OperacionDe(ot, operacionId);
            if (op.Estado != "Registrada" || op.Datos.Tipo != "Reemplazo") Fallar("Solo se informa la entrega de un envase retirado ya registrado.");
            if (!op.EntregaInformada) { op.EntregaInformada = true; Anotar(ot, user, "Devolución informada por técnico", op.Datos.CodigoRetirado!); }
            return Ver(ot); // No equivale a la recepción oficial de bodega.
        }
    }
    public OtResponse Recibir(Guid id, RecibirCilindroRequest dto, ClaimsPrincipal user)
    {
        lock (inventario.SyncRoot)
        {
            var ot = Acceder(id, user); Abierta(ot);
            var c = BuscarCilindro(dto.Codigo); ValidarPeso(c, dto.PesoBrutoGr);
            var op = ot.Operaciones.Values.FirstOrDefault(o => o.Estado == "Registrada" && o.Datos.CodigoRetirado == c.Codigo);
            if (op?.Recibido == true)
            {
                if (op.PesoRecepcionGr != dto.PesoBrutoGr) Fallar("La recepción ya está confirmada con otro peso. No se sobrescribe.", 409);
                return Ver(ot);
            }
            if (ot.AprobacionBodega is not null) Fallar("Bodega ya aprobó esta OT. No se sustituyen los pesos revisados.", 409);
            if (op is not null && !op.EntregaInformada) Fallar("El técnico debe informar la devolución del cilindro antes de confirmar su recepción.", 409);
            var entrega = ot.Entregas.FindLastIndex(e => e.Codigo == c.Codigo && e.Estado == "EnProcesoDespacho");
            if (op is null && entrega < 0) Fallar("No hay retiro ni entrega sin uso pendiente de ese envase en esta OT.", 409);
            if (c.Ubicacion != "Tecnico" || c.Tecnico != ot.Tecnico) Fallar("El envase no está bajo esta custodia.", 409);
            inventario.Ubicar(c.Codigo, "Bodega", dto.PesoBrutoGr == c.TaraGr ? "Vacio" : "Disponible", null, null, Actor(user), "Recepción de envase retirado", dto.PesoBrutoGr);
            if (op is not null) { op.Recibido = true; op.UsuarioRecepcion = Actor(user); op.FechaRecepcion = DateTimeOffset.UtcNow; op.PesoRecepcionGr = dto.PesoBrutoGr; op.ObservacionRecepcion = dto.Observacion?.Trim(); }
            else ot.Entregas[entrega] = ot.Entregas[entrega] with { Estado = "Devuelto", PesoRecepcionGr = dto.PesoBrutoGr, UsuarioRecepcion = Actor(user), FechaRecepcion = DateTimeOffset.UtcNow };
            Anotar(ot, user, "Recepción oficial de bodega", $"{c.Codigo}: {dto.PesoBrutoGr} g. {dto.Observacion}");
            return Ver(ot);
        }
    }

    public OtResponse Incidencia(Guid id, CrearIncidenciaRequest dto, ClaimsPrincipal user)
    {
        lock (inventario.SyncRoot)
        {
            var ot = Acceder(id, user);
            Abierta(ot);
            if (ot.AprobacionBodega is not null) Fallar("La OT ya fue aprobada por Bodega. No se agregan incidencias a una revisión firmada.", 409);
            if (dto.Id == Guid.Empty || dto.Tipo is not ("Incidencia" or "Rectificacion") || string.IsNullOrWhiteSpace(dto.Motivo)) Fallar("Ingresa un motivo y un tipo válido.");
            if (dto.OperacionId.HasValue) OperacionDe(ot, dto.OperacionId.Value);
            var existente = ot.Incidencias.FirstOrDefault(i => i.Id == dto.Id);
            if (existente is not null)
            {
                if (existente.Usuario != Actor(user) || existente.Motivo != dto.Motivo.Trim() || existente.Tipo != dto.Tipo || existente.OperacionId != dto.OperacionId) Fallar("Identificador de incidencia usado con otros datos.", 409);
                return Ver(ot);
            }
            ot.Incidencias.Add(new(dto.Id, dto.OperacionId, dto.Tipo, dto.Motivo.Trim(), Actor(user), DateTimeOffset.UtcNow, null, null, null));
            Anotar(ot, user, dto.Tipo, dto.Motivo.Trim());
            return Ver(ot);
        }
    }
    public OtResponse Resolver(Guid id, Guid incidenciaId, ResolverIncidenciaRequest dto, ClaimsPrincipal user)
    {
        lock (inventario.SyncRoot)
        {
            var ot = Acceder(id, user); Abierta(ot);
            if (ot.AprobacionBodega is null) Fallar("Bodega debe aprobar los cilindros y sus pesos antes de la revisión del Autorizador.", 409);
            var indice = ot.Incidencias.FindIndex(i => i.Id == incidenciaId);
            if (indice < 0) Fallar("Incidencia no encontrada.", 404);
            if (ot.Incidencias[indice].Respuesta is not null) Fallar("La revisión ya está registrada; no se sobrescribe.", 409);
            if (string.IsNullOrWhiteSpace(dto.Respuesta)) Fallar("Ingresa la conclusión de la revisión.");
            ot.Incidencias[indice] = ot.Incidencias[indice] with { Respuesta = dto.Respuesta.Trim(), Revisor = Actor(user), FechaRevision = DateTimeOffset.UtcNow };
            Anotar(ot, user, "Revisión de incidencia", dto.Respuesta.Trim());
            return Ver(ot);
        }
    }
    public OtResponse Terminar(Guid id, TerminarTrabajoRequest dto, ClaimsPrincipal user)
    {
        lock (inventario.SyncRoot)
        {
            var ot = Acceder(id, user);
            if (ot.Estado is "PendienteBodega" or "PendienteRevision" or "Cerrada") return Ver(ot);
            Editable(ot);
            if (!ot.Operaciones.Values.Any(o => o.Estado == "Registrada") || ot.Operaciones.Values.Any(o => o.Estado == "Borrador")) Fallar("Registra al menos una operación y confirma o descarta los borradores antes de informar el término.");
            if (ot.Retirar.Any(c => !ot.Operaciones.Values.Any(o => o.Estado == "Registrada" && o.Datos.CodigoRetirado == c)) && string.IsNullOrWhiteSpace(dto.Observacion))
                Fallar("Quedan cilindros del pedido sin retirar. Explica la ejecución parcial al Autorizador.");
            ot.Estado = "PendienteBodega"; ot.ObservacionTermino = dto.Observacion?.Trim();
            Anotar(ot, user, "Término técnico informado", dto.Observacion ?? "Enviado a Bodega para confirmar cilindros y pesos antes de la revisión del Autorizador.");
            return Ver(ot);
        }
    }

    public OtResponse AprobarBodega(Guid id, RevisarTrabajoRequest dto, ClaimsPrincipal user)
    {
        lock (inventario.SyncRoot)
        {
            var ot = Acceder(id, user);
            if (ot.AprobacionBodega is not null) return Ver(ot);
            if (ot.Estado != "PendienteBodega") Fallar("El técnico debe informar el término antes de la aprobación de Bodega.", 409);
            RecepcionesCompletas(ot);
            if (!dto.ConfirmoRevision) Fallar("Confirma que revisaste los cilindros, pesos y fotos antes de aprobar.");
            ot.AprobacionBodega = new(Actor(user), DateTimeOffset.UtcNow, dto.Observacion?.Trim());
            ot.Estado = "PendienteRevision";
            Anotar(ot, user, "Aprobación de Bodega", dto.Observacion ?? "Cilindros, pesos de recepción y evidencias revisados. Se habilita la revisión del Autorizador.");
            return Ver(ot);
        }
    }

    public OtResponse RevisarAutorizador(Guid id, RevisarTrabajoRequest dto, ClaimsPrincipal user)
    {
        lock (inventario.SyncRoot)
        {
            var ot = Acceder(id, user);
            if (ot.RevisionAutorizador is not null) return Ver(ot);
            if (ot.Estado != "PendienteRevision" || ot.AprobacionBodega is null) Fallar("Primero Bodega debe confirmar y aprobar los cilindros y sus pesos.", 409);
            RecepcionesCompletas(ot);
            if (ot.Incidencias.Any(i => i.Respuesta is null)) Fallar("Revisa las incidencias antes de aprobar el registro.");
            if (!dto.ConfirmoRevision) Fallar("Confirma que comparaste las fotografías con los registros de despacho, terreno y Bodega.");
            ot.RevisionAutorizador = new(Actor(user), DateTimeOffset.UtcNow, dto.Observacion?.Trim());
            Anotar(ot, user, "Revisión de fotos y registro por Autorizador", dto.Observacion ?? "Fotografías contrastadas con el registro después de la aprobación de Bodega.");
            return Ver(ot);
        }
    }

    private static void RecepcionesCompletas(Orden ot)
    {
        if (ot.Operaciones.Values.Any(o => o.Estado == "Registrada" && o.Datos.Tipo == "Reemplazo" && !o.Recibido)) Fallar("Bodega debe confirmar los envases retirados antes de aprobar o cerrar.");
        if (ot.Entregas.Any(e => e.Estado == "EnProcesoDespacho")) Fallar("Quedan envases de esta OT sin instalar ni devolver. Resuelve sus entregas antes de aprobar o cerrar.");
    }
    public OtResponse Cerrar(Guid id, ClaimsPrincipal user)
    {
        lock (inventario.SyncRoot)
        {
            var ot = Acceder(id, user);
            if (ot.Estado == "Cerrada") return Ver(ot);
            if (ot.Estado != "PendienteRevision") Fallar("El cierre requiere el término técnico y la aprobación previa de Bodega.", 409);
            if (ot.AprobacionBodega is null || ot.RevisionAutorizador is null) Fallar("Falta la aprobación de Bodega o la revisión de fotos y registro del Autorizador.", 409);
            RecepcionesCompletas(ot);
            if (ot.Incidencias.Any(i => i.Respuesta is null)) Fallar("Hay incidencias o rectificaciones sin revisar.");
            ot.Estado = "Cerrada"; Anotar(ot, user, "Cierre administrativo", "Autorizador verificó el trabajo y las recepciones.");
            return Ver(ot);
        }
    }

    public OtResponse Descartar(Guid id, Guid operacionId, int version, ClaimsPrincipal user)
    {
        lock (inventario.SyncRoot)
        {
            var ot = Acceder(id, user); Editable(ot); var op = OperacionDe(ot, operacionId);
            if (op.Estado == "Descartada") return Ver(ot);
            if (op.Estado != "Borrador" || op.Version != version) Fallar("Solo se descarta un borrador sin cambios pendientes de otra pestaña.", 409);
            op.Estado = "Descartada"; op.Version++; op.Actualizada = DateTimeOffset.UtcNow;
            Anotar(ot, user, "Borrador descartado", operacionId.ToString());
            return Ver(ot);
        }
    }

    private Orden Acceder(Guid id, ClaimsPrincipal user)
    {
        if (!ordenes.TryGetValue(id, out var ot)) throw new ReglaInventarioException("OT no encontrada en esta instancia.", 404);
        if (user.IsInRole("Tecnico") && ot.Tecnico != Actor(user)) Fallar("La OT pertenece a otro técnico.", 403);
        return ot;
    }
    private CilindroResponse BuscarCilindro(string codigo) => inventario.Cilindro(codigo) ?? throw new ReglaInventarioException($"Cilindro {codigo} no registrado.");
    private static Operacion OperacionDe(Orden ot, Guid id) => ot.Operaciones.GetValueOrDefault(id) ?? throw new ReglaInventarioException("Operación no encontrada.", 404);
    private static string Actor(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new ReglaInventarioException("Sesión inválida.", 401);
    private static void Abierta(Orden ot) { if (ot.Estado == "Cerrada") Fallar("La OT ya está cerrada.", 409); }
    private static bool EsEditable(Orden ot) => ot.Estado is "Asignada" or "EnProcesoDespacho" or "EnTerreno";
    private static void Editable(Orden ot) { if (!EsEditable(ot)) Fallar("La OT ya se envió a revisión o está cerrada. Solicita rectificación.", 409); }
    private static void AccesoEvidencias(Orden ot, ClaimsPrincipal user)
    {
        if (user.IsInRole("Autorizador") && ot.AprobacionBodega is null)
            Fallar("La revisión de fotografías del Autorizador se habilita después de la aprobación de Bodega.", 409);
    }
    private static void Fallar(string mensaje, int codigo = 400) => throw new ReglaInventarioException(mensaje, codigo);
    private static string? LimpiarCodigo(string? codigo) => string.IsNullOrWhiteSpace(codigo) ? null : codigo.Trim().ToUpperInvariant();
    private static void ValidarPeso(CilindroResponse c, long peso)
    {
        if (peso < c.TaraGr || (decimal)peso > (decimal)c.TaraGr + c.CapacidadGr) Fallar($"Peso inválido de {c.Codigo}: usa gramos brutos entre tara y tara más capacidad.");
    }
    private static byte[] LeerImagen(SubirFotoRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Base64) || dto.Base64.Length > 5_600_000) Fallar("La foto está vacía o supera el límite de 4 MiB.");
        byte[] datos;
        try { datos = Convert.FromBase64String(dto.Base64); } catch (FormatException) { throw new ReglaInventarioException("Archivo de foto inválido."); }
        if (datos.Length is < 12 or > MaxFotoBytes) Fallar("Foto vacía o mayor a 4 MiB.");
        var valida = dto.ContentType switch
        {
            "image/jpeg" => datos[0] == 255 && datos[1] == 216 && datos[2] == 255,
            "image/png" => datos.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            "image/webp" => System.Text.Encoding.ASCII.GetString(datos, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(datos, 8, 4) == "WEBP",
            _ => false
        };
        if (!valida) Fallar("Solo se aceptan fotos JPEG, PNG o WebP cuya firma coincida con el formato.");
        return datos;
    }
    private static void Anotar(Orden ot, ClaimsPrincipal user, string accion, string descripcion) => ot.Historial.Add(new(DateTimeOffset.UtcNow, accion, Actor(user), descripcion));
    private static OperacionResponse VerOperacion(Operacion op) => new(op.Datos.Id, op.Version, op.Datos.Tipo, op.Datos.CodigoRetirado,
        op.Datos.CodigoInstalado, op.Datos.PesoRetiradoGr, op.Datos.PesoInstaladoGr, op.Datos.Observacion, op.Estado,
        op.Fotos.Values.Select(f => f.Info).ToArray(), op.Actualizada, op.EntregaInformada, op.Recibido, op.UsuarioRecepcion, op.FechaRecepcion,
        op.PesoRecepcionGr, op.ObservacionRecepcion);
    private static OtResponse Ver(Orden o) => new(o.Id, o.Codigo, o.Sucursal, o.Direccion, o.Descripcion, o.Tecnico, o.Estado, o.Autorizador,
        o.Fecha, o.Detalle.ToArray(), o.Retirar.ToArray(), o.Entregas.ToArray(), o.Operaciones.Values.Select(VerOperacion).ToArray(), o.Incidencias.ToArray(), o.Historial.ToArray(), o.ObservacionTermino,
        o.AprobacionBodega, o.RevisionAutorizador);
    private sealed class Orden
    {
        public Guid Id; public string Codigo = "", Sucursal = "", Direccion = "", Descripcion = "", Tecnico = "", Autorizador = "", Estado = "Asignada";
        public DateTimeOffset Fecha = DateTimeOffset.UtcNow; public string? ObservacionTermino;
        public RevisionTrabajoResponse? AprobacionBodega, RevisionAutorizador;
        public LineaOtResponse[] Detalle = []; public string[] Retirar = [];
        public Dictionary<Guid, (string Huella, FotoGuardada Foto)> Despachos = [];
        public List<EntregaResponse> Entregas = []; public Dictionary<Guid, Operacion> Operaciones = [];
        public List<IncidenciaResponse> Incidencias = []; public List<EventoResponse> Historial = [];
    }
    private sealed class Operacion
    {
        public GuardarOperacionRequest Datos = new(); public int Version; public string Estado = "Borrador";
        public DateTimeOffset Actualizada; public Dictionary<Guid, FotoGuardada> Fotos = [];
        public bool EntregaInformada, Recibido; public string? UsuarioRecepcion; public DateTimeOffset? FechaRecepcion;
        public long? PesoRecepcionGr; public string? ObservacionRecepcion;
    }
    private sealed record FotoGuardada(FotoResponse Info, byte[] Bytes);
}
