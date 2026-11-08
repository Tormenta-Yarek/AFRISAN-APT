using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Afrisan.Nuevo.Contratos;
using Afrisan.Nuevo.Web.Models;
using Afrisan.Nuevo.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace Afrisan.Nuevo.Web.Pages;

public partial class OperacionTecnica : IAsyncDisposable
{
    [Parameter] public Guid Id { get; set; }
    [Parameter] public Guid OperacionId { get; set; }
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private InventarioClient Api { get; set; } = default!;
    [Inject] private TrabajoLocalService Local { get; set; } = default!;
    [Inject] private SesionService Sesion { get; set; } = default!;
    private OtResponse? ot;
    private OperacionResponse? versionDisponible;
    private CambioForm form = new();
    private EditContext? contexto;
    private readonly List<SubirFotoRequest> fotos = [];
    private readonly HashSet<Guid> fotosRemotas = [];
    private readonly HashSet<Guid> eliminar = [];
    private readonly SemaphoreSlim guardado = new(1, 1);
    private bool cargando = true, ocupado, registrada, pendienteConfirmar, listo, disposed;
    private int version;
    private string instancia = "", estadoLocal = "Sin guardar", tipoAnterior = "Reemplazo";
    private string? error, mensaje;

    protected override async Task OnParametersSetAsync()
    {
        cargando = true; listo = false; error = null;
        Local.Editar(OperacionId);
        try
        {
            ot = await Local.OrdenAsync(Id);
            var local = (await Local.BorradoresAsync()).FirstOrDefault(b => b.OtId == Id && b.Datos.Id == OperacionId);
            var remoto = ot.Operaciones.FirstOrDefault(o => o.Id == OperacionId);
            if (remoto?.Estado == "Descartada") throw new InvalidOperationException("Este borrador fue descartado. Vuelve a la OT para registrar otro trabajo.");
            if (remoto?.Estado == "Registrada") { registrada = true; return; }
            instancia = local?.Datos.Instancia ?? Sesion.Entorno?.Instancia ?? "";
            if (string.IsNullOrWhiteSpace(instancia)) throw new InvalidOperationException("Abre la OT con conexión antes de preparar un borrador offline.");
            var d = local?.Datos ?? new GuardarOperacionRequest { Id = OperacionId, Instancia = instancia, Version = remoto?.Version ?? 0,
                Tipo = remoto?.Tipo ?? "Reemplazo", CodigoRetirado = remoto?.CodigoRetirado, CodigoInstalado = remoto?.CodigoInstalado,
                PesoRetiradoGr = remoto?.PesoRetiradoGr, PesoInstaladoGr = remoto?.PesoInstaladoGr, Observacion = remoto?.Observacion };
            version = d.Version; tipoAnterior = d.Tipo;
            form = new() { Tipo = d.Tipo, CodigoRetirado = d.CodigoRetirado ?? "", CodigoInstalado = d.CodigoInstalado ?? "",
                PesoRetiradoGr = d.PesoRetiradoGr, PesoInstaladoGr = d.PesoInstaladoGr, Observacion = d.Observacion };
            fotos.Clear(); eliminar.Clear(); fotosRemotas.Clear();
            if (local is not null) { fotos.AddRange(local.Fotos); eliminar.UnionWith(local.FotosEliminar); pendienteConfirmar = local.ConfirmarAlSincronizar; error = local.Error; }
            if (remoto is not null)
            {
                foreach (var foto in remoto.Fotos)
                {
                    fotosRemotas.Add(foto.Id);
                    if (eliminar.Contains(foto.Id) || fotos.Any(f => f.Id == foto.Id)) continue;
                    var data = await Api.FotoAsync(Id, OperacionId, foto);
                    fotos.Add(new() { Id = foto.Id, Grupo = foto.Grupo, Nombre = foto.Nombre, ContentType = foto.ContentType, Base64 = data[(data.IndexOf(',') + 1)..] });
                }
            }
            if (contexto is not null) contexto.OnFieldChanged -= CampoCambiado;
            contexto = new(form); contexto.OnFieldChanged += CampoCambiado;
            estadoLocal = local is null ? "Borrador nuevo" : local.Pendiente ? "Conservado en este dispositivo; pendiente de envío" : "Borrador sincronizado";
            listo = true;
        }
        catch (Exception ex) { error = ex is JSException ? "No se pudo acceder al almacenamiento local. No se considera guardado el trabajo." : Formato.Error(ex); }
        finally { cargando = false; }
    }

