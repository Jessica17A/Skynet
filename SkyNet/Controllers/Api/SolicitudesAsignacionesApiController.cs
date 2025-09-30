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
                Fecha_Inicio = a.Fecha_Inicio,

                Notas = a.Notas,
                Estado = (byte)a.Estado
            })
            .ToListAsync();

        return Ok(list);
    }

    // Controllers/Api/SolicitudesAsignacionesApiController.cs
    [HttpPost("{id:long}/asignaciones")]
    public async Task<IActionResult> CrearAsignacion(long id, [FromBody] SolicitudAsignacionCreateDto dto)
    {
        if (dto is null) return BadRequest("Body requerido.");
        if (id != dto.IdSolicitud) return BadRequest("Id de ruta no coincide con el body.");
        if (dto.IdGrupo <= 0 || dto.FkTecnico <= 0) return BadRequest("Grupo y Técnico son requeridos.");

        // Validaciones básicas de existencia (opcionalmente deja solo la de solicitud)
        var solicitudExiste = await _db.Solicitudes.AnyAsync(s => s.Id == id);
        if (!solicitudExiste) return NotFound("La solicitud no existe.");

        // (Opcional) valida que el técnico exista
        var tecnicoExiste = await _db.Empleados.AnyAsync(e => e.Id == dto.FkTecnico && e.Estado != 0);
        if (!tecnicoExiste) return NotFound("El técnico no existe o está inactivo.");

        // (Opcional) valida que el grupo exista
        var grupoExiste = await _db.GruposSupervisoresTec.AnyAsync(g => g.IdGrupo == dto.IdGrupo && g.Estado);
        if (!grupoExiste) return NotFound("El grupo no existe o está inactivo.");

        // *** PERMITIR varias activas ***
        // Solo evitamos duplicar el MISMO técnico activo en la misma solicitud
        var yaEstaEseTecnico = await _db.SolicitudAsignaciones.AnyAsync(a =>
            a.FkSolicitud == id &&
            a.FkTecnico == dto.FkTecnico &&
            a.Estado == SolicitudAsignacionEstado.Activa);

        if (yaEstaEseTecnico)
        {
            // No lo consideres error: simplemente ignora y devuelve ok informativo
            return Ok(new { ok = true, ignored = true, reason = "El técnico ya está activo en esta solicitud." });
        }

        var asign = new SolicitudAsignacion
        {
            FkSolicitud = id,
            IdGrupo = dto.IdGrupo,
            FkTecnico = dto.FkTecnico,
            Fecha_Inicio = dto.Fecha_Inicio, // puede ser null
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
            Fecha_Inicio = asign.Fecha_Inicio,
            Notas = asign.Notas,
            Estado = (byte)asign.Estado
        };

        return Ok(outDto);
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
                Fecha_Inicio = x.Fecha_Inicio,
                Notas = x.Notas,
                Estado = (byte)x.Estado
            })
            .FirstOrDefaultAsync();

        if (a == null) return NotFound();
        return Ok(a);
    }
}
