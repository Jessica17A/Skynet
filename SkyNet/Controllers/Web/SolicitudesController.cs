using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace SkyNet.Controllers.Web
{
    public class SolicitudesController : Controller
    {
        private readonly IHttpClientFactory _factory;
        private readonly ILogger<SolicitudesController> _logger;

        public SolicitudesController(IHttpClientFactory factory, ILogger<SolicitudesController> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index() => RedirectToAction(nameof(Create));

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string Nombre, string Email, string? Telefono, string Tipo, string? Prioridad,
            string Descripcion, IFormFile? Adjunto, bool AceptaTerminos)
        {
            var cli = _factory.CreateClient();
            cli.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}/");

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(Nombre ?? ""), "Nombre");
            form.Add(new StringContent(Email ?? ""), "Email");
            form.Add(new StringContent(Telefono ?? ""), "Telefono");
            form.Add(new StringContent(Tipo ?? ""), "Tipo");
            form.Add(new StringContent(string.IsNullOrWhiteSpace(Prioridad) ? "Normal" : Prioridad!), "Prioridad");
            form.Add(new StringContent(Descripcion ?? ""), "Descripcion");

            if (Adjunto is not null && Adjunto.Length > 0)
            {
                var content = new StreamContent(Adjunto.OpenReadStream());
                content.Headers.ContentType = new MediaTypeHeaderValue(Adjunto.ContentType ?? "application/octet-stream");
                form.Add(content, "Adjunto", Adjunto.FileName);
            }

            var resp = await cli.PostAsync("api/solicitudes", form);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                TempData["Error"] = "No se pudo enviar la solicitud. Intenta nuevamente.";
                return View();
            }

            var doc = System.Text.Json.JsonDocument.Parse(body);
            var ticket = doc.RootElement.GetProperty("ticket").GetString();


            // Deja flags separados
            TempData["Success"] = true;
            TempData["Ticket"] = ticket;

            return RedirectToAction(nameof(Create));
        }

    }
}
