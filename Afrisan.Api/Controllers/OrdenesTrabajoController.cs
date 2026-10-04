using Afrisan.Api.Data;
using Afrisan.Api.DTOs;
using Afrisan.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Afrisan.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdenesTrabajoController : ControllerBase
    {
        private readonly AfrisanDbContext _context;

        public OrdenesTrabajoController(AfrisanDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // LISTAR ÓRDENES DE TRABAJO
        // GET: api/OrdenesTrabajo
        // =========================================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrdenTrabajo>>> ObtenerOrdenes()
        {
            var ordenes = await _context.OrdenesTrabajo
                .AsNoTracking()
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return Ok(ordenes);
        }

        // =========================================================
        // LISTAR ÓRDENES DE TRABAJO ACTIVAS
        // GET: api/OrdenesTrabajo/activas
        // =========================================================
        [HttpGet("activas")]
        public async Task<ActionResult<IEnumerable<OrdenTrabajo>>> ObtenerActivas()
        {
            var ordenes = await _context.OrdenesTrabajo
                .AsNoTracking()
                .Where(o => o.Activa)
                .OrderBy(o => o.Codigo)
                .ToListAsync();

            return Ok(ordenes);
        }

        // =========================================================
        // OBTENER ORDEN DE TRABAJO POR ID
        // GET: api/OrdenesTrabajo/1
        // =========================================================
        [HttpGet("{id:int}")]
        public async Task<ActionResult<OrdenTrabajo>> ObtenerPorId(int id)
        {
            var orden = await _context.OrdenesTrabajo
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (orden == null)
            {
                return NotFound(new
                {
                    mensaje =
                        $"No se encontró la Orden de Trabajo con ID {id}."
                });
            }

            return Ok(orden);
        }

        // =========================================================
        // CREAR ORDEN DE TRABAJO
        // POST: api/OrdenesTrabajo
        // =========================================================
        [HttpPost]
        public async Task<ActionResult<OrdenTrabajo>> CrearOrden(
            [FromBody] CrearOrdenTrabajoDto dto)
        {
            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // El código se genera automáticamente.
                // El usuario no puede inventarlo manualmente.
                var ultimoId = await _context.OrdenesTrabajo
                    .MaxAsync(o => (int?)o.Id) ?? 0;

                var siguienteNumero = ultimoId + 1;

                string codigo;

                do
                {
                    codigo = $"OT-{siguienteNumero:D6}";

                    siguienteNumero++;
                }
                while (await _context.OrdenesTrabajo
                    .AnyAsync(o => o.Codigo == codigo));

                var orden = new OrdenTrabajo
                {
                    Codigo = codigo,

                    Descripcion =
                        string.IsNullOrWhiteSpace(dto.Descripcion)
                            ? null
                            : dto.Descripcion.Trim(),

                    Activa = true,

                    FechaCreacion = DateTime.UtcNow
                };

                _context.OrdenesTrabajo.Add(orden);

                await _context.SaveChangesAsync();

                await transaccion.CommitAsync();

                return CreatedAtAction(
                    nameof(ObtenerPorId),
                    new { id = orden.Id },
                    orden
                );
            }
            catch
            {
                await transaccion.RollbackAsync();

                throw;
            }
        }

        // =========================================================
        // DESACTIVAR ORDEN DE TRABAJO
        // PUT: api/OrdenesTrabajo/1/desactivar
        // =========================================================
        [HttpPut("{id:int}/desactivar")]
        public async Task<IActionResult> DesactivarOrden(int id)
        {
            var orden = await _context.OrdenesTrabajo
                .FirstOrDefaultAsync(o => o.Id == id);

            if (orden == null)
            {
                return NotFound(new
                {
                    mensaje =
                        $"No se encontró la Orden de Trabajo con ID {id}."
                });
            }

            if (!orden.Activa)
            {
                return BadRequest(new
                {
                    mensaje =
                        "La Orden de Trabajo ya se encuentra inactiva."
                });
            }

            orden.Activa = false;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje =
                    "Orden de Trabajo desactivada correctamente.",

                orden.Id,

                orden.Codigo,

                orden.Activa
            });
        }
    }
}