    private GuardarOperacionRequest Datos() => new() { Id = OperacionId, Instancia = instancia, Version = version, Tipo = form.Tipo,
        CodigoRetirado = form.Tipo == "Agregado" ? null : form.CodigoRetirado, CodigoInstalado = form.CodigoInstalado,
        PesoRetiradoGr = form.Tipo == "Agregado" ? null : form.PesoRetiradoGr, PesoInstaladoGr = form.PesoInstaladoGr, Observacion = form.Observacion };
    private async Task GuardarLocal()
    {
        if (!listo || registrada) return;
        await guardado.WaitAsync();
        try
        {
            await Local.GuardarLocalAsync(new() { Usuario = Sesion.Sesion!.Usuario, OtId = Id, Datos = Datos(), Fotos = fotos.ToArray(),
                FotosEliminar = eliminar.ToArray(), ConfirmarAlSincronizar = pendienteConfirmar, Pendiente = true });
            estadoLocal = $"Guardado en este dispositivo a las {DateTimeOffset.Now:HH:mm:ss}";
        }
        finally { guardado.Release(); }
    }
    private async void CampoCambiado(object? sender, FieldChangedEventArgs args)
    {
        if (!listo || ocupado || disposed) return;
        try
        {
            if (form.Tipo != tipoAnterior)
            {
                if (fotos.Count > 0) { form.Tipo = tipoAnterior; error = "Retira las fotos del borrador antes de cambiar el tipo de trabajo."; }
                else { tipoAnterior = form.Tipo; if (form.Tipo == "Agregado") { form.CodigoRetirado = ""; form.PesoRetiradoGr = null; } }
            }
            pendienteConfirmar = false;
            await GuardarLocal();
        }
        catch (Exception ex) { error = "No se pudo guardar localmente: " + ex.Message; estadoLocal = "Cambios sin guardar"; }
        if (!disposed) await InvokeAsync(StateHasChanged);
    }
    private async Task GuardarSoloLocal()
    {
        ocupado = true; error = null;
        try { pendienteConfirmar = false; await GuardarLocal(); mensaje = "Borrador y fotos conservados en este navegador. Aún no se confirmó el trabajo en la API."; }
        catch (Exception ex) { error = "No se pudo guardar localmente: " + ex.Message; }
        finally { ocupado = false; }
    }
    private async Task Descartar()
    {
        if (ocupado) return; ocupado = true; error = null;
        try { await Local.DescartarAsync(Id, OperacionId); Navigation.NavigateTo($"ordenes/{Id}"); }
        catch (Exception ex) { error = Formato.Error(ex); }
        finally { ocupado = false; }
    }
    private Task GuardarYEnviar() => Enviar(false);
    private Task Confirmar() => Enviar(true);
    private async Task Enviar(bool confirmar)
    {
        if (ocupado || !listo) return;
        ocupado = true; error = null; mensaje = null;
        try
        {
            if (confirmar)
            {
                if (string.IsNullOrWhiteSpace(form.CodigoInstalado) || form.PesoInstaladoGr is null || !fotos.Any(f => f.Grupo == "Instalado")) throw new InvalidOperationException("Falta cilindro instalado, peso o foto de su pesaje.");
                if (form.Tipo == "Reemplazo" && (string.IsNullOrWhiteSpace(form.CodigoRetirado) || form.PesoRetiradoGr is null || !fotos.Any(f => f.Grupo == "Retirado"))) throw new InvalidOperationException("El reemplazo exige también cilindro retirado, su peso y fotos de balanza.");
            }
            pendienteConfirmar = confirmar;
            await GuardarLocal();
            await Local.SincronizarAsync(OperacionId);
            var local = (await Local.BorradoresAsync()).First(b => b.Datos.Id == OperacionId);
            version = local.Datos.Version;
            if (local.Error is not null) error = local.Error;
            if (local.Pendiente) { mensaje = "Conservado localmente, pendiente de sincronizar. " + Local.Mensaje; estadoLocal = "Pendiente de sincronización"; }
            else
            {
                eliminar.Clear(); fotosRemotas.UnionWith(fotos.Select(f => f.Id));
                registrada = confirmar; estadoLocal = "Sincronizado con la API";
                mensaje = confirmar ? "Cambio confirmado con sus pesajes y evidencias. Vuelve a la OT para continuar." : "Borrador y fotos guardados en la API; aún puedes revisarlo antes de confirmar.";
            }
        }
        catch (Exception ex) { error = Formato.Error(ex); }
        finally { ocupado = false; }
    }

