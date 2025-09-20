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

        [HttpPost]
        [RequestSizeLimit(25_000_000)] 
        public async Task<ActionResult<SolicitudDto>> Create([FromForm] SolicitudCreateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            // Coordenadas: ambas o ninguna, y dentro de rango
            if (dto.Latitud.HasValue ^ dto.Longitud.HasValue)
                return BadRequest(new { error = "Debe proporcionar latitud y longitud juntas." });
            if ((dto.Latitud is < -90 or > 90) || (dto.Longitud is < -180 or > 180))
                return BadRequest(new { error = "Coordenadas fuera de rango." });

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
            await _db.SaveChangesAsync(ct); // genera entidad.Id

            // Si hay archivos, valida y sube (Cloudinary: imágenes -> ImageUploadParams; otros -> RawUploadParams)
            var archivos = dto.Archivos?.Where(f => f is not null && f.Length > 0).ToList() ?? new();
            if (archivos.Count > 0)
            {
                var imgExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var pdfExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf" };
                var wordExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".doc", ".docx" };
                var xlsExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".xls", ".xlsx" };
                const long MAX_FILE_BYTES = 10_000_000; // 10 MB por archivo

                foreach (var file in archivos)
                {
                    if (file.Length > MAX_FILE_BYTES)
                        return BadRequest(new { error = $"El archivo {file.FileName} supera 10 MB." });

                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    bool esImagen = imgExt.Contains(ext);
                    bool esPdf = pdfExt.Contains(ext);
                    bool esWord = wordExt.Contains(ext);
                    bool esExcel = xlsExt.Contains(ext);

                    if (!(esImagen || esPdf || esWord || esExcel))
                        return BadRequest(new { error = $"Tipo de archivo no permitido: {file.FileName}" });

                    string publicId;

                    await using (var stream = file.OpenReadStream())
                    {
                        if (esImagen)
                        {
                            var up = new ImageUploadParams
                            {
                                File = new FileDescription(file.FileName, stream),
                                Folder = $"solicitudes/{ticket}",
                                UseFilename = true,
                                UniqueFilename = true,
                                Overwrite = false
                            };

                            var res = await _cloud.UploadAsync(up); // sin CancellationToken
                            if (res.StatusCode != HttpStatusCode.OK && res.StatusCode != HttpStatusCode.Created)
                                return StatusCode((int)res.StatusCode, new { error = $"Error subiendo imagen {file.FileName}." });

                            publicId = res.PublicId!;
                        }
                        else
                        {
                            var up = new RawUploadParams
                            {
                                File = new FileDescription(file.FileName, stream),
                                Folder = $"solicitudes/{ticket}",
                                UseFilename = true,
                                UniqueFilename = true,
                                Overwrite = false
                                // NO asignes ResourceType: RawUploadParams ya lo fija a raw
                            };

                            var res = await _cloud.UploadAsync(up);
                            if (res.StatusCode != HttpStatusCode.OK && res.StatusCode != HttpStatusCode.Created)
                                return StatusCode((int)res.StatusCode, new { error = $"Error subiendo archivo {file.FileName}." });

                            publicId = res.PublicId!;
                        }
                    }

                    _db.ArchivosSolicitudes.Add(new ArchivoSolicitud
                    {
                        Fk_Solicitud = entidad.Id,   // FK
                        PublicId = publicId,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }

                await _db.SaveChangesAsync(ct);
            }

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
           
            Direccion = s.Direccion,
            Latitud = s.Latitud,
            Longitud = s.Longitud
        };
    }
}
