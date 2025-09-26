using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyNet.Data;
using SkyNet.Models;
using SkyNet.Models.DTOs;

namespace SkyNet.Controllers.Api
{
    [ApiController]
    [Route("api/grupos")]
    public class GruposApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public GruposApiController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
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
                    .AnyAsync(g => g.FkSupervisor == dto.SupervisorId
                                && g.FkTecnico == tecId
                                && g.Estado);

                if (!dup)
                {
                    _db.GruposSupervisoresTec.Add(new GrupoSupervisorTec
                    {
                        FkSupervisor = dto.SupervisorId,
                        FkTecnico = tecId,
                        Estado = true,
                        FechaCreacionUtc = DateTime.UtcNow
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

        // GET: api/grupos/mis-tecnicos  (solo IDs y nombres)
        [HttpGet("mis-tecnicos")]
        public async Task<ActionResult<object>> MisTecnicos()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var supervisor = await _db.Empleados
                .FirstOrDefaultAsync(e => e.UserId == userId && e.Estado != 0 && e.Cargo == "Supervisor");

            if (supervisor == null)
                return Ok(new { isSupervisor = false, tecnicos = Array.Empty<OpcionEmpleadoDto>() });

            var tecIds = await _db.GruposSupervisoresTec
                .Where(g => g.FkSupervisor == supervisor.Id && g.Estado)
                .Select(g => g.FkTecnico)
                .Distinct()
                .ToListAsync();

            if (tecIds.Count == 0)
                return Ok(new { isSupervisor = true, tecnicos = Array.Empty<OpcionEmpleadoDto>() });

            var tecnicos = await _db.Empleados
                .Where(e => tecIds.Contains(e.Id) && e.Estado == 1)
                .OrderBy(e => e.Nombres).ThenBy(e => e.Apellidos)
                .Select(e => new OpcionEmpleadoDto { Id = e.Id, Nombre = e.Nombres + " " + e.Apellidos })
                .ToListAsync();

            return Ok(new { isSupervisor = true, tecnicos });
        }

        // GET: /api/grupos?sup=123&mine=true  (único GET de lista)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GrupoItemDto>>> Get([FromQuery] long? sup, [FromQuery] bool mine = false)
        {
            if (mine)
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

                var supEmp = await _db.Empleados
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.Estado == 1 && e.Cargo == "Supervisor");

                if (supEmp == null)
                    return Ok(Array.Empty<GrupoItemDto>());

                sup = supEmp.Id;
            }

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
                    SupervisorNombre = x.Supervisor != null
                        ? (x.Supervisor.Nombres + " " + x.Supervisor.Apellidos) : "",
                    TecnicoNombre = x.Tecnico != null
                        ? (x.Tecnico.Nombres + " " + x.Tecnico.Apellidos) : "",
                    FechaCreacionUtc = x.FechaCreacionUtc,
                    Estado = x.Estado
                })
                .ToListAsync();

            return Ok(data);
        }
    }
}
