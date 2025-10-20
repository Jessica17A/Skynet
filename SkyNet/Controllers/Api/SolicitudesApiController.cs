using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SkyNet.Data;
using SkyNet.Models;
using SkyNet.Models.DTOs;
using SkyNet.Services;
using System.Security.Claims;

namespace SkyNet.Controllers.Api
{
    [ApiController]
    [Route("api/solicitudes")]
    [Produces("application/json")]
    public class SolicitudesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SolicitudesApiController> _logger;
        private readonly EmailService _emailService;

        public SolicitudesApiController(ApplicationDbContext db, ILogger<SolicitudesApiController> logger, EmailService emailService)
        {
            _db = db;
            _logger = logger;
            _emailService = emailService;
        }

        // GET: /api/solicitudes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SolicitudDto>>> GetAll(CancellationToken ct)
        {
            var list = await _db.Solicitudes
                .AsNoTracking()
               .Where(s => (int)s.Estado >= 0 && (int)s.Estado <= 2)
                .OrderBy(x => x.CreatedAtUtc)
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
        public async Task<ActionResult<SolicitudDto>> Details(long id, CancellationToken ct)
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

            return CreatedAtAction(nameof(Details), new { id = entidad.Id }, Map(entidad));
        }

       
        // PATCH: /api/solicitudes/{id}/estado
        [HttpPatch("{id:long}/estado")]
        public async Task<ActionResult<SolicitudDto>> CambiarEstado(
            long id, [FromBody] CambiarEstadoDto dto, CancellationToken ct)
        {
            if (dto is null || dto.Estado < 0 || dto.Estado > 5)
                return BadRequest(new { error = "Estado inválido. Debe ser 0..5" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Forbid();

            var p1 = new SqlParameter("@SolicitudId", id);
            var p2 = new SqlParameter("@NuevoEstado", dto.Estado);
            var p3 = new SqlParameter("@UserId", userId);

            // 1) Cambiar estado vía SP
            await _db.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_Solicitudes_CambiarEstado @SolicitudId, @NuevoEstado, @UserId",
                new[] { p1, p2, p3 }, ct);

            // 2) Recuperar la solicitud ya actualizada
            var s = await _db.Solicitudes.AsNoTracking()
                         .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (s is null) return NotFound();


            if (dto.Estado == 5 && !string.IsNullOrWhiteSpace(s.Email))
            {
                try
                {
                    _logger.LogInformation("📧 Intentando enviar correo de finalización a {Email}", s.Email);

                    await _emailService.EnviarCorreoFinalizacionAsync(s.Email, s.Nombre, s.Ticket);

                    _logger.LogInformation("✅ Correo enviado correctamente a {Email}", s.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error al enviar correo a {Email}: {Mensaje}", s.Email, ex.Message);
                    return Ok(new { message = "Estado cambiado, pero fallo el correo", error = ex.Message });
                }
            }



            return Ok(Map(s));
        }


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
    


    [HttpGet("{id:long}/tracking")]
        public async Task<ActionResult<IEnumerable<SolicitudTrackingTimelineRow>>> Tracking(long id, CancellationToken ct)
        {
            var rows = await _db.SolicitudTrackingTimeline
                .FromSqlRaw("EXEC dbo.sp_Solicitud_Tracking_Timeline @SolicitudId = {0}", id)
                .AsNoTracking()
                .ToListAsync(ct);

            return Ok(rows);
        }


        [HttpGet("detalle-completo/{id:long}")]
        public async Task<IActionResult> GetDetalleCompleto(long id)
        {
            var data = await _db.SolicitudDetalleCompleto
                .FromSqlRaw("EXEC usp_SolicitudDetalle_Completo @ID_SOLICITUD={0}", id)
                .ToListAsync();

            if (!data.Any())
                return NotFound();

            return Ok(data);
        }

        //[HttpGet("test-brevo")]
        //public async Task<IActionResult> TestBrevo()
        //{
        //    var emailService = new SkyNet.Services.EmailService();
        //    await emailService.EnviarCorreoFinalizacionAsync("gonzalezjessica813@gmail.com", "Prueba SkyNet", "TCK-0001");
        //    return Ok("Intento de envío realizado. Revisa tu correo o el log de Brevo.");
        //}





    }

}
