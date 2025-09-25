using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using SkyNet.Models.DTOs;

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

            var solicitud = await http.GetFromJsonAsync<SolicitudDto>($"api/solicitudes/{id}", cancellationToken: ct);
            return solicitud is null ? NotFound() : View(solicitud);
        }

        // FORMULARIO CREATE (GET)
        public IActionResult Create() => View(new SolicitudCreateDto());

        // FORMULARIO CREATE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SolicitudCreateDto form)
        {
            if (!ModelState.IsValid)
                return View(form);

            var http = _factory.CreateClient();
            http.BaseAddress ??= new Uri($"{Request.Scheme}://{Request.Host}/");

            var resp = await http.PostAsJsonAsync("api/solicitudes", form);

            if (resp.IsSuccessStatusCode)
            {
                TempData["Ok"] = "Solicitud creada correctamente.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, $"Error del API: {(int)resp.StatusCode} {resp.ReasonPhrase}");
            return View(form);
        }
    }
}
