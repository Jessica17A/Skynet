// Controllers/Api/SolicitudesAsignacionesApiController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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


    //// PATCH: /api/solicitudes/{id}/estado
    //[HttpPatch("{id:long}/estado")]
    //public async Task<ActionResult<SolicitudDto>> CambiarEstado(
    //    long id, [FromBody] CambiarEstadoDto dto, CancellationToken ct)
    //{
    //    if (dto is null || dto.Estado < 0 || dto.Estado > 5)
    //        return BadRequest(new { error = "Estado inválido. Debe ser 0..5" });

    //    var p1 = new SqlParameter("@SolicitudId", id);
    //    var p2 = new SqlParameter("@NuevoEstado", dto.Estado);

    //    await _db.Database.ExecuteSqlRawAsync(
    //        "EXEC dbo.sp_Solicitudes_CambiarEstado @SolicitudId, @NuevoEstado",
    //        new[] { p1, p2 }, ct);

    //    var s = await _db.Solicitudes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
    //    if (s is null) return NotFound();

    //    return Ok(Map(s));
    //}





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


    
    [HttpGet("solicitudes/{id:long}/asignaciones/tecnico/detalle")]
    public async Task<ActionResult<SolicitudAsignacionListado>> DetalleAsignacionTecnico(long id)
    {
        var userId = _userManager.GetUserId(User);

        var rows = await _db.SolicitudAsignacionListado
            .FromSqlRaw("EXEC dbo.usp_SolicitudDetalle_Tecnico @ID_SOLICITUD = {0}, @AspNetUserId = {1}", id, userId)
            .AsNoTracking()
            .ToListAsync();

        var item = rows.FirstOrDefault();
        if (item is null) return NotFound();  // el técnico no tiene asignación en esa solicitud

        return Ok(item);
    }






}
