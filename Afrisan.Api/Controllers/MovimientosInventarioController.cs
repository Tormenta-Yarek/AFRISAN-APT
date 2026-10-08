using Afrisan.Api.Data;
using Afrisan.Api.DTOs;
using Afrisan.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Afrisan.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MovimientosInventarioController : ControllerBase
    {
        private readonly AfrisanDbContext _context;

        public MovimientosInventarioController(
            AfrisanDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // DESPACHO DE CILINDRO
        // POST: api/MovimientosInventario/despacho/6
        // Solo Administrador / Bodega
        // =========================================================

        [HttpPost("despacho/{cilindroId:int}")]
        [Authorize(Roles = "Administrador,Bodega")]
        public async Task<IActionResult> DespacharCilindro(
            int cilindroId,
            [FromBody] DespachoCilindroDto dto)
        {
            // 1. Buscar el cilindro.
            var cilindro = await _context.Cilindros
                .FirstOrDefaultAsync(
                    c => c.Id == cilindroId);

            if (cilindro == null)
            {
                return NotFound(new
                {
                    mensaje =
                        $"No se encontró el cilindro con ID {cilindroId}."
                });
            }

            // 2. Comprobar que esté disponible.
            if (!string.Equals(
                    cilindro.Estado?.Trim(),
                    "Disponible",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    mensaje =
                        "Solo se pueden despachar cilindros disponibles."
                });
            }

            // 3. Validar Orden de Trabajo.
            if (!dto.OrdenTrabajoId.HasValue)
            {
                return BadRequest(new
                {
                    mensaje =
                        "Debes seleccionar una Orden de Trabajo."
                });
            }

            var ordenTrabajo =
                await _context.OrdenesTrabajo
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o =>
                        o.Id == dto.OrdenTrabajoId.Value);

            if (ordenTrabajo == null)
            {
                return BadRequest(new
                {
                    mensaje =
                        "La Orden de Trabajo seleccionada no existe."
                });
            }

            if (!ordenTrabajo.Activa)
            {
                return BadRequest(new
                {
                    mensaje =
                        "La Orden de Trabajo seleccionada no está activa."
                });
            }

            // 4. Validar responsable.
            if (string.IsNullOrWhiteSpace(
                dto.Responsable))
            {
                return BadRequest(new
                {
                    mensaje =
                        "Debes indicar quién retira el cilindro."
                });
            }

            // 5. Validar peso.
            if (!dto.PesoKg.HasValue ||
                dto.PesoKg.Value < 0)
            {
                return BadRequest(new
                {
                    mensaje =
                        "Debes ingresar un peso válido, igual o mayor que cero."
                });
            }

            // 6. Validar tara.
            if (!cilindro.TaraKg.HasValue ||
                cilindro.TaraKg.Value <= 0)
            {
                return BadRequest(new
                {
                    mensaje =
                        "El cilindro no tiene una tara registrada. " +
                        "Debes registrar la tara antes de realizar el despacho."
                });
            }

            var tara =
                cilindro.TaraKg.Value;

            var pesoDespacho =
                dto.PesoKg.Value;

            // 7. Peso debe ser mayor que tara.
            if (pesoDespacho <= tara)
            {
                return BadRequest(new
                {
                    mensaje =
                        $"El peso bruto de despacho debe ser mayor " +
                        $"que la tara del cilindro ({tara:0.##} kg)."
                });
            }

            // 8. Peso máximo.
            var pesoBrutoMaximo =
                tara + cilindro.CapacidadKg;

            if (pesoDespacho >
                pesoBrutoMaximo)
            {
                return BadRequest(new
                {
                    mensaje =
                        $"El peso bruto de despacho no puede superar " +
                        $"tara + capacidad ({pesoBrutoMaximo:0.##} kg)."
                });
            }

            // 9. Crear movimiento.
            var movimiento =
                new MovimientoInventario
                {
                    CilindroId =
                        cilindro.Id,

                    OrdenTrabajoId =
                        ordenTrabajo.Id,

                    TipoMovimiento =
                        "Despacho",

                    PesoKg =
                        pesoDespacho,

                    Responsable =
                        dto.Responsable.Trim(),

                    FechaMovimiento =
                        DateTime.UtcNow,

                    Observaciones =
                        dto.Observaciones
                };

            // 10. Actualizar cilindro.
            cilindro.Estado =
                "Despachado";

            cilindro.PesoActualKg =
                pesoDespacho;

            // 11. Guardar.
            _context.MovimientosInventario
                .Add(movimiento);

            await _context.SaveChangesAsync();

            // 12. Respuesta.
            return Ok(new
            {
                mensaje =
                    "Cilindro despachado correctamente.",

                cilindroId =
                    cilindro.Id,

                codigoQr =
                    cilindro.CodigoQr,

                ordenTrabajoId =
                    ordenTrabajo.Id,

                ordenTrabajo =
                    ordenTrabajo.Codigo,

                estado =
                    cilindro.Estado,

                taraKg =
                    cilindro.TaraKg,

                movimientoId =
                    movimiento.Id,

                pesoDespachoKg =
                    movimiento.PesoKg,

                responsable =
                    movimiento.Responsable,

                fechaMovimiento =
                    movimiento.FechaMovimiento
            });
        }

        // =========================================================
        // RETORNO DE CILINDRO
        // POST: api/MovimientosInventario/retorno/6
        // Solo Administrador / Bodega
        // =========================================================

        [HttpPost("retorno/{cilindroId:int}")]
        [Authorize(Roles = "Administrador,Bodega")]
        public async Task<IActionResult> RetornarCilindro(
            int cilindroId,
            [FromBody] RetornoCilindroDto dto)
        {
            // 1. Buscar cilindro.
            var cilindro =
                await _context.Cilindros
                    .FirstOrDefaultAsync(
                        c => c.Id == cilindroId);

            if (cilindro == null)
            {
                return NotFound(new
                {
                    mensaje =
                        $"No se encontró el cilindro con ID {cilindroId}."
                });
            }

            // 2. Comprobar estado.
            if (!string.Equals(
                    cilindro.Estado?.Trim(),
                    "Despachado",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    mensaje =
                        "Solo se pueden retornar cilindros despachados."
                });
            }

            // 3. Responsable.
            if (string.IsNullOrWhiteSpace(
                dto.Responsable))
            {
                return BadRequest(new
                {
                    mensaje =
                        "Debes indicar quién devuelve el cilindro."
                });
            }

            // 4. Peso.
            if (!dto.PesoKg.HasValue ||
                dto.PesoKg.Value < 0)
            {
                return BadRequest(new
                {
                    mensaje =
                        "Debes ingresar un peso válido, igual o mayor que cero."
                });
            }

            // 5. Peso retorno >= tara.
            if (cilindro.TaraKg.HasValue &&
                cilindro.TaraKg.Value > 0 &&
                dto.PesoKg.Value <
                cilindro.TaraKg.Value)
            {
                return BadRequest(new
                {
                    mensaje =
                        $"El peso bruto de retorno no puede ser menor " +
                        $"que la tara del cilindro " +
                        $"({cilindro.TaraKg.Value:0.##} kg)."
                });
            }

            // 6. Crear movimiento.
            var movimiento =
                new MovimientoInventario
                {
                    CilindroId =
                        cilindro.Id,

                    TipoMovimiento =
                        "Retorno",

                    PesoKg =
                        dto.PesoKg.Value,

                    Responsable =
                        dto.Responsable.Trim(),

                    FechaMovimiento =
                        DateTime.UtcNow,

                    Observaciones =
                        dto.Observaciones
                };

            // 7. Actualizar cilindro.
            cilindro.Estado =
                "Disponible";

            cilindro.PesoActualKg =
                dto.PesoKg.Value;

            // 8. Guardar.
            _context.MovimientosInventario
                .Add(movimiento);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje =
                    "Cilindro retornado correctamente.",

                cilindroId =
                    cilindro.Id,

                codigoQr =
                    cilindro.CodigoQr,

                estado =
                    cilindro.Estado,

                movimientoId =
                    movimiento.Id,

                pesoRetornoKg =
                    movimiento.PesoKg,

                responsable =
                    movimiento.Responsable,

                fechaMovimiento =
                    movimiento.FechaMovimiento
            });
        }

        // =========================================================
        // HISTORIAL DE MOVIMIENTOS
        // GET: api/MovimientosInventario/historial/6
        // Todos los roles
        // =========================================================

        [HttpGet("historial/{cilindroId:int}")]
        [Authorize(
            Roles =
                "Administrador,Jefatura,Bodega,Tecnico")]
        public async Task<IActionResult> ObtenerHistorial(
            int cilindroId)
        {
            // 1. Comprobar cilindro.
            var cilindro =
                await _context.Cilindros
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        c => c.Id == cilindroId);

            if (cilindro == null)
            {
                return NotFound(new
                {
                    mensaje =
                        $"No se encontró el cilindro con ID {cilindroId}."
                });
            }

            // 2. Obtener movimientos.
            var movimientos =
                await _context.MovimientosInventario
                    .AsNoTracking()
                    .Where(m =>
                        m.CilindroId ==
                        cilindroId)
                    .OrderByDescending(m =>
                        m.FechaMovimiento)
                    .ThenByDescending(m =>
                        m.Id)
                    .Select(m => new
                    {
                        m.Id,

                        m.TipoMovimiento,

                        m.PesoKg,

                        m.Responsable,

                        m.FechaMovimiento,

                        m.Observaciones,

                        ordenTrabajoId =
                            m.OrdenTrabajoId,

                        ordenTrabajo =
                            m.OrdenTrabajo != null
                                ? m.OrdenTrabajo.Codigo
                                : null
                    })
                    .ToListAsync();

            // 3. Respuesta.
            return Ok(new
            {
                cilindroId =
                    cilindro.Id,

                codigoQr =
                    cilindro.CodigoQr,

                estadoActual =
                    cilindro.Estado,

                taraKg =
                    cilindro.TaraKg,

                totalMovimientos =
                    movimientos.Count,

                movimientos
            });
        }

        // =========================================================
        // BALANCE DE MASA
        // GET: api/MovimientosInventario/balance/6
        // Administrador / Jefatura / Bodega
        // =========================================================

        [HttpGet("balance/{cilindroId:int}")]
        [Authorize(
            Roles =
                "Administrador,Jefatura,Bodega")]
        public async Task<IActionResult> ObtenerBalanceMasa(
            int cilindroId)
        {
            // 1. Comprobar cilindro.
            var cilindro =
                await _context.Cilindros
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c =>
                        c.Id == cilindroId);

            if (cilindro == null)
            {
                return NotFound(new
                {
                    mensaje =
                        $"No se encontró el cilindro con ID {cilindroId}."
                });
            }

            // 2. Obtener movimientos.
            var movimientos =
                await _context.MovimientosInventario
                    .AsNoTracking()
                    .Where(m =>
                        m.CilindroId ==
                        cilindroId &&
                        (
                            m.TipoMovimiento ==
                            "Despacho"
                            ||
                            m.TipoMovimiento ==
                            "Retorno"
                        ))
                    .OrderByDescending(m =>
                        m.FechaMovimiento)
                    .ThenByDescending(m =>
                        m.Id)
                    .ToListAsync();

            // 3. Retorno más reciente.
            var retorno =
                movimientos.FirstOrDefault(m =>
                    string.Equals(
                        m.TipoMovimiento,
                        "Retorno",
                        StringComparison.OrdinalIgnoreCase));

            if (retorno == null)
            {
                return BadRequest(new
                {
                    mensaje =
                        "El cilindro todavía no tiene un retorno registrado."
                });
            }

            // 4. Despacho anterior.
            var despacho =
                movimientos
                    .Where(m =>
                        string.Equals(
                            m.TipoMovimiento,
                            "Despacho",
                            StringComparison.OrdinalIgnoreCase)
                        &&
                        (
                            m.FechaMovimiento <
                            retorno.FechaMovimiento
                            ||
                            (
                                m.FechaMovimiento ==
                                retorno.FechaMovimiento
                                &&
                                m.Id <
                                retorno.Id
                            )
                        ))
                    .OrderByDescending(m =>
                        m.FechaMovimiento)
                    .ThenByDescending(m =>
                        m.Id)
                    .FirstOrDefault();

            if (despacho == null)
            {
                return BadRequest(new
                {
                    mensaje =
                        "No se encontró un despacho anterior " +
                        "asociado al retorno."
                });
            }

            // 5. Diferencia.
            var diferenciaKg =
                despacho.PesoKg -
                retorno.PesoKg;

            var balanceValido =
                diferenciaKg >= 0;

            // 6. Refrigerante al despacho.
            decimal?
                refrigeranteDespachoKg =
                    null;

            if (cilindro.TaraKg.HasValue &&
                cilindro.TaraKg.Value > 0)
            {
                refrigeranteDespachoKg =
                    despacho.PesoKg -
                    cilindro.TaraKg.Value;
            }

            // 7. Respuesta.
            return Ok(new
            {
                cilindroId =
                    cilindro.Id,

                codigoQr =
                    cilindro.CodigoQr,

                taraKg =
                    cilindro.TaraKg,

                despachoId =
                    despacho.Id,

                ordenTrabajoId =
                    despacho.OrdenTrabajoId,

                pesoDespachoKg =
                    despacho.PesoKg,

                fechaDespacho =
                    despacho.FechaMovimiento,

                retornoId =
                    retorno.Id,

                pesoRetornoKg =
                    retorno.PesoKg,

                fechaRetorno =
                    retorno.FechaMovimiento,

                diferenciaKg,

                refrigeranteDespachoKg,

                balanceValido,

                estadoBalance =
                    balanceValido
                        ? "Balance de masa correcto."
                        : "Inconsistencia: el peso de retorno " +
                          "es mayor que el peso de despacho."
            });
        }
    }
}