using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyNet.Data;
using SkyNet.Models;
using SkyNet.Models.DTOs;

namespace SkyNet.Controllers.Api
{
    [ApiController]
    [Route("api/solicitudes")]
    [Produces("application/json")]
    public class SolicitudesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SolicitudesApiController> _logger;

        public SolicitudesApiController(ApplicationDbContext db, ILogger<SolicitudesApiController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET: /api/solicitudes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SolicitudDto>>> GetAll(CancellationToken ct)
        {
            var list = await _db.Solicitudes
                .AsNoTracking()
               .Where(s => (int)s.Estado >= 0 && (int)s.Estado <= 2)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(s => new SolicitudDto
                {
                    Id = s.Id,
                    Nombre = s.Nombre,
                    Email = s.Email,
                    Telefono = s.Telefono,
                    Tipo = s.Tipo,
                    Prioridad = s.Prioridad,
                    Descripcion = s.Descripcion,
                    Ticket = s.Ticket,
                    CreatedAtUtc = s.CreatedAtUtc,
                    Estado = s.Estado,
                    Direccion = s.Direccion,
                    Latitud = s.Latitud,
                    Longitud = s.Longitud
                })
                .ToListAsync(ct);

            return Ok(list);
        }

        // GET: /api/solicitudes/{id}
        [HttpGet("{id:long}")]
        public async Task<ActionResult<SolicitudDto>> GetById(long id, CancellationToken ct)
        {
            var s = await _db.Solicitudes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            return s is null ? NotFound() : Ok(Map(s));
        }

        // GET: /api/solicitudes/by-ticket/{ticket}
        [HttpGet("by-ticket/{ticket}")]
        public async Task<ActionResult<SolicitudDto>> GetByTicket(string ticket, CancellationToken ct)
        {
            var s = await _db.Solicitudes.AsNoTracking().FirstOrDefaultAsync(x => x.Ticket == ticket, ct);
            return s is null ? NotFound() : Ok(Map(s));
        }

        // POST: /api/solicitudes
        [HttpPost]
        public async Task<ActionResult<SolicitudDto>> Create([FromBody] SolicitudCreateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var ticket = GenerateTicket();

            var entidad = new Solicitud
            {
                Nombre = dto.Nombre.Trim(),
                Email = dto.Email.Trim(),
                Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim(),
                Tipo = dto.Tipo.Trim(),
                Prioridad = dto.Prioridad?.Trim() ?? "",
                Descripcion = dto.Descripcion.Trim(),
                Ticket = ticket,
                CreatedAtUtc = DateTime.UtcNow,
                Estado = SolicitudEstado.Pendiente,
                Direccion = string.IsNullOrWhiteSpace(dto.Direccion) ? null : dto.Direccion.Trim(),
                Latitud = dto.Latitud,
                Longitud = dto.Longitud
            };

            _db.Solicitudes.Add(entidad);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetById), new { id = entidad.Id }, Map(entidad));
        }

        // PATCH: /api/solicitudes/{id}/estado
        [HttpPatch("{id:long}/estado")]
        public async Task<ActionResult<SolicitudDto>> CambiarEstado(long id, [FromBody] CambiarEstadoDto dto, CancellationToken ct)
        {
            if (dto is null || dto.Estado < 0 || dto.Estado > 5)
                return BadRequest(new { error = "Estado inválido. Debe ser 0..5" });

            var s = await _db.Solicitudes.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (s is null) return NotFound();

            s.Estado = (SolicitudEstado)dto.Estado;
            await _db.SaveChangesAsync(ct);

            return Ok(Map(s));
        }

        // Helpers
        private static string GenerateTicket()
        {
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var r = new Random();
            var tail = new string(Enumerable.Range(0, 6).Select(_ => chars[r.Next(chars.Length)]).ToArray());
            return $"SKY-{date}-{tail}";
        }

        private static SolicitudDto Map(Solicitud s) => new()
        {
            Id = s.Id,
            Nombre = s.Nombre,
            Email = s.Email,
            Telefono = s.Telefono,
            Tipo = s.Tipo,
            Prioridad = s.Prioridad,
            Descripcion = s.Descripcion,
            Ticket = s.Ticket,
            CreatedAtUtc = s.CreatedAtUtc,
            Estado = s.Estado,
            Direccion = s.Direccion,
            Latitud = s.Latitud,
            Longitud = s.Longitud
        };
    }

}
