using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using SkyNet.Models.DTOs;

namespace SkyNet.Controllers.Web
{
    public class EmpleadosUiController : Controller
    {
        private readonly IHttpClientFactory _factory;
        public EmpleadosUiController(IHttpClientFactory factory) => _factory = factory;

        // LISTA
        public async Task<IActionResult> Index()
        {
            var http = _factory.CreateClient();
            http.BaseAddress ??= new Uri($"{Request.Scheme}://{Request.Host}/");

            var lista = await http.GetFromJsonAsync<List<EmpleadoDTO>>("api/empleados") ?? new();
            return View(lista);
        }

        // DETALLE
        public async Task<IActionResult> Detalle(long id)
        {
            var http = _factory.CreateClient();
            http.BaseAddress ??= new Uri($"{Request.Scheme}://{Request.Host}/");

            var emp = await http.GetFromJsonAsync<EmpleadoDTO>($"api/empleados/{id}");
            return emp is null ? NotFound() : View(emp);
        }

        // GET: crear (formulario)
        [HttpGet]
        public IActionResult Create()=> View(new EmpleadoCrearDTO());

        // POST: crear (env�a al API)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmpleadoCrearDTO form)
        {
            if (!ModelState.IsValid)
                return View(form);

            var http = _factory.CreateClient();
            http.BaseAddress ??= new Uri($"{Request.Scheme}://{Request.Host}/");

            var resp = await http.PostAsJsonAsync("api/empleados", form);

            if (resp.IsSuccessStatusCode)
            {
                TempData["Ok"] = "Empleado creado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            // Mostrar error que devuelve el API (si viene)
            try
            {
                // mismo patr�n que en clientes
                var apiErr = await resp.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                if (apiErr is not null && apiErr.TryGetValue("error", out var msg))
                    ModelState.AddModelError(string.Empty, msg);
                else
                    ModelState.AddModelError(string.Empty, $"Error del API: {(int)resp.StatusCode} {resp.ReasonPhrase}");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, $"Error del API: {(int)resp.StatusCode} {resp.ReasonPhrase}");
            }

            return View(form);
        }
    }
}
