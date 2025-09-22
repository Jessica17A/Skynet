// Controllers/Web/UsuariosUiController.cs
using Microsoft.AspNetCore.Mvc;

namespace SkyNet.Controllers.Web
{
    public class UsuariosUiController : Controller
    {
        private readonly IHttpClientFactory _factory;
        public UsuariosUiController(IHttpClientFactory factory) => _factory = factory;

        [HttpGet]
        public IActionResult Index() => View();

        // (Opcional) Si prefieres consumir la API via proxy desde el mismo host
        [HttpGet]
        public async Task<IActionResult> Json()
        {
            var cli = _factory.CreateClient();
            cli.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}/");
            var data = await cli.GetFromJsonAsync<object>("api/usuarios");
            return Json(data ?? new { });
        }
    }
}
