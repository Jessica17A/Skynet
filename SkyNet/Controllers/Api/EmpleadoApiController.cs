using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyNet.Data;
using SkyNet.Models;
using SkyNet.Models.DTOs;

namespace SkyNet.Controllers.Api
{
    [ApiController]
    [Route("api/empleados")]
    [Produces("application/json")]
    public class EmpleadosApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public EmpleadosApiController(ApplicationDbContext db) => _db = db;

        // helper para normalizar strings (null/empty -> "N/I")
        private static string F(string? s) => string.IsNullOrWhiteSpace(s) ? "N/I" : s.Trim();

        // GET: /api/empleados
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmpleadoDTO>>> GetAll(CancellationToken ct)
        {
            var data = await _db.Empleados.AsNoTracking()
                .Select(e => new EmpleadoDTO
                {
                    Id = e.Id,
                    Nombre = e.Nombre,
                    DPI = e.DPI,
                    Direccion = e.Direccion,
                    Telefono = e.Telefono,
                    Email = e.Email,
                    Cargo = e.Cargo,
                    CreatedAtUtc = e.CreatedAtUtc,
                    Estado = e.Estado
                })
                .ToListAsync(ct);

            return Ok(data);
        }

        // GET: /api/empleados/{id}
        [HttpGet("{id:long}")]
        public async Task<ActionResult<EmpleadoDTO>> GetById(long id, CancellationToken ct)
        {
            var dto = await _db.Empleados.AsNoTracking()
                .Where(e => e.Id == id)
                .Select(e => new EmpleadoDTO
                {
                    Id = e.Id,
                    Nombre = e.Nombre,
                    DPI = e.DPI,
                    Direccion = e.Direccion,
                    Telefono = e.Telefono,
                    Email = e.Email,
                    Cargo = e.Cargo,
                    CreatedAtUtc = e.CreatedAtUtc,
                    Estado = e.Estado
                })
                .FirstOrDefaultAsync(ct);

            return dto is null ? NotFound() : Ok(dto);
        }

        // POST: /api/empleados
        [HttpPost]
        public async Task<ActionResult<EmpleadoDTO>> Create([FromBody] EmpleadoCrearDTO? dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest(new { error = "Body vacío" });

            var entidad = new Empleado
            {
                Nombre = dto.Nombre?.Trim(),
                DPI = dto.DPI?.Trim(),
                Direccion = F(dto.Direccion),
                Telefono = F(dto.Telefono),
                Email = F(dto.Email),
                Cargo = dto.Cargo?.Trim(),
                Estado = dto.Estado ?? 1   // default 1 (activo)
                // CreatedAtUtc se llena con DateTime.UtcNow por el modelo
            };

            _db.Empleados.Add(entidad);
            await _db.SaveChangesAsync(ct);

            var outDto = new EmpleadoDTO
            {
                Id = entidad.Id,
                Nombre = entidad.Nombre,
                DPI = entidad.DPI,
                Direccion = entidad.Direccion,
                Telefono = entidad.Telefono,
                Email = entidad.Email,
                Cargo = entidad.Cargo,
                CreatedAtUtc = entidad.CreatedAtUtc,
                Estado = entidad.Estado
            };

            return CreatedAtAction(nameof(GetById), new { id = outDto.Id }, outDto);
        }

        // PUT: /api/empleados/{id}
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] EmpleadoEditarDTO? dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest(new { error = "Body vacío" });

            var e = await _db.Empleados.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null) return NotFound();

            // Actualiza solo lo enviado; normaliza NOT NULL a "N/I" si viene vacío
            if (dto.Nombre != null) e.Nombre = dto.Nombre.Trim();
            if (dto.DPI != null) e.DPI = dto.DPI.Trim();
            if (dto.Direccion != null) e.Direccion = F(dto.Direccion);
            if (dto.Telefono != null) e.Telefono = F(dto.Telefono);
            if (dto.Email != null) e.Email = F(dto.Email);
            if (dto.Cargo != null) e.Cargo = dto.Cargo.Trim();
            if (dto.Estado.HasValue) e.Estado = dto.Estado.Value;

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        // DELETE: /api/empleados/{id}
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken ct)
        {
            var e = await _db.Empleados.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null) return NotFound();

            _db.Empleados.Remove(e);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }
    }
}
