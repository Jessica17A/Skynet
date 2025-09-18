#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkyNet.Data;           // DbContext
using SkyNet.Models;         // Empleado

namespace SkyNet.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _db;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            ApplicationDbContext db,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _db = db;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public List<SelectListItem> EmpleadosOptions { get; set; } = new();
        public List<SelectListItem> RolesOptions { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Selecciona un empleado.")]
            [Display(Name = "Empleado")]
            public long EmpleadoId { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "La {0} debe tener entre {2} y {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Contraseña")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirmar contraseña")]
            [Compare("Password", ErrorMessage = "La contraseña y su confirmación no coinciden.")]
            public string ConfirmPassword { get; set; }

            [Required(ErrorMessage = "Selecciona un rol.")]
            [Display(Name = "Rol")]
            public string RoleName { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null, CancellationToken ct = default)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            await CargarListasAsync(ct);
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null, CancellationToken ct = default)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid)
            {
                await CargarListasAsync(ct);
                return Page();
            }

            // 1) Traer empleado (con tracking, porque lo vamos a actualizar)
            var emp = await _db.Empleados.FirstOrDefaultAsync(e => e.Id == Input.EmpleadoId, ct);
            if (emp is null)
            {
                ModelState.AddModelError("Input.EmpleadoId", "Empleado no encontrado.");
                await CargarListasAsync(ct);
                return Page();
            }

            if (string.IsNullOrWhiteSpace(emp.Email))
            {
                ModelState.AddModelError(string.Empty, "El empleado no tiene email registrado.");
                await CargarListasAsync(ct);
                return Page();
            }

            if (!string.IsNullOrEmpty(emp.UserId))
            {
                ModelState.AddModelError(string.Empty, "Ese empleado ya está vinculado a un usuario.");
                await CargarListasAsync(ct);
                return Page();
            }

            // 2) Validar rol existe
            if (!await _roleManager.RoleExistsAsync(Input.RoleName))
            {
                ModelState.AddModelError("Input.RoleName", "El rol seleccionado no existe.");
                await CargarListasAsync(ct);
                return Page();
            }

            // 3) Generar username: primera letra del primer nombre + primer apellido (sin acentos), en minúsculas
            var baseUserName = BuildBaseUserName(emp.Nombres, emp.Apellidos);
            var uniqueUserName = await EnsureUniqueUserNameAsync(baseUserName);

            // 4) Validar que no exista ya un usuario con ese email (opcional pero recomendado)
            var existing = await _userManager.FindByEmailAsync(emp.Email);
            if (existing is not null)
            {
                ModelState.AddModelError(string.Empty, $"Ya existe un usuario con el email {emp.Email}.");
                await CargarListasAsync(ct);
                return Page();
            }

            // 5) Crear usuario con username generado y email del empleado
            var user = CreateUser();
            await _userStore.SetUserNameAsync(user, uniqueUserName, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, emp.Email, CancellationToken.None);

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("Usuario {UserName} creado para empleado {EmpleadoId}.", uniqueUserName, emp.Id);

                // 5.1) Asignar rol
                var roleRes = await _userManager.AddToRoleAsync(user, Input.RoleName);
                if (!roleRes.Succeeded)
                {
                    foreach (var err in roleRes.Errors)
                        ModelState.AddModelError(string.Empty, err.Description);

                    // revertir usuario si falla rol
                    await _userManager.DeleteAsync(user);
                    await CargarListasAsync(ct);
                    return Page();
                }

                // 5.2) Claim con EmpleadoId (útil si no navegas por FK desde Identity)
                await _userManager.AddClaimAsync(user, new Claim("EmpleadoId", emp.Id.ToString()));

                // 5.3) Vincular Empleado.UserId <- AspNetUsers.Id
                emp.UserId = user.Id;
                emp.Estado = 2;
                await _db.SaveChangesAsync(ct);

                // 5.4) Email de confirmación
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(emp.Email, "Confirma tu email",
                    $"Confirma tu cuenta haciendo clic <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>aquí</a>.");

                // 6) No iniciar sesión; mostrar OK y resetear form
                ModelState.Clear();
                TempData["RegisterMsg"] = $"Usuario '{uniqueUserName}' creado para {emp.Nombres}  y asignado al rol '{Input.RoleName}'.";
                await CargarListasAsync(ct);
                return Page();
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            await CargarListasAsync(ct);
            return Page();
        }

        private async Task CargarListasAsync(CancellationToken ct)
        {
            // Solo empleados activos, con email y sin usuario vinculado
            var emps = await _db.Empleados.AsNoTracking()
                .Where(e => e.Estado == 1 && !string.IsNullOrEmpty(e.Email) && e.UserId == null)
                .OrderBy(e => e.Nombres)
                .Select(e => new { e.Id, e.Nombres, e.Email })
                .ToListAsync(ct);

            EmpleadosOptions = emps
                .Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = $"{e.Nombres} — {e.Email}"
                })
                .ToList();

            RolesOptions = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                .ToListAsync(ct);
        }

        // ---- Helpers ----

        private static string BuildBaseUserName(string nombres, string apellidos)
        {
            // primera letra del primer nombre
            var firstName = (nombres ?? "").Trim();
            var firstLetter = firstName.Length > 0 ? firstName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0][0].ToString() : "x";

            // primer apellido (palabra completa)
            var lastName1 = "x";
            var ap = (apellidos ?? "").Trim();
            if (!string.IsNullOrEmpty(ap))
            {
                lastName1 = ap.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "x";
            }
            else
            {
                // fallback: toma última palabra de nombres como apellido si no hay Apellidos
                var parts = firstName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1) lastName1 = parts.Last();
            }

            var raw = (firstLetter + lastName1).ToLowerInvariant();
            return SlugifyLettersAndDigits(raw);
        }

        private static string SlugifyLettersAndDigits(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "user";
            // quita acentos
            var norm = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(capacity: norm.Length);
            foreach (var ch in norm)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    // deja solo letras y números
                    if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
                }
            }
            var outp = sb.ToString().Normalize(NormalizationForm.FormC);
            return string.IsNullOrEmpty(outp) ? "user" : outp;
        }

        private async Task<string> EnsureUniqueUserNameAsync(string baseUserName)
        {
            var candidate = baseUserName;
            var i = 0;
            // intenta base, luego base1, base2, ...
            while (true)
            {
                var existing = await _userManager.FindByNameAsync(candidate);
                if (existing is null) return candidate;
                i++;
                candidate = baseUserName + i.ToString();
            }
        }

        private IdentityUser CreateUser()
        {
            try { return Activator.CreateInstance<IdentityUser>(); }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
                    $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor.");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
                throw new NotSupportedException("The default UI requires a user store with email support.");
            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}
