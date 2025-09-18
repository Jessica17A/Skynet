using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using SkyNet.Models.DTOs;
using System.Globalization;
using System.Net.Http.Json; // 👈 NECESARIO para ReadFromJsonAsync

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
        public async Task<IActionResult> Create([FromForm] SolicitudCreateDto form)
        {
            // Validación mínima del lado cliente-servidor
            if (string.IsNullOrWhiteSpace(form.Nombre) ||
                string.IsNullOrWhiteSpace(form.Email) ||
                string.IsNullOrWhiteSpace(form.Tipo) ||
                string.IsNullOrWhiteSpace(form.Descripcion))
            {
                TempData["Error"] = "Completa Nombre, Email, Tipo y Descripción.";
                return View();
            }

            var cli = _factory.CreateClient();
            cli.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}/");
            cli.Timeout = TimeSpan.FromSeconds(60);

            using var mp = new MultipartFormDataContent();
            var inv = CultureInfo.InvariantCulture;

            mp.Add(new StringContent(form.Nombre), "Nombre");
            mp.Add(new StringContent(form.Email), "Email");
            mp.Add(new StringContent(form.Telefono ?? ""), "Telefono");
            mp.Add(new StringContent(form.Tipo), "Tipo");
            mp.Add(new StringContent(form.Prioridad), "Prioridad");
            mp.Add(new StringContent(form.Descripcion), "Descripcion");

            mp.Add(new StringContent(form.Direccion ?? ""), "Direccion");
            mp.Add(new StringContent(form.Latitud?.ToString(inv) ?? ""), "Latitud");
            mp.Add(new StringContent(form.Longitud?.ToString(inv) ?? ""), "Longitud");

            if (form.Adjunto is not null && form.Adjunto.Length > 0)
            {
                var content = new StreamContent(form.Adjunto.OpenReadStream());
                content.Headers.ContentType = new MediaTypeHeaderValue(form.Adjunto.ContentType ?? "application/octet-stream");
                mp.Add(content, "Adjunto", form.Adjunto.FileName);
            }

            var resp = await cli.PostAsync("api/solicitudes", mp);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Error creando solicitud: {Status} - {Body}", resp.StatusCode, body);
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    var title = doc.RootElement.TryGetProperty("title", out var t) ? t.GetString() : "Error";
                    TempData["Error"] = title ?? "No se pudo enviar la solicitud.";
                }
                catch
                {
                    TempData["Error"] = "No se pudo enviar la solicitud. Intenta nuevamente.";
                }
                return View();
            }

            string? ticket = null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.TryGetProperty("ticket", out var t1)) ticket = t1.GetString();
                else if (root.TryGetProperty("Ticket", out var t2)) ticket = t2.GetString();
            }
            catch { /* ignore */ }

            if (string.IsNullOrWhiteSpace(ticket))
            {
                TempData["Error"] = "Se creó la solicitud, pero no se pudo obtener el número de ticket.";
                return View();
            }

            TempData["Success"] = true;
            TempData["Ticket"] = ticket;
            return RedirectToAction(nameof(Create));
        }

        // ============================
        // Tracking por TICKET (GET)
        // ============================
        [HttpGet] // /Solicitudes/Tracking?ticket=...
        public async Task<IActionResult> Tracking(string? ticket, CancellationToken ct)
        {
            ViewBag.QueryTried = !string.IsNullOrWhiteSpace(ticket);

            if (string.IsNullOrWhiteSpace(ticket))
                return View(model: null); // solo muestra el buscador

            var cli = _factory.CreateClient();
            cli.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}/");

            // Tu API expone: GET /api/solicitudes/by-ticket/{ticket}
            var resp = await cli.GetAsync($"api/solicitudes/by-ticket/{Uri.EscapeDataString(ticket)}", ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogInformation("Tracking ticket {Ticket} API status: {StatusCode}", ticket, resp.StatusCode);
                return View(model: null); // Ticket no encontrado
            }

            var model = await resp.Content.ReadFromJsonAsync<SolicitudDto>(cancellationToken: ct);
            return View(model); // View = Views/Solicitudes/Tracking.cshtml
        }

    }
}
