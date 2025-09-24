using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyNet.Data;
using SkyNet.Models;
using SkyNet.Models.DTOs;

namespace SkyNet.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class GruposApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public GruposApiController(ApplicationDbContext db) => _db = db;

        // GET: api/grupos?sup=123  (opcional filtrar por supervisor)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GrupoItemDto>>> Get([FromQuery] long? sup)
        {
            var q = _db.GruposSupervisoresTec
                .Include(x => x.Supervisor)
                .Include(x => x.Tecnico)
                .AsQueryable();

            if (sup.HasValue && sup.Value > 0)
                q = q.Where(x => x.FkSupervisor == sup.Value);

            var data = await q
                .Where(x => x.Estado)
                .OrderByDescending(x => x.IdGrupo)                
                .Select(x => new GrupoItemDto
                {
                    IdGrupo = x.IdGrupo,
                    SupervisorId = x.FkSupervisor,
                    TecnicoId = x.FkTecnico,
                    SupervisorNombre = x.Supervisor != null ? (x.Supervisor.Nombres + " " + x.Supervisor.Apellidos) : "",
                    TecnicoNombre = x.Tecnico != null ? (x.Tecnico.Nombres + " " + x.Tecnico.Apellidos) : "",
                    FechaCreacionUtc = x.FechaCreacionUtc,
                    Estado = x.Estado
                })
                .ToListAsync();

            return Ok(data);
        }

        // GET: api/grupos/lookups  (para llenar selects)
        [HttpGet("lookups")]
        public async Task<ActionResult<object>> Lookups()
        {
            var supervisores = await _db.Empleados
                .Where(e => e.Estado == 1 && e.Cargo == "Supervisor")
                .OrderBy(e => e.Nombres).ThenBy(e => e.Apellidos)
                .Select(e => new OpcionEmpleadoDto { Id = e.Id, Nombre = e.Nombres + " " + e.Apellidos })
                .ToListAsync();

            var tecnicos = await _db.Empleados
                .Where(e => e.Estado == 1 && e.Cargo == "Tecnico")
                .OrderBy(e => e.Nombres).ThenBy(e => e.Apellidos)
                .Select(e => new OpcionEmpleadoDto { Id = e.Id, Nombre = e.Nombres + " " + e.Apellidos })
                .ToListAsync();

            return Ok(new { supervisores, tecnicos });
        }

        // POST: api/grupos
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] GrupoCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // valida existencia
            var existeSup = await _db.Empleados.AnyAsync(e => e.Id == dto.SupervisorId && e.Estado == 1);
            if (!existeSup) return NotFound($"Supervisor {dto.SupervisorId} no existe o está inactivo.");

            var tecValidos = await _db.Empleados
                .Where(e => dto.TecnicosIds.Contains(e.Id) && e.Estado == 1)
                .Select(e => e.Id)
                .ToListAsync();

            if (tecValidos.Count == 0) return BadRequest("No hay técnicos válidos.");

            foreach (var tecId in tecValidos.Distinct())
            {
                var dup = await _db.GruposSupervisoresTec
                    .AnyAsync(g => g.FkSupervisor == dto.SupervisorId && g.FkTecnico == tecId);
                if (!dup)
                {
                    _db.GruposSupervisoresTec.Add(new GrupoSupervisorTec
                    {
                        FkSupervisor = dto.SupervisorId,
                        FkTecnico = tecId,
                        Estado = true
                    });
                }
            }

            await _db.SaveChangesAsync();
            return StatusCode(201);
        }

        // POST: api/grupos/{id}/toggle
        [HttpPost("{id:int}/toggle")]
        public async Task<IActionResult> Toggle(int id)
        {
            var g = await _db.GruposSupervisoresTec.FindAsync(id);
            if (g == null) return NotFound();

            g.Estado = !g.Estado;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/grupos/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var g = await _db.GruposSupervisoresTec.FindAsync(id);
            if (g == null) return NotFound();

            _db.GruposSupervisoresTec.Remove(g);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
