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


    [HttpGet("asignaciones")]
    public async Task<ActionResult<IEnumerable<SolicitudAsignacionListado>>> ListarAsignacionesTodas()
    {
        var rows = await _db.SolicitudAsignacionListado
            .FromSqlRaw("EXEC dbo.usp_SolicitudesAsignaciones_Todas")
            .AsNoTracking()
            .ToListAsync();

        return Ok(rows);
    }


    // GET /api/solicitudes/{id}/asignaciones
    [HttpGet("{id:long}/asignaciones")]
    public async Task<ActionResult<IEnumerable<SolicitudAsignacionListado>>> GetAsignacionesPorSolicitud(long id)
    {
        var asignaciones = await _db.SolicitudAsignacionListado
            .FromSqlRaw("EXEC dbo.usp_SolicitudesAsignaciones_PorSolicitud @ID_SOLICITUD={0}", id)
            .AsNoTracking()
            .ToListAsync();

        if (!asignaciones.Any())
            return NotFound();

        return Ok(asignaciones);
    }






    // Controllers/Api/SolicitudesAsignacionesApiController.cs
    [HttpPost("{id:long}/asignaciones")]
    public async Task<IActionResult> CrearAsignacion(long id, [FromBody] SolicitudAsignacionCreateDto dto)
    {
        if (dto is null) return BadRequest("Body requerido.");
        if (id != dto.IdSolicitud) return BadRequest("Id de ruta no coincide con el body.");
       
       
        var yaEstaEseTecnico = await _db.SolicitudAsignaciones.AnyAsync(a =>
            a.FkSolicitud == id &&
            a.FkTecnico == dto.FkTecnico &&
            a.Estado == SolicitudAsignacionEstado.Asignada);

        if (yaEstaEseTecnico)
        {
            
            return Ok(new { ok = true, ignored = true, reason = "El técnico ya está activo en esta solicitud." });
        }

        var asign = new SolicitudAsignacion
        {
            FkSolicitud = id,
            IdGrupo = dto.IdGrupo,
            FkTecnico = dto.FkTecnico,
            Fecha_Inicio = dto.Fecha_Inicio, 
            Notas = dto.Notas,
            Estado = SolicitudAsignacionEstado.Asignada,
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
            Fecha_Inicio = asign.Fecha_Inicio,
            Notas = asign.Notas,
            Estado = (byte)asign.Estado
        };

        return Ok(outDto);
    }


    [HttpPatch("asignaciones/{asigId:long}/estado")]
    public async Task<IActionResult> CambiarEstado(long asigId, [FromBody] SolicitudAsignacionEstadoDto body)
    {
        if (body is null) return BadRequest("Body requerido.");
        if (body.Estado != 0 && body.Estado != 2 && body.Estado != 5)
            return BadRequest("Estado no permitido (0, 2 o 5).");

        var asign = await _db.SolicitudAsignaciones.FirstOrDefaultAsync(a => a.Id == asigId);
        if (asign == null) return NotFound("Asignación no encontrada.");

        // Anular
        if (body.Estado == 0)
        {
            asign.Estado = SolicitudAsignacionEstado.Anulada; // 0
            if (!string.IsNullOrWhiteSpace(body.Nota))
                asign.Notas = string.IsNullOrWhiteSpace(asign.Notas) ? body.Nota : $"{asign.Notas} | {body.Nota}";
        }
        else
        {
            // Finalizar (acepta 2 o 5)
            if (!body.Fecha_Fin.HasValue)
                return BadRequest("Fecha_Fin es requerida al finalizar.");

            asign.Estado = SolicitudAsignacionEstado.Finalizada; // 2
            asign.Fecha_Fin = DateTime.SpecifyKind(body.Fecha_Fin.Value, DateTimeKind.Utc);

            if (!string.IsNullOrWhiteSpace(body.Nota))
                asign.Notas = string.IsNullOrWhiteSpace(asign.Notas) ? body.Nota : $"{asign.Notas} | {body.Nota}";

            // Si viene 5, además cerrar la SOLICITUD en 5 (Finalizado)
            if (body.Estado == 5)
            {
                var sol = await _db.Solicitudes.FirstOrDefaultAsync(s => s.Id == asign.FkSolicitud);
                if (sol != null) sol.Estado = SolicitudEstado.Finalizado; // 5
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new
        {
            ok = true,
            asignEstado = (byte)asign.Estado,
            fechaFin = asign.Fecha_Fin
        });
    }



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
                Fecha_Inicio = x.Fecha_Inicio,
                Notas = x.Notas,
                Estado = (byte)x.Estado
            })
            .FirstOrDefaultAsync();

        if (a == null) return NotFound();
        return Ok(a);
    }
}