    private async Task AgregarFotos(InputFileChangeEventArgs args, string grupo)
    {
        if (ocupado || !listo) return;
        ocupado = true; error = null;
        try
        {
            var codigo = grupo == "Retirado" ? form.CodigoRetirado : form.CodigoInstalado;
            if (string.IsNullOrWhiteSpace(codigo)) throw new InvalidOperationException("Selecciona el cilindro antes de adjuntar sus fotos.");
            var archivos = args.GetMultipleFiles(8);
            if (fotos.Count(f => f.Grupo == grupo) + archivos.Count > 8) throw new InvalidOperationException("Máximo 8 fotos por grupo. Puedes agregar más de una en cada selección.");
            foreach (var archivo in archivos)
            {
                if (archivo.ContentType is not ("image/jpeg" or "image/png" or "image/webp")) throw new InvalidOperationException("Usa una imagen JPEG, PNG o WebP.");
                await using var lectura = archivo.OpenReadStream(4 * 1024 * 1024);
                using var memoria = new MemoryStream(); await lectura.CopyToAsync(memoria);
                fotos.Add(new() { Id = Guid.NewGuid(), Grupo = grupo, Nombre = archivo.Name.Length > 150 ? archivo.Name[..150] : archivo.Name,
                    ContentType = archivo.ContentType, Base64 = Convert.ToBase64String(memoria.ToArray()) });
                pendienteConfirmar = false;
                await GuardarLocal();
            }
            mensaje = "Fotografías guardadas en este dispositivo. Confirma o sincroniza cuando estés listo.";
        }
        catch (Exception ex) { error = ex.Message; }
        finally { ocupado = false; }
    }
    private async Task QuitarFoto(SubirFotoRequest foto)
    {
        ocupado = true; error = null;
        try { if (fotosRemotas.Contains(foto.Id)) eliminar.Add(foto.Id); fotos.Remove(foto); pendienteConfirmar = false; await GuardarLocal(); }
        catch (Exception ex) { error = "No se pudo conservar el cambio: " + ex.Message; }
        finally { ocupado = false; }
    }
    private async Task ConsultarVersion()
    {
        ocupado = true; error = null;
        try { versionDisponible = (await Api.OrdenAsync(Id)).Operaciones.FirstOrDefault(o => o.Id == OperacionId) ?? throw new InvalidOperationException("El borrador todavía no existe en la API."); }
        catch (Exception ex) { error = Formato.Error(ex); }
        finally { ocupado = false; }
    }
    private async Task UsarVersionComoBase()
    {
        if (versionDisponible?.Estado != "Borrador") return;
        ocupado = true; error = null;
        try { version = versionDisponible.Version; pendienteConfirmar = false; await GuardarLocal(); mensaje = "Nueva base de versión guardada. Revisa y sincroniza tus datos explícitamente."; versionDisponible = null; }
        catch (Exception ex) { error = ex.Message; }
        finally { ocupado = false; }
    }
    public ValueTask DisposeAsync()
    {
        disposed = true; listo = false;
        if (contexto is not null) contexto.OnFieldChanged -= CampoCambiado;
        Local.DejarEditar(OperacionId);
        return ValueTask.CompletedTask;
    }
    private sealed class CambioForm
    {
        [Required] public string Tipo { get; set; } = "Reemplazo";
        public string CodigoRetirado { get; set; } = "";
        public string CodigoInstalado { get; set; } = "";
        [Range(0, long.MaxValue)] public long? PesoRetiradoGr { get; set; }
        [Range(0, long.MaxValue)] public long? PesoInstaladoGr { get; set; }
        [StringLength(2000)] public string? Observacion { get; set; }
    }
}
