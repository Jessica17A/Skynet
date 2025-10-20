using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyNet.Data;
using SkyNet.Models.DTOs;
using System.Security.Claims;
using System.Globalization;
using System.Text;

namespace SkyNet.Controllers.Api
{
    [ApiController]
    [Route("api/usuarios")]
    [Authorize]
    public class UsuariosApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsuariosApiController(
            ApplicationDbContext db,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ===========================================
        // 🔹 Listar usuarios
        // ===========================================
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

        // ===========================================
        // 🔹 Crear nuevo usuario
        // ===========================================
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CrearUsuario([FromBody] CrearUsuarioRequest model, CancellationToken ct)
        {
            if (model == null || model.EmpleadoId <= 0 || string.IsNullOrWhiteSpace(model.Password) || string.IsNullOrWhiteSpace(model.Role))
                return BadRequest(new { error = "Datos incompletos." });

            var empleado = await _db.Empleados.FirstOrDefaultAsync(e => e.Id == model.EmpleadoId, ct);
            if (empleado == null)
                return NotFound(new { error = "Empleado no encontrado." });

            if (string.IsNullOrWhiteSpace(empleado.Email))
                return BadRequest(new { error = "El empleado no tiene email registrado." });

            if (empleado.UserId != null)
                return BadRequest(new { error = "Este empleado ya tiene usuario asignado." });

            if (!await _roleManager.RoleExistsAsync(model.Role))
                return BadRequest(new { error = $"El rol '{model.Role}' no existe." });

            // 🔸 Generar username limpio
            var userName = BuildUserName(empleado.Nombres, empleado.Apellidos);
            var uniqueUserName = await EnsureUniqueUserNameAsync(userName);

            // 🔸 Crear usuario
            var user = new IdentityUser
            {
                UserName = uniqueUserName,
                Email = empleado.Email
            };

            var createRes = await _userManager.CreateAsync(user, model.Password);
            if (!createRes.Succeeded)
                return BadRequest(new { error = string.Join("; ", createRes.Errors.Select(e => e.Description)) });

            // 🔸 Asignar rol
            var roleRes = await _userManager.AddToRoleAsync(user, model.Role);
            if (!roleRes.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return BadRequest(new { error = string.Join("; ", roleRes.Errors.Select(e => e.Description)) });
            }

            // 🔸 Vincular con empleado
            empleado.UserId = user.Id;
            empleado.Estado = 2;
            await _db.SaveChangesAsync(ct);

            // 🔸 Agregar claim opcional
            await _userManager.AddClaimAsync(user, new Claim("EmpleadoId", empleado.Id.ToString()));

            return Ok(new
            {
                ok = true,
                msg = $"Usuario '{uniqueUserName}' creado correctamente para {empleado.Nombres} {empleado.Apellidos}.",
                userName = uniqueUserName
            });
        }

        // ===========================================
        // 🔹 Reset de contraseña
        // ===========================================
        [HttpPost("{id}/reset-password")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetPasswordRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.NewPassword))
                return BadRequest(new { error = "NewPassword es requerido." });

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new { error = "Usuario no encontrado." });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (!result.Succeeded)
                return BadRequest(new { error = string.Join("; ", result.Errors.Select(e => e.Description)) });

            await _userManager.UpdateSecurityStampAsync(user);
            return Ok(new { ok = true, msg = "Contraseña reseteada correctamente." });
        }

        // ===========================================
        // 🔹 Listar empleados disponibles
        // ===========================================
        [HttpGet("/api/empleados/disponibles")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetEmpleadosDisponibles(CancellationToken ct)
        {
            var empleados = await _db.Empleados
                .AsNoTracking()
                .Where(e => e.Estado == 1 && !string.IsNullOrEmpty(e.Email) && e.UserId == null)
                .OrderBy(e => e.Nombres)
                .Select(e => new
                {
                    id = e.Id,
                    nombres = e.Nombres,
                    apellidos = e.Apellidos,
                    cargo = e.Cargo
                })
                .ToListAsync(ct);

            return Ok(empleados);
        }

        // ===========================================
        // 🔹 Listar roles disponibles
        // ===========================================
        [HttpGet("/api/roles")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetRoles(CancellationToken ct)
        {
            var roles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .Select(r => new { name = r.Name })
                .ToListAsync(ct);

            return Ok(roles);
        }

        // ===========================================
        // 🔧 Métodos auxiliares
        // ===========================================
        private string BuildUserName(string nombres, string apellidos)
        {
            // Primera letra del nombre
            var firstName = (nombres ?? "").Trim();
            var firstLetter = firstName.Length > 0 ? firstName[0].ToString() : "x";

            // Primer apellido
            var lastName = (apellidos ?? "").Trim();
            if (!string.IsNullOrEmpty(lastName))
                lastName = lastName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "user";
            else
                lastName = "user";

            // Une y limpia caracteres inválidos
            var raw = $"{firstLetter}{lastName}".ToLowerInvariant();
            return RemoveInvalidChars(raw);
        }

        // 🔸 Quita acentos, ñ y símbolos no válidos
        private string RemoveInvalidChars(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "user";

            var normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var ch in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);

                // Elimina acentos y conserva solo letras o números
                if (category != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(ch))
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private async Task<string> EnsureUniqueUserNameAsync(string baseUserName)
        {
            var candidate = baseUserName;
            var i = 0;
            while (true)
            {
                var existing = await _userManager.FindByNameAsync(candidate);
                if (existing == null) return candidate;
                i++;
                candidate = baseUserName + i.ToString();
            }
        }

        // ===========================================
        // 🔸 Clases auxiliares
        // ===========================================
        public class CrearUsuarioRequest
        {
            public long EmpleadoId { get; set; }
            public string Password { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
        }

        public class ResetPasswordRequest
        {
            public string NewPassword { get; set; } = string.Empty;
        }
    }
}
