using System.Net;
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
        private readonly ILogger<SolicitudesApiController> _logger;

        public SolicitudesApiController(ApplicationDbContext db, Cloudinary cloud, ILogger<SolicitudesApiController> logger)
        {
            _db = db;
            _cloud = cloud;
            _logger = logger;
        }

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
                    AdjuntoPublicId = s.AdjuntoPublicId,
                    Direccion = s.Direccion,
                    Latitud = s.Latitud,
                    Longitud = s.Longitud
                })
                .ToListAsync(ct);

            return Ok(list);
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<SolicitudDto>> GetById(long id, CancellationToken ct)
        {
            var s = await _db.Solicitudes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            return s is null ? NotFound() : Map(s);
        }

        [HttpGet("by-ticket/{ticket}")]
        public async Task<ActionResult<SolicitudDto>> GetByTicket(string ticket, CancellationToken ct)
        {
            var s = await _db.Solicitudes.AsNoTracking().FirstOrDefaultAsync(x => x.Ticket == ticket, ct);
            return s is null ? NotFound() : Map(s);
        }

        // POST: multipart/form-data
        [HttpPost]
        [RequestSizeLimit(10_000_000)]
        public async Task<ActionResult<SolicitudDto>> Create([FromForm] SolicitudCreateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                // devuelve 400 con los errores (para que el Web Controller lo muestre)
                return ValidationProblem(ModelState);
            }

            // validar coordenadas si envían sólo una
            if (dto.Latitud.HasValue ^ dto.Longitud.HasValue)
                return BadRequest(new { error = "Debe proporcionar latitud y longitud juntas." });

            if (dto.Latitud is < -90 or > 90 || dto.Longitud is < -180 or > 180)
                return BadRequest(new { error = "Coordenadas fuera de rango." });

            var ticket = GenerateTicket();
            string? publicId = null;

            // Imagen (opcional)
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

                if (result.StatusCode != HttpStatusCode.OK && result.StatusCode != HttpStatusCode.Created)
                    return StatusCode((int)result.StatusCode, new { error = "Error subiendo la imagen a Cloudinary." });

                publicId = result.PublicId; // guardamos sólo el public_id
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
                AdjuntoPublicId = publicId,
                Direccion = string.IsNullOrWhiteSpace(dto.Direccion) ? null : dto.Direccion.Trim(),
                Latitud = dto.Latitud,
                Longitud = dto.Longitud
            };

            _db.Solicitudes.Add(entidad);
            await _db.SaveChangesAsync(ct);

            var outDto = Map(entidad);
            // Created => JSON queda camelCase por defecto
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
            AdjuntoPublicId = s.AdjuntoPublicId,
            Direccion = s.Direccion,
            Latitud = s.Latitud,
            Longitud = s.Longitud
        };
    }
}
