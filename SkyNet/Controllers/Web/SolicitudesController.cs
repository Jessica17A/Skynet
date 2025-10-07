using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SkyNet.Models.DTOs;
using System.Net.Http.Json;

namespace SkyNet.Controllers.Web
{
    [Authorize]
    public class SolicitudesController : Controller
    {
        private readonly IHttpClientFactory _factory;
        private readonly ILogger<SolicitudesController> _logger;

        public SolicitudesController(IHttpClientFactory factory, ILogger<SolicitudesController> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        // LISTADO
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var http = _factory.CreateClient();
            http.BaseAddress ??= new Uri($"{Request.Scheme}://{Request.Host}/");

            List<SolicitudDto> lista = new();
            try
            {
                lista = await http.GetFromJsonAsync<List<SolicitudDto>>("api/solicitudes", cancellationToken: ct)
                        ?? new List<SolicitudDto>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error llamando GET api/solicitudes");
                TempData["Error"] = "No se pudo cargar el listado de solicitudes.";
            }

            return View(lista); // Views/Solicitudes/Index.cshtml
        }

        // DETALLE
        public async Task<IActionResult> Details(long id, CancellationToken ct)
        {
            var http = _factory.CreateClient();
            http.BaseAddress ??= new Uri($"{Request.Scheme}://{Request.Host}/");

            SolicitudDto? sol = null;
            SolicitudAsignacionDto? asig = null;

            try
            {
                sol = await http.GetFromJsonAsync<SolicitudDto>($"api/solicitudes/{id}", ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cargando solicitud {Id}", id);
            }

            try
            {
                asig = await http.GetFromJsonAsync<SolicitudAsignacionDto>($"api/solicitudes/{id}/asignacion-activa", ct);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // sin asignación activa está bien
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "No se pudo obtener asignación activa para solicitud {Id}", id);
            }

            if (sol == null)
            {
                TempData["Error"] = "No se pudo cargar el detalle de la solicitud.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AsignacionActiva = asig;
            return View(sol);
        }

        // FORMULARIO CREATE (GET)
        public IActionResult Create() => View(new SolicitudCreateDto());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Create(SolicitudCreateDto form)
        {
            if (!ModelState.IsValid) return View(form);

            var http = _factory.CreateClient();
            http.BaseAddress ??= new Uri($"{Request.Scheme}://{Request.Host}/");

            var resp = await http.PostAsJsonAsync("api/solicitudes", form);

            if (resp.IsSuccessStatusCode) // 201 Created
            {
                var creado = await resp.Content.ReadFromJsonAsync<SolicitudDto>();

                TempData["Success"] = true;
                TempData["Ticket"] = creado?.Ticket ?? "";

                return RedirectToAction(nameof(Create));
            }

            var problema = await resp.Content.ReadAsStringAsync();
            ModelState.AddModelError("", $"Error del API: {(int)resp.StatusCode} {resp.ReasonPhrase}. {problema}");
            return View(form);
        }

        [HttpGet] // /Solicitudes/Tracking?ticket=...
        [AllowAnonymous]
        public async Task<IActionResult> Tracking(string? ticket, CancellationToken ct)
        {
            ViewBag.QueryTried = !string.IsNullOrWhiteSpace(ticket);

            if (string.IsNullOrWhiteSpace(ticket))
                return View(model: null); // solo muestra el buscador

            var cli = _factory.CreateClient();
            cli.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}/");

            var resp = await cli.GetAsync($"api/solicitudes/by-ticket/{Uri.EscapeDataString(ticket)}", ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogInformation("Tracking ticket {Ticket} API status: {StatusCode}", ticket, resp.StatusCode);
                return View(model: null); // Ticket no encontrado
            }

            var model = await resp.Content.ReadFromJsonAsync<SolicitudDto>(cancellationToken: ct);
            return View(model); // Views/Solicitudes/Tracking.cshtml
        }




        [HttpGet("{id:long}/historial")]
        public IActionResult Historial(long id)
        {
            ViewBag.SolicitudId = id;
            return View();
        }


    }
}
