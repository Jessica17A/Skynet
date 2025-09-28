// Controllers/Web/UsuariosUiController.cs
using System.Net.Http;
using System.Net.Http.Json; // <= necesario
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SkyNet.Controllers.Web
{
    [Authorize(Roles = "Administrador")] // opcional pero recomendado
    public class UsuariosUiController : Controller
    {
        private readonly IHttpClientFactory _factory;
        public UsuariosUiController(IHttpClientFactory factory) => _factory = factory;

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
        [ValidateAntiForgeryToken] // si envías token desde la vista
        public async Task<IActionResult> ResetPassword(string id, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(newPassword))
                return BadRequest(new { error = "Faltan parámetros." });

            var cli = CreateClient();

            // API espera "NewPassword"
            var resp = await cli.PostAsJsonAsync($"api/usuarios/{id}/reset-password",
                                                 new { NewPassword = newPassword });

            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync();
                return StatusCode((int)resp.StatusCode, new { error = err });
            }

            return Ok(new { ok = true });
        }

        // --- helper para reenviar cookie de auth al API ---
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
