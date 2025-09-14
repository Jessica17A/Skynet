using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
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
        private readonly Cloudinary _cloud;

        public SolicitudesApiController(ApplicationDbContext db, Cloudinary cloud)
        {
            _db = db;
            _cloud = cloud;
        }

        // GET: api/solicitudes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SolicitudDto>>> GetAll(CancellationToken ct)
        {
            var list = await _db.Solicitudes.AsNoTracking()
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
                    AdjuntoPublicId = s.AdjuntoPublicId
                })
                .ToListAsync(ct);

            return Ok(list);
        }

        // GET: api/solicitudes/{id}
        [HttpGet("{id:long}")]
        public async Task<ActionResult<SolicitudDto>> GetById(long id, CancellationToken ct)
        {
            var s = await _db.Solicitudes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            return s is null ? NotFound() : Map(s);
        }

        // GET: api/solicitudes/by-ticket/{ticket}
        [HttpGet("by-ticket/{ticket}")]
        public async Task<ActionResult<SolicitudDto>> GetByTicket(string ticket, CancellationToken ct)
        {
            var s = await _db.Solicitudes.AsNoTracking().FirstOrDefaultAsync(x => x.Ticket == ticket, ct);
            return s is null ? NotFound() : Map(s);
        }

        // POST: api/solicitudes  (multipart/form-data)
        [HttpPost]
        [RequestSizeLimit(10_000_000)]
        public async Task<ActionResult<SolicitudDto>> Create([FromForm] SolicitudCreateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var ticket = GenerateTicket();
            string? publicId = null;

            if (dto.Adjunto is not null && dto.Adjunto.Length > 0)
            {
                if (!dto.Adjunto.ContentType?.StartsWith("image/") ?? true)
                    return BadRequest(new { error = "Solo se permiten imágenes." });
                if (dto.Adjunto.Length > 5_000_000)
                    return BadRequest(new { error = "La imagen supera 5 MB." });

                var ext = Path.GetExtension(dto.Adjunto.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png" };
                if (!allowed.Contains(ext))
                    return BadRequest(new { error = "Extensión no permitida. Usa JPG o PNG." });

                await using var stream = dto.Adjunto.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(dto.Adjunto.FileName, stream),
                    Folder = $"solicitudes/{ticket}",
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false
                };
                var result = await _cloud.UploadAsync(uploadParams, ct);

                if (result.StatusCode != System.Net.HttpStatusCode.OK &&
                    result.StatusCode != System.Net.HttpStatusCode.Created)
                {
                    return StatusCode((int)result.StatusCode, new { error = "Error subiendo la imagen a Cloudinary." });
                }

               
                publicId = result.PublicId;  // Ejemplo: "solicitudes/SKY-20250914-ABC123/archivo_xyz"
            }

            var entidad = new Solicitud
            {
                Nombre = dto.Nombre.Trim(),
                Email = dto.Email.Trim(),
                Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim(),
                Tipo = dto.Tipo.Trim(),
                Prioridad = string.IsNullOrWhiteSpace(dto.Prioridad) ? "Normal" : dto.Prioridad.Trim(),
                Descripcion = dto.Descripcion.Trim(),
                Ticket = ticket,
                CreatedAtUtc = DateTime.UtcNow,
                Estado = SolicitudEstado.Pendiente,
                AdjuntoPublicId = publicId   // 👈 aquí guardas el identificador
            };

            _db.Solicitudes.Add(entidad);
            await _db.SaveChangesAsync(ct);

            var outDto = Map(entidad);
            return CreatedAtAction(nameof(GetById), new { id = entidad.Id }, outDto);
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
            AdjuntoPublicId = s.AdjuntoPublicId
        };
    }
}
