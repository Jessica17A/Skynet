// Controllers/Api/SolicitudesAsignacionesApiController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyNet.Data;
using SkyNet.Models;
using SkyNet.Models.DTOs;
using System.Linq;

[ApiController]
[Route("api/solicitudes")]
public class SolicitudesAsignacionesApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<SolicitudesAsignacionesApiController> _log;

    public SolicitudesAsignacionesApiController(ApplicationDbContext db, ILogger<SolicitudesAsignacionesApiController> log)
    {
        _db = db; _log = log;
    }

    // POST /api/solicitudes/{id}/asignaciones
    [HttpPost("{id:long}/asignaciones")]
    public async Task<IActionResult> CrearAsignacion(long id, [FromBody] SolicitudAsignacionCreateDto dto)
    {
        if (dto is null) return BadRequest("Body requerido.");
        if (id != dto.IdSolicitud) return BadRequest("Solicitud no coincide.");

        // Validaciones mínimas
        var solicitudExiste = await _db.Solicitudes.AnyAsync(s => s.Id == id);
        if (!solicitudExiste) return NotFound("Solicitud no existe.");

        // Cumplir regla: solo una activa por solicitud
        var hayActiva = await _db.SolicitudAsignaciones
            .AnyAsync(a => a.FkSolicitud == id && a.Estado == SolicitudAsignacionEstado.Activa);
        if (hayActiva) return Conflict("La solicitud ya tiene una asignación activa.");

        var asign = new SolicitudAsignacion
        {
            FkSolicitud = id,
            IdGrupo = dto.IdGrupo,
            FkTecnico = dto.FkTecnico,
            AsignadoPorUserId = User?.Identity?.Name,
            Notas = dto.Notas,
            Estado = SolicitudAsignacionEstado.Activa,
            FechaAsignacionUtc = DateTime.UtcNow
        };

        _db.SolicitudAsignaciones.Add(asign);
        await _db.SaveChangesAsync();

        var outDto = new SolicitudAsignacionDto
        {
            Id = asign.Id,
            IdSolicitud = asign.FkSolicitud,
            IdGrupo = asign.IdGrupo,
            FkTecnico = asign.FkTecnico,
            FechaAsignacionUtc = asign.FechaAsignacionUtc,
            AsignadoPorUserId = asign.AsignadoPorUserId,
            Notas = asign.Notas,
            Estado = (byte)asign.Estado
        };

        return Ok(outDto);
    }

    // GET /api/solicitudes/{id}/asignaciones (todas)
    [HttpGet("{id:long}/asignaciones")]
    public async Task<ActionResult<IEnumerable<SolicitudAsignacionDto>>> ListarAsignaciones(long id)
    {
        var list = await _db.SolicitudAsignaciones
            .Where(a => a.FkSolicitud == id)
            .OrderByDescending(a => a.FechaAsignacionUtc)
            .Select(a => new SolicitudAsignacionDto
            {
                Id = a.Id,
                IdSolicitud = a.FkSolicitud,
                IdGrupo = a.IdGrupo,
                FkTecnico = a.FkTecnico,
                FechaAsignacionUtc = a.FechaAsignacionUtc,
                AsignadoPorUserId = a.AsignadoPorUserId,
                Notas = a.Notas,
                Estado = (byte)a.Estado
            })
            .ToListAsync();

        return Ok(list);
    }

    // PATCH /api/solicitudes/asignaciones/{asigId}/estado
    [HttpPatch("asignaciones/{asigId:long}/estado")]
    public async Task<IActionResult> CambiarEstado(long asigId, [FromBody] SolicitudAsignacionEstadoDto body)
    {
        var asign = await _db.SolicitudAsignaciones.FirstOrDefaultAsync(a => a.Id == asigId);
        if (asign == null) return NotFound();

        // Solo se permite cambiar a Anulada(0) o Finalizada(2)
        if (body.Estado != 0 && body.Estado != 2) return BadRequest("Estado no permitido");

        asign.Estado = (SolicitudAsignacionEstado)body.Estado;

        if (!string.IsNullOrWhiteSpace(body.Nota))
        {
            asign.Notas = string.IsNullOrWhiteSpace(asign.Notas)
                ? body.Nota
                : $"{asign.Notas} | {body.Nota}";
        }

        await _db.SaveChangesAsync();
        return Ok(new { ok = true, estado = (byte)asign.Estado });
    }

    // GET /api/solicitudes/{id}/asignacion-activa
    [HttpGet("{id:long}/asignacion-activa")]
    public async Task<IActionResult> GetActiva(long id)
    {
        var a = await _db.SolicitudAsignaciones
            .Where(x => x.FkSolicitud == id && x.Estado == SolicitudAsignacionEstado.Activa)
            .Select(x => new SolicitudAsignacionDto
            {
                Id = x.Id,
                IdSolicitud = x.FkSolicitud,
                IdGrupo = x.IdGrupo,
                FkTecnico = x.FkTecnico,
                FechaAsignacionUtc = x.FechaAsignacionUtc,
                AsignadoPorUserId = x.AsignadoPorUserId,
                Notas = x.Notas,
                Estado = (byte)x.Estado
            })
            .FirstOrDefaultAsync();

        if (a == null) return NotFound();
        return Ok(a);
    }
}
