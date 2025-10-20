using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace SkyNet.Controllers.Web
{
    [Authorize(Roles = "Administrador")]
    public class UsuariosUiController : Controller
    {
        private readonly IHttpClientFactory _factory;

        public UsuariosUiController(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        // 🔹 Vista principal de usuarios (tabla, etc.)
        [HttpGet]
        public IActionResult Index() => View();

        // 🔹 Nueva vista para crear usuarios
        [HttpGet]
        public IActionResult Crear() => View(); // 👉 esta es la nueva página Crear.cshtml

        // 🔹 Listado de usuarios (usado por tabla)
        [HttpGet]
        public async Task<IActionResult> Json()
        {
            var cli = CreateClient();
            var data = await cli.GetFromJsonAsync<object>("api/usuarios");
            return Json(data ?? new { });
        }

        // 🔹 Crear usuario (conecta con el API)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearUsuario([FromBody] CrearUsuarioRequest req)
        {
            if (req == null || req.EmpleadoId <= 0 || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrWhiteSpace(req.Role))
                return Json(new { ok = false, msg = "Datos incompletos." });

            var cli = CreateClient();

            var payload = JsonContent.Create(new
            {
                EmpleadoId = req.EmpleadoId,
                Password = req.Password,
                Role = req.Role
            }, options: new JsonSerializerOptions { PropertyNamingPolicy = null });

            var resp = await cli.PostAsync("api/usuarios", payload);

            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync();
                return Json(new { ok = false, msg = err });
            }

            var result = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            return Json(new { ok = true, msg = result?["msg"] ?? "Usuario creado correctamente." });
        }

        // 🔹 Reset Password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Id) || string.IsNullOrWhiteSpace(req.NewPassword))
                return Json(new { ok = false, msg = "Faltan parámetros." });

            var cli = CreateClient();

            var payload = JsonContent.Create(
                new { NewPassword = req.NewPassword },
                options: new JsonSerializerOptions { PropertyNamingPolicy = null }
            );

            var resp = await cli.PostAsync($"api/usuarios/{req.Id}/reset-password", payload);

            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync();
                return Json(new { ok = false, msg = err });
            }

            return Json(new { ok = true, msg = "Contraseña actualizada correctamente." });
        }

        // 🔹 Clases auxiliares
        public class CrearUsuarioRequest
        {
            public long EmpleadoId { get; set; }
            public string Password { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
        }

        public class ResetPasswordRequest
        {
            public string Id { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
        }

        // 🔹 Helper para HttpClient
        private HttpClient CreateClient()
        {
            var cli = _factory.CreateClient();
            cli.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}/");

            if (Request.Headers.TryGetValue("Cookie", out var cookie))
                cli.DefaultRequestHeaders.Add("Cookie", cookie.ToString());

            return cli;
        }
    }
}
