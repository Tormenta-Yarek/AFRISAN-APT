using Afrisan.Api.Data;
using Afrisan.Api.DTOs;
using Afrisan.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Afrisan.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CilindrosController : ControllerBase
    {
        private readonly AfrisanDbContext _context;

        public CilindrosController(AfrisanDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // GET: api/Cilindros
        // =========================================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cilindro>>> GetCilindros()
        {
            var cilindros = await _context.Cilindros
                .AsNoTracking()
                .Include(c => c.GasRefrigerante)
                .OrderBy(c => c.Id)
                .ToListAsync();

            return Ok(cilindros);
        }


        // =========================================================
        // GET: api/Cilindros/1
        // =========================================================
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Cilindro>> GetCilindro(int id)
        {
            var cilindro = await _context.Cilindros
                .AsNoTracking()
                .Include(c => c.GasRefrigerante)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cilindro is null)
            {
                return NotFound(new
                {
                    mensaje = $"No se encontró el cilindro con ID {id}."
                });
            }

            return Ok(cilindro);
        }


        // =========================================================
        // POST: api/Cilindros
        // =========================================================
        [HttpPost]
        public async Task<ActionResult<Cilindro>> CrearCilindro(
            [FromBody] CrearCilindroDto dto)
        {
            // -----------------------------------------------------
            // 1. VALIDAR GAS
            // -----------------------------------------------------
            if (!dto.GasRefrigeranteId.HasValue)
            {
                return BadRequest(new
                {
                    mensaje = "Debes seleccionar un gas refrigerante."
                });
            }

            var gas = await _context.GasesRefrigerantes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    g => g.Id == dto.GasRefrigeranteId.Value
                );

            if (gas is null || !gas.Activo)
            {
                return BadRequest(new
                {
                    mensaje = "El gas refrigerante no existe o no está activo."
                });
            }


            // -----------------------------------------------------
            // 2. VALIDAR CAPACIDAD
            // -----------------------------------------------------
            if (!dto.CapacidadKg.HasValue ||
                dto.CapacidadKg.Value <= 0)
            {
                return BadRequest(new
                {
                    mensaje = "La capacidad debe ser mayor que cero."
                });
            }


            // -----------------------------------------------------
            // 3. VALIDAR TARA
            // -----------------------------------------------------
            if (!dto.TaraKg.HasValue ||
                dto.TaraKg.Value <= 0)
            {
                return BadRequest(new
                {
                    mensaje = "La tara debe ser mayor que cero."
                });
            }


            // -----------------------------------------------------
            // 4. VALIDAR PESO BRUTO ACTUAL
            // -----------------------------------------------------
            if (!dto.PesoActualKg.HasValue ||
                dto.PesoActualKg.Value < 0)
            {
                return BadRequest(new
                {
                    mensaje = "El peso bruto actual no puede ser negativo."
                });
            }

            if (dto.PesoActualKg.Value < dto.TaraKg.Value)
            {
                return BadRequest(new
                {
                    mensaje =
                        "El peso bruto actual no puede ser menor " +
                        "que la tara del cilindro."
                });
            }

            var pesoBrutoMaximo =
                dto.TaraKg.Value + dto.CapacidadKg.Value;

            if (dto.PesoActualKg.Value > pesoBrutoMaximo)
            {
                return BadRequest(new
                {
                    mensaje =
                        $"El peso bruto actual no puede superar " +
                        $"la tara más la capacidad " +
                        $"({pesoBrutoMaximo:0.##} kg)."
                });
            }


            // -----------------------------------------------------
            // 5. GENERAR CÓDIGO QR OFICIAL
            // -----------------------------------------------------
            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            await _context.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock(20260923)"
            );

            var codigos = await _context.Cilindros
                .AsNoTracking()
                .Select(c => c.CodigoQr)
                .ToListAsync();

            int ultimoNumero = 0;

            foreach (var codigo in codigos)
            {
                if (!Regex.IsMatch(
                    codigo ?? string.Empty,
                    @"^CIL-[0-9]{4}$"))
                {
                    continue;
                }

                var numeroTexto = codigo!.Substring(4);

                if (int.TryParse(numeroTexto, out int numero) &&
                    numero > ultimoNumero)
                {
                    ultimoNumero = numero;
                }
            }

            if (ultimoNumero >= 9999)
            {
                return Conflict(new
                {
                    mensaje =
                        "Se alcanzó el límite del formato de cuatro dígitos."
                });
            }

            var nuevoCodigo =
                $"CIL-{ultimoNumero + 1:D4}";


            // -----------------------------------------------------
            // 6. CREAR CILINDRO
            // -----------------------------------------------------
            var cilindro = new Cilindro
            {
                CodigoQr = nuevoCodigo,

                TipoRefrigerante = gas.Codigo,

                GasRefrigeranteId = gas.Id,

                CapacidadKg = dto.CapacidadKg.Value,

                TaraKg = dto.TaraKg.Value,

                PesoActualKg = dto.PesoActualKg.Value,

                Estado = "Disponible",

                FechaRegistro = DateTime.UtcNow
            };

            _context.Cilindros.Add(cilindro);

            await _context.SaveChangesAsync();

            await transaccion.CommitAsync();

            return CreatedAtAction(
                nameof(GetCilindro),
                new { id = cilindro.Id },
                cilindro
            );
        }


        // =========================================================
        // PUT: api/Cilindros/1
        // =========================================================
        [HttpPut("{id:int}")]
        public async Task<IActionResult> ActualizarCilindro(
            int id,
            [FromBody] CilindroDto dto)
        {
            var cilindro = await _context.Cilindros
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cilindro is null)
            {
                return NotFound(new
                {
                    mensaje = $"No se encontró el cilindro con ID {id}."
                });
            }


            // -----------------------------------------------------
            // CÓDIGO QR NO EDITABLE
            // -----------------------------------------------------
            if (!string.Equals(
                cilindro.CodigoQr,
                dto.CodigoQr,
                StringComparison.Ordinal))
            {
                return BadRequest(new
                {
                    mensaje =
                        "No está permitido modificar el código QR del cilindro."
                });
            }


            // -----------------------------------------------------
            // ESTADO SOLO MEDIANTE MOVIMIENTOS
            // -----------------------------------------------------
            if (!string.Equals(
                cilindro.Estado,
                dto.Estado,
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    mensaje =
                        "No puedes modificar el estado manualmente. " +
                        "Utiliza las operaciones de despacho y retorno."
                });
            }


            // -----------------------------------------------------
            // PESO ACTUAL SOLO MEDIANTE MOVIMIENTOS
            // -----------------------------------------------------
            if (cilindro.PesoActualKg != dto.PesoActualKg)
            {
                return BadRequest(new
                {
                    mensaje =
                        "No puedes modificar el peso actual desde la edición. " +
                        "Debes utilizar una operación de inventario."
                });
            }


            // -----------------------------------------------------
            // VALIDAR GAS
            // -----------------------------------------------------
            if (!dto.GasRefrigeranteId.HasValue)
            {
                return BadRequest(new
                {
                    mensaje = "Debes seleccionar un gas refrigerante."
                });
            }

            var gas = await _context.GasesRefrigerantes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    g => g.Id == dto.GasRefrigeranteId.Value
                );

            if (gas is null || !gas.Activo)
            {
                return BadRequest(new
                {
                    mensaje = "El gas refrigerante no existe o no está activo."
                });
            }


            // -----------------------------------------------------
            // VALIDAR CAPACIDAD
            // -----------------------------------------------------
            if (dto.CapacidadKg <= 0)
            {
                return BadRequest(new
                {
                    mensaje = "La capacidad debe ser mayor que cero."
                });
            }


            // -----------------------------------------------------
            // VALIDAR TARA
            // -----------------------------------------------------
            if (!dto.TaraKg.HasValue ||
                dto.TaraKg.Value <= 0)
            {
                return BadRequest(new
                {
                    mensaje = "Debes ingresar una tara válida mayor que cero."
                });
            }

            var nuevaTara =
                dto.TaraKg.Value;


            // -----------------------------------------------------
            // VALIDAR PESO ACTUAL CONTRA TARA
            // -----------------------------------------------------
            if (cilindro.PesoActualKg < nuevaTara)
            {
                return BadRequest(new
                {
                    mensaje =
                        "El peso actual registrado no puede ser " +
                        "menor que la tara del cilindro."
                });
            }

            var pesoBrutoMaximo =
                nuevaTara + dto.CapacidadKg;

            if (cilindro.PesoActualKg > pesoBrutoMaximo)
            {
                return BadRequest(new
                {
                    mensaje =
                        $"El peso actual supera el máximo permitido " +
                        $"según tara + capacidad " +
                        $"({pesoBrutoMaximo:0.##} kg)."
                });
            }


            // -----------------------------------------------------
            // COMPROBAR MOVIMIENTOS EXISTENTES
            // -----------------------------------------------------
            var tieneMovimientos =
                await _context.MovimientosInventario
                    .AnyAsync(m => m.CilindroId == cilindro.Id);

            var cambiaGas =
                cilindro.GasRefrigeranteId != gas.Id;

            var cambiaCapacidad =
                cilindro.CapacidadKg != dto.CapacidadKg;

            var cambiaTara =
                cilindro.TaraKg != nuevaTara;


            // Si ya tiene movimientos:
            // - no se cambia gas;
            // - no se cambia capacidad;
            // - la tara puede asignarse una sola vez si estaba en null.
            if (tieneMovimientos)
            {
                if (cambiaGas || cambiaCapacidad)
                {
                    return Conflict(new
                    {
                        mensaje =
                            "Este cilindro tiene movimientos registrados. " +
                            "No puedes cambiar su gas ni su capacidad."
                    });
                }

                if (cilindro.TaraKg.HasValue &&
                    cambiaTara)
                {
                    return Conflict(new
                    {
                        mensaje =
                            "Este cilindro tiene movimientos registrados " +
                            "y su tara ya fue definida. " +
                            "No puedes modificarla."
                    });
                }
            }


            // -----------------------------------------------------
            // ACTUALIZAR DATOS TÉCNICOS
            // -----------------------------------------------------
            cilindro.GasRefrigeranteId = gas.Id;

            cilindro.TipoRefrigerante = gas.Codigo;

            cilindro.CapacidadKg = dto.CapacidadKg;

            cilindro.TaraKg = nuevaTara;

            await _context.SaveChangesAsync();

            return Ok(cilindro);
        }


        // =========================================================
        // DELETE: api/Cilindros/1
        // =========================================================
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> EliminarCilindro(int id)
        {
            var cilindro = await _context.Cilindros
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cilindro is null)
            {
                return NotFound(new
                {
                    mensaje = $"No se encontró el cilindro con ID {id}."
                });
            }

            var tieneMovimientos =
                await _context.MovimientosInventario
                    .AnyAsync(m => m.CilindroId == id);

            if (tieneMovimientos)
            {
                return Conflict(new
                {
                    mensaje =
                        "No se puede eliminar un cilindro que tenga " +
                        "movimientos registrados."
                });
            }

            _context.Cilindros.Remove(cilindro);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje =
                    $"Cilindro con ID {id} eliminado correctamente."
            });
        }
    }
}