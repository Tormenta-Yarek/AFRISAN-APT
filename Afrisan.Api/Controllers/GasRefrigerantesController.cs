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
    public class GasRefrigerantesController : ControllerBase
    {
        private readonly AfrisanDbContext _context;

        public GasRefrigerantesController(
            AfrisanDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // LISTAR GASES REFRIGERANTES
        // GET: api/GasRefrigerantes
        // Administrador / Jefatura / Bodega
        // =========================================================

        [HttpGet]
        [Authorize(Roles = "Administrador,Jefatura,Bodega")]
        public async Task<ActionResult<IEnumerable<GasRefrigerante>>>
            GetGases()
        {
            var gases =
                await _context.GasesRefrigerantes
                    .AsNoTracking()
                    .OrderBy(g => g.Id)
                    .ToListAsync();

            return Ok(gases);
        }

        // =========================================================
        // OBTENER GAS POR ID
        // GET: api/GasRefrigerantes/1
        // Administrador / Jefatura / Bodega
        // =========================================================

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Administrador,Jefatura,Bodega")]
        public async Task<ActionResult<GasRefrigerante>>
            GetGas(int id)
        {
            var gas =
                await _context.GasesRefrigerantes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        g => g.Id == id);

            if (gas == null)
            {
                return NotFound(new
                {
                    mensaje =
                        $"No se encontró el gas refrigerante con ID {id}."
                });
            }

            return Ok(gas);
        }

        // =========================================================
        // CREAR GAS REFRIGERANTE
        // POST: api/GasRefrigerantes
        // Solo Administrador
        // =========================================================

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<GasRefrigerante>>
            CrearGas(
                [FromBody] GasRefrigeranteDto dto)
        {
            var codigo =
                dto.Codigo?.Trim();

            var nombre =
                dto.Nombre?.Trim();

            if (string.IsNullOrWhiteSpace(codigo))
            {
                return BadRequest(new
                {
                    mensaje =
                        "El código del gas es obligatorio."
                });
            }

            if (string.IsNullOrWhiteSpace(nombre))
            {
                return BadRequest(new
                {
                    mensaje =
                        "El nombre del gas es obligatorio."
                });
            }

            var existeCodigo =
                await _context.GasesRefrigerantes
                    .AnyAsync(g =>
                        g.Codigo == codigo);

            if (existeCodigo)
            {
                return BadRequest(new
                {
                    mensaje =
                        $"Ya existe un gas con el código {codigo}."
                });
            }

            var gas =
                new GasRefrigerante
                {
                    Nombre =
                        nombre,

                    Codigo =
                        codigo,

                    Descripcion =
                        string.IsNullOrWhiteSpace(
                            dto.Descripcion)
                            ? string.Empty
                            : dto.Descripcion.Trim(),

                    Activo =
                        dto.Activo
                };

            _context.GasesRefrigerantes
                .Add(gas);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetGas),
                new
                {
                    id = gas.Id
                },
                gas);
        }

        // =========================================================
        // ACTUALIZAR GAS REFRIGERANTE
        // PUT: api/GasRefrigerantes/1
        // Solo Administrador
        // =========================================================

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult>
            ActualizarGas(
                int id,
                [FromBody] GasRefrigeranteDto dto)
        {
            var gas =
                await _context.GasesRefrigerantes
                    .FindAsync(id);

            if (gas == null)
            {
                return NotFound(new
                {
                    mensaje =
                        $"No se encontró el gas refrigerante con ID {id}."
                });
            }

            var codigo =
                dto.Codigo?.Trim();

            var nombre =
                dto.Nombre?.Trim();

            if (string.IsNullOrWhiteSpace(codigo))
            {
                return BadRequest(new
                {
                    mensaje =
                        "El código del gas es obligatorio."
                });
            }

            if (string.IsNullOrWhiteSpace(nombre))
            {
                return BadRequest(new
                {
                    mensaje =
                        "El nombre del gas es obligatorio."
                });
            }

            var codigoDuplicado =
                await _context.GasesRefrigerantes
                    .AnyAsync(g =>
                        g.Codigo == codigo &&
                        g.Id != id);

            if (codigoDuplicado)
            {
                return BadRequest(new
                {
                    mensaje =
                        $"Ya existe otro gas con el código {codigo}."
                });
            }

            gas.Nombre =
                nombre;

            gas.Codigo =
                codigo;

            gas.Descripcion =
                string.IsNullOrWhiteSpace(
                    dto.Descripcion)
                    ? string.Empty
                    : dto.Descripcion.Trim();

            gas.Activo =
                dto.Activo;

            await _context.SaveChangesAsync();

            return Ok(gas);
        }

        // =========================================================
        // ELIMINAR GAS REFRIGERANTE
        // DELETE: api/GasRefrigerantes/1
        // Solo Administrador
        // =========================================================

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult>
            EliminarGas(int id)
        {
            var gas =
                await _context.GasesRefrigerantes
                    .FindAsync(id);

            if (gas == null)
            {
                return NotFound(new
                {
                    mensaje =
                        $"No se encontró el gas refrigerante con ID {id}."
                });
            }

            var estaEnUso =
                await _context.Cilindros
                    .AnyAsync(c =>
                        c.GasRefrigeranteId == id);

            if (estaEnUso)
            {
                return BadRequest(new
                {
                    mensaje =
                        "No se puede eliminar el gas porque está asociado " +
                        "a uno o más cilindros."
                });
            }

            _context.GasesRefrigerantes
                .Remove(gas);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje =
                    $"Gas refrigerante con ID {id} eliminado correctamente."
            });
        }
    }
}