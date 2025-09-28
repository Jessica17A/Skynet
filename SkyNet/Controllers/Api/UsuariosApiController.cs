using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyNet.Data;
using SkyNet.Models.DTOs;

namespace SkyNet.Controllers.Api
{
    [ApiController]
    [Route("api/usuarios")]
    [Authorize]
    public class UsuariosApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public UsuariosApiController(ApplicationDbContext db) => _db = db;

        // GET: api/usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioDto>>> GetAll(CancellationToken ct)
        {
          
            var baseQuery =
                from u in _db.Users.AsNoTracking()
                join e in _db.Empleados.AsNoTracking() on u.Id equals e.UserId into ue
                from e in ue.DefaultIfEmpty()
                select new { u, e };

            var baseList = await baseQuery.ToListAsync(ct);
            var userIds = baseList.Select(x => x.u.Id).ToList();

         
            var rolesRaw = await (
                from ur in _db.UserRoles.AsNoTracking()
                join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
                where userIds.Contains(ur.UserId)
                select new { ur.UserId, RoleName = r.Name }
            ).ToListAsync(ct);

            var rolesDict = rolesRaw
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(z => z.RoleName)));

            var result = baseList
             .Where(x => x.e != null && x.e.Estado != 0) // filtra Estado ≠ 0
             .Select(x => new UsuarioDto
             {
                UserId = x.u.Id,
                UserName = x.u.UserName ?? "",
                Email = x.u.Email ?? "",
                Roles = rolesDict.TryGetValue(x.u.Id, out var r) ? r : "",

                EmpleadoId = x.e == null ? null : (long?)x.e.Id,
                Nombres = x.e?.Nombres,
                Apellidos = x.e?.Apellidos,
                Cargo = x.e?.Cargo,
                Estado = x.e?.Estado
            }).ToList();

            return Ok(result);
        }

        // PUT: api/usuarios/{id}/estado   (activar/desactivar)
        [HttpPut("{id}/estado")]
        public async Task<ActionResult> UpdateEstado(string id, [FromBody] UsuarioEstadoUpdateDto dto, CancellationToken ct)
        {
            var empleado = await _db.Empleados.FirstOrDefaultAsync(e => e.UserId == id, ct);
            if (empleado is null)
                return NotFound(new { error = "Este usuario no está enlazado a un empleado." });

            empleado.Estado = dto.Estado;
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        // DELETE: api/usuarios/{id}  (soft-delete = Estado = 0)
        [HttpDelete("{id}")]
        public async Task<ActionResult> SoftDelete(string id, CancellationToken ct)
        {
            var empleado = await _db.Empleados.FirstOrDefaultAsync(e => e.UserId == id, ct);
            if (empleado is null)
                return NotFound(new { error = "Este usuario no está enlazado a un empleado." });

            if (empleado.Estado != 0)
            {
                empleado.Estado = 0;
                await _db.SaveChangesAsync(ct);
            }
            return NoContent();
        }


        [Authorize(Roles = "Administrador")]
        [HttpPost("{id}/reset-password")]
        public async Task<ActionResult> ResetPassword(
         string id,
         [FromBody] UsuarioResetDto dto,
         [FromServices] UserManager<IdentityUser> userManager)
        {
            if (dto is null)
                return BadRequest(new { error = "Body vacío o JSON inválido." });

            if (string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(new { error = "NewPassword es requerido." });

            var user = await userManager.FindByIdAsync(id);
            if (user is null) return NotFound(new { error = "Usuario no encontrado." });

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, dto.NewPassword);

            if (!result.Succeeded)
                return BadRequest(new { error = string.Join("; ", result.Errors.Select(e => e.Description)) });

            await userManager.UpdateSecurityStampAsync(user); // opcional: cierra sesiones activas
            return Ok(new { ok = true, message = "Contraseña reseteada correctamente." });
        }

    }
}
