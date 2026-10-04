using Afrisan.Api.Data;
using Afrisan.Api.DTOs;
using Afrisan.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Afrisan.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GasRefrigerantesController : ControllerBase
    {
        private readonly AfrisanDbContext _context;

        public GasRefrigerantesController(AfrisanDbContext context)
        {
            _context = context;
        }

        // GET: api/GasRefrigerantes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GasRefrigerante>>> GetGases()
        {
            var gases = await _context.GasesRefrigerantes
                .OrderBy(g => g.Id)
                .ToListAsync();

            return Ok(gases);
        }

        // GET: api/GasRefrigerantes/1
        [HttpGet("{id}")]
        public async Task<ActionResult<GasRefrigerante>> GetGas(int id)
        {
            var gas = await _context.GasesRefrigerantes.FindAsync(id);

            if (gas == null)
            {
                return NotFound(new
                {
                    mensaje = $"No se encontró el gas refrigerante con ID {id}."
                });
            }

            return Ok(gas);
        }

        // POST: api/GasRefrigerantes
        [HttpPost]
        public async Task<ActionResult<GasRefrigerante>> CrearGas(
            GasRefrigeranteDto dto)
        {
            var existeCodigo = await _context.GasesRefrigerantes
                .AnyAsync(g => g.Codigo == dto.Codigo);

            if (existeCodigo)
            {
                return BadRequest(new
                {
                    mensaje = $"Ya existe un gas con el código {dto.Codigo}."
                });
            }

            var gas = new GasRefrigerante
            {
                Nombre = dto.Nombre,
                Codigo = dto.Codigo,
                Descripcion = dto.Descripcion,
                Activo = dto.Activo
            };

            _context.GasesRefrigerantes.Add(gas);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetGas),
                new { id = gas.Id },
                gas
            );
        }

        // PUT: api/GasRefrigerantes/1
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarGas(
            int id,
            GasRefrigeranteDto dto)
        {
            var gas = await _context.GasesRefrigerantes.FindAsync(id);

            if (gas == null)
            {
                return NotFound(new
                {
                    mensaje = $"No se encontró el gas refrigerante con ID {id}."
                });
            }

            var codigoDuplicado = await _context.GasesRefrigerantes
                .AnyAsync(g =>
                    g.Codigo == dto.Codigo &&
                    g.Id != id);

            if (codigoDuplicado)
            {
                return BadRequest(new
                {
                    mensaje = $"Ya existe otro gas con el código {dto.Codigo}."
                });
            }

            gas.Nombre = dto.Nombre;
            gas.Codigo = dto.Codigo;
            gas.Descripcion = dto.Descripcion;
            gas.Activo = dto.Activo;

            await _context.SaveChangesAsync();

            return Ok(gas);
        }

        // DELETE: api/GasRefrigerantes/1
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarGas(int id)
        {
            var gas = await _context.GasesRefrigerantes.FindAsync(id);

            if (gas == null)
            {
                return NotFound(new
                {
                    mensaje = $"No se encontró el gas refrigerante con ID {id}."
                });
            }

            var estaEnUso = await _context.Cilindros
                .AnyAsync(c => c.GasRefrigeranteId == id);

            if (estaEnUso)
            {
                return BadRequest(new
                {
                    mensaje = "No se puede eliminar el gas porque está asociado a uno o más cilindros."
                });
            }

            _context.GasesRefrigerantes.Remove(gas);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = $"Gas refrigerante con ID {id} eliminado correctamente."
            });
        }
    }
}