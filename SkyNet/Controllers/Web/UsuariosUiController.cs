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

        [HttpGet]
        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> Json()
        {
            var cli = CreateClient();
            var data = await cli.GetFromJsonAsync<object>("api/usuarios");
            return Json(data ?? new { });
        }

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

        public class ResetPasswordRequest
        {
            public string Id { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
        }

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
