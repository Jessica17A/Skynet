// Controllers/Api/SolicitudesAsignacionesApiController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SkyNet.Data;
using SkyNet.Models;
using SkyNet.Models.DTOs;
using System.Linq;
using System.Security.Claims;

[ApiController]
[Route("api/solicitudes")]
public class SolicitudesAsignacionesApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<SolicitudesAsignacionesApiController> _log;

    public SolicitudesAsignacionesApiController(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager,
        ILogger<SolicitudesAsignacionesApiController> log)
    {
        _db = db;
        _userManager = userManager;
        _log = log;
    }

    [HttpPatch("asignaciones/{id:long}/estado")]
    public async Task<IActionResult> PatchEstado(long id,[FromBody] SolicitudFinalizarDto dto,CancellationToken ct)
    {
        if (dto is null) return BadRequest("Body requerido.");

        var estadoNuevo = (SolicitudAsignacionEstado)dto.Estado;
        if (!Enum.IsDefined(typeof(SolicitudAsignacionEstado), estadoNuevo))
            return BadRequest("Estado inválido (0..5).");

        var asig = await _db.SolicitudAsignaciones
                            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (asig is null) return NotFound();

        // UserId para tracking
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Forbid(); // o asigna "system" si prefieres

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // --- Actualiza ASIGNACIÓN ---
        asig.Estado = estadoNuevo;
        if (!string.IsNullOrWhiteSpace(dto.Nota))
            asig.Notas = dto.Nota;

        if (estadoNuevo == SolicitudAsignacionEstado.Finalizada)
            asig.Fecha_Fin = dto.Fecha_Fin ?? DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        // --- Propaga a SOLICITUD y registra tracking SOLO si 4=Proceso o 5=Finalizada ---
        if (estadoNuevo == SolicitudAsignacionEstado.Proceso
            || estadoNuevo == SolicitudAsignacionEstado.Finalizada)
        {
            var p1 = new SqlParameter("@SolicitudId", asig.FkSolicitud);
            var p2 = new SqlParameter("@NuevoEstado", (int)estadoNuevo);
            var p3 = new SqlParameter("@UserId", userId);

            await _db.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_Solicitudes_CambiarEstado @SolicitudId, @NuevoEstado, @UserId",
                new[] { p1, p2, p3 }, ct);
        }

        await tx.CommitAsync(ct);
        return NoContent();
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

    [HttpGet("{id:long}/asignaciones/tecnico")]
    public async Task<ActionResult<IEnumerable<SolicitudAsignacionDto>>> ListarAsignacionesPorSolicitudTecnico(long id)
    {
        // Obtener el UserId del Identity actual
        var userId = _userManager.GetUserId(User);

        // Ejecutar el SP pasando la solicitud y el usuario logueado
        var rows = await _db.SolicitudAsignacionListado
        .FromSqlRaw("EXEC dbo.usp_SolicitudesAsignaciones_PorSolicitud @ID_SOLICITUD = {0}, @AspNetUserId = {1}", id, userId)
        .AsNoTracking()
        .ToListAsync();

        return Ok(rows);
    }




    [HttpPost("{id:long}/asignaciones")]
    public async Task<IActionResult> CrearAsignacion(long id, [FromBody] SolicitudAsignacionCreateDto dto, CancellationToken ct)
    {
        if (dto is null || id != dto.IdSolicitud) return BadRequest("Body o id inválidos.");

        var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var yaEsta = await _db.SolicitudAsignaciones.AnyAsync(a =>
            a.FkSolicitud == id &&
            a.FkTecnico == dto.FkTecnico &&
            a.Estado == SolicitudAsignacionEstado.Asignada, ct);

        if (yaEsta) return Ok(new { ok = true, ignored = true, reason = "Técnico ya activo." });

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var asign = new SolicitudAsignacion
            {
                FkSolicitud = id,
                IdGrupo = dto.IdGrupo,
                FkTecnico = dto.FkTecnico,
                Fecha_Inicio = dto.Fecha_Inicio,
                Notas = dto.Notas,
                Estado = SolicitudAsignacionEstado.Asignada, // 3
                FechaAsignacionUtc = DateTime.UtcNow
            };

            _db.SolicitudAsignaciones.Add(asign);
            await _db.SaveChangesAsync(ct);

            // Cambia estado de la SOLICITUD y registra tracking (SP)
            await _db.Database.ExecuteSqlInterpolatedAsync($@"
            EXEC dbo.sp_Solicitudes_CambiarEstado 
                 @SolicitudId={id}, 
                 @NuevoEstado={(int)SolicitudAsignacionEstado.Asignada}, 
                 @UserId={userId}", ct);

            await tx.CommitAsync(ct);

            return Ok(new
            {
                ok = true,
                asignacion = new
                {
                    asign.Id,
                    asign.FkSolicitud,
                    asign.IdGrupo,
                    asign.FkTecnico,
                    asign.FechaAsignacionUtc,
                    asign.Fecha_Inicio,
                    asign.Notas,
                    Estado = (byte)asign.Estado
                },
                estadoSolicitud = (int)SolicitudAsignacionEstado.Asignada
            });
        }
        catch
        {
            if (tx.GetDbTransaction().Connection is not null) await tx.RollbackAsync(ct);
            throw;
        }
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


  
    [HttpGet("asignaciones/supervisor")]
    public async Task<ActionResult<IEnumerable<SolicitudAsignacionListado>>> ListarAsignacionesSupervisor()
    {
        var userId = _userManager.GetUserId(User); // AspNetUsers.Id

        var rows = await _db.SolicitudAsignacionListado
            .FromSqlRaw("EXEC dbo.usp_SolicitudesAsignaciones_Supervisor @AspNetUserId = {0}", userId)
            .AsNoTracking()
            .ToListAsync();

        return Ok(rows);
    }

    [HttpGet("asignaciones/tecnico")]
    public async Task<ActionResult<IEnumerable<SolicitudAsignacionListado>>> ListarAsignacionesTecnico()
    {
        var userId = _userManager.GetUserId(User);

        var rows = await _db.SolicitudAsignacionListado
            .FromSqlRaw("EXEC dbo.usp_SolicitudesAsignaciones_Tecnico @AspNetUserId = {0}", userId)
            .AsNoTracking()
            .ToListAsync();

        return Ok(rows);
    }



    //[HttpGet("solicitudes/{id:long}/asignaciones/tecnico/detalle")]
    //public async Task<ActionResult<SolicitudAsignacionListado>> DetalleAsignacionTecnico(long id)
    //{
    //    var userId = _userManager.GetUserId(User);

    //    var rows = await _db.SolicitudAsignacionListado
    //        .FromSqlRaw("EXEC dbo.usp_SolicitudDetalle_Tecnico @ID_SOLICITUD = {0}, @AspNetUserId = {1}", id, userId)
    //        .AsNoTracking()
    //        .ToListAsync();

    //    var item = rows.FirstOrDefault();
    //    if (item is null) return NotFound();  // el técnico no tiene asignación en esa solicitud

    //    return Ok(item);
    //}

    [HttpGet("{id:long}/asignaciones/tecnico/detalle")]
    public async Task<ActionResult<SolicitudAsignacionDetalleDto>> DetalleAsignacionTecnico(long id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var rows = await _db.SolicitudAsignacionDetalle
            .FromSqlRaw("EXEC dbo.usp_SolicitudDetalle_Tecnico @ID_SOLICITUD = {0}, @AspNetUserId = {1}", id, userId)
            .AsNoTracking()
            .ToListAsync();

        var item = rows.FirstOrDefault();
        if (item is null) return NotFound();
        return Ok(item);
    }


    //[HttpGet("solicitudes/{id:long}/tracking")]
    //public async Task<ActionResult<IEnumerable<SolicitudTrackingTimelineRow>>> Tracking(long id, CancellationToken ct)
    //{
    //    var rows = await _db.SolicitudTrackingTimeline
    //        .FromSqlRaw("EXEC dbo.sp_Solicitud_Tracking_Timeline @SolicitudId = {0}", id)
    //        .AsNoTracking()
    //        .ToListAsync(ct);

    //    return Ok(rows);
    //}




}
