using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyNet.Data;
using SkyNet.Models.DTOs;
using System.Text.Json;

namespace SkyNet.Controllers.Api
{
    [ApiController]
    [Route("api/usuarios")]
    [Authorize]
    public class UsuariosApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public UsuariosApiController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // 🔹 Obtener usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioDto>>> GetAll(CancellationToken ct)
        {
            var baseQuery =
                from u in _db.Users.AsNoTracking()
                join e in _db.Empleados.AsNoTracking() on u.Id equals e.UserId into ue
                from e in ue.DefaultIfEmpty()
                select new { u, e };

            var baseList = await baseQuery.ToListAsync(ct);

            var result = baseList
                .Where(x => x.e != null && x.e.Estado != 0)
                .Select(x => new UsuarioDto
                {
                    UserId = x.u.Id,
                    UserName = x.u.UserName ?? "",
                    Email = x.u.Email ?? "",
                    Roles = "",
                    EmpleadoId = x.e.Id,
                    Nombres = x.e.Nombres,
                    Apellidos = x.e.Apellidos,
                    Cargo = x.e.Cargo,
                    Estado = x.e.Estado
                }).ToList();

            return Ok(result);
        }

        // 🔹 Reset Password (versión que sí funciona)
        [Authorize(Roles = "Administrador")]
        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(string id)
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            // Deserializar manualmente
            var json = JsonSerializer.Deserialize<Dictionary<string, string>>(body);

            if (json == null || !json.TryGetValue("NewPassword", out var newPassword) || string.IsNullOrWhiteSpace(newPassword))
                return BadRequest(new { error = "NewPassword es requerido." });

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new { error = "Usuario no encontrado." });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
                return BadRequest(new { error = string.Join("; ", result.Errors.Select(e => e.Description)) });

            await _userManager.UpdateSecurityStampAsync(user);
            return Ok(new { ok = true, msg = "Contraseña reseteada correctamente." });
        }
    }
}
