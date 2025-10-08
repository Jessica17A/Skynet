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
    public async Task<IActionResult> CrearAsignacion(long id, [FromBody] SolicitudAsignacionCreateDto dto)
    {
        if (dto is null || id != dto.IdSolicitud)
            return BadRequest(new { ok = false, msg = "Datos inválidos." });

        var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "sistema";

        try
        {
            var result = await _db.Database.SqlQueryRaw<AsignacionResultDto>(
                @"EXEC dbo.usp_SolicitudAsignarTecnico 
                @IdSolicitud = {0},
                @IdGrupo = {1},
                @FkTecnico = {2},
                @Notas = {3},
                @FechaInicioUtc = {4},
                @UserId = {5}",
                dto.IdSolicitud,
                dto.IdGrupo,
                dto.FkTecnico,
                dto.Notas,
                dto.Fecha_Inicio ?? DateTime.UtcNow, // Evita nulls
                userId
            ).ToListAsync();

            var r = result.FirstOrDefault();

            if (r is null)
                return StatusCode(500, new { ok = false, msg = "Sin respuesta del procedimiento." });

            if (r.Ok == 1)
                return Ok(new { ok = true, msg = r.Msg });

            return BadRequest(new { ok = false, msg = r.Msg });

        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            var inner = ex.InnerException?.Message;
            var stack = ex.StackTrace;

            // Esto devolverá todo el detalle al frontend (para depuración)
            return StatusCode(500, new
            {
                ok = false,
                msg = msg,
                inner = inner,
                stack = stack
            });
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

    [HttpGet("asignaciones/supervisor")]
    public async Task<ActionResult<IEnumerable<SolicitudAsignacionListadoS>>> ListarAsignacionesSupervisor()
    {
        var userId = _userManager.GetUserId(User);

       
            var rows = await _db.SolicitudAsignacionListado
                .FromSqlRaw("EXEC dbo.usp_SolicitudesAsignaciones_Supervisor @AspNetUserId = {0}", userId)
                .AsNoTracking()
                .ToListAsync();

            return Ok(rows);
       
    }





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


        // POST /api/solicitudes/{id}/asignaciones/tecnicos
        [HttpPost("{id:long}/asignaciones/tecnicos")]
        public async Task<IActionResult> AgregarTecnicos(long id, [FromBody] AddTechsDto dto)
        {
            var userId = _userManager.GetUserId(User);
            var idSup = await _db.Empleados.Where(e => e.UserId == userId).Select(e => e.Id).FirstOrDefaultAsync();
            if (idSup == 0) return Forbid();

            foreach (var tecId in dto.Tecnicos.Distinct())
            {
                await _db.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.usp_Solicitud_Asignacion_AgregarTecnico @ID_SOLICITUD={0}, @IdSupervisor={1}, @FkTecnico={2}, @Nota={3}",
                    id, idSup, tecId, dto.Nota ?? (object)DBNull.Value
                );
            }
            return NoContent();
        }

        public record AddTechsDto(List<long> Tecnicos, string? Nota);

        // PATCH /api/solicitudes/asignaciones/{asigId}/anular
        [HttpPatch("asignaciones/{asigId:long}/anular")]
        public async Task<IActionResult> AnularAsignacion(long asigId, [FromBody] MotivoDto body)
        {
            var userId = _userManager.GetUserId(User);
            var idSup = await _db.Empleados.Where(e => e.UserId == userId).Select(e => e.Id).FirstOrDefaultAsync();
            if (idSup == 0) return Forbid();

            await _db.Database.ExecuteSqlRawAsync(
                "EXEC dbo.usp_Solicitud_Asignacion_Anular @IdAsignacion={0}, @IdSupervisor={1}, @Nota={2}",
                asigId, idSup, body?.Nota ?? (object)DBNull.Value
            );
            return NoContent();
        }

        public record MotivoDto(string? Nota);






}
