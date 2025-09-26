using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SkyNet.Models.DTOs;

namespace SkyNet.Controllers.Web
{
    public class GruposUiController : Controller
    {
        private readonly IHttpClientFactory _http;
        private readonly ILogger<GruposUiController> _log;

        public GruposUiController(IHttpClientFactory http, ILogger<GruposUiController> log)
        {
            _http = http;
            _log = log;
        }

        // Crea un HttpClient con BaseAddress al mismo sitio (http/https + host:puerto)
        private HttpClient CreateClient()
        {
            var client = _http.CreateClient();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            client.BaseAddress = new Uri(baseUrl);
            return client;
        }

        // ---------------------------
        // Index: lista de grupos
        // ---------------------------
        // GET /GruposUi?sup=123
        public async Task<IActionResult> Index(long? sup)
        {
            try
            {
                using var c = CreateClient();

                // Lookups para el combo de supervisores
                var lookups = await c.GetFromJsonAsync<LookupsDto>("/api/grupos/lookups");
                ViewBag.Supervisores = lookups?.supervisores ?? new List<OpcionEmpleadoDto>();
                ViewBag.SupSel = sup ?? 0L;

                // Lista de grupos (filtrada o no)
                var path = "/api/grupos";
                if (sup.HasValue && sup.Value > 0)
                    path += $"?sup={sup.Value}";

                var data = await c.GetFromJsonAsync<List<GrupoItemDto>>(path);
                return View(data ?? new List<GrupoItemDto>());
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error cargando Index de Grupos");
                TempData["ok"] = false;
                return View(new List<GrupoItemDto>());
            }
        }

        // ---------------------------
        // Mis técnicos (del supervisor logueado)
        // ---------------------------
        // GET /GruposUi/MisTecnicos
        public async Task<IActionResult> MisTecnicos()
        {
            try
            {
                using var c = CreateClient();
                // Puedes consumir /api/grupos?mine=true (lista de GrupoItemDto)
                var data = await c.GetFromJsonAsync<List<GrupoItemDto>>("/api/grupos?mine=true");
                return View(data ?? new List<GrupoItemDto>());
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error cargando MisTecnicos");
                return View(new List<GrupoItemDto>());
            }
        }

        // ---------------------------
        // Crear grupos (GET+POST)
        // ---------------------------
        // GET /GruposUi/Create
        public async Task<IActionResult> Create()
        {
            using var c = CreateClient();
            var lookups = await c.GetFromJsonAsync<LookupsDto>("/api/grupos/lookups");

            var vm = new GrupoCreateVm
            {
                Supervisores = lookups?.supervisores ?? new List<OpcionEmpleadoDto>(),
                Tecnicos = lookups?.tecnicos ?? new List<OpcionEmpleadoDto>()
            };
            return View(vm);
        }

        // POST /GruposUi/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GrupoCreateVm vm)
        {
            if (vm == null || vm.SupervisorId <= 0 || vm.TecnicosIds == null || vm.TecnicosIds.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Seleccione un supervisor y al menos un técnico.");
            }

            if (!ModelState.IsValid)
            {
                using var cRe = CreateClient();
                var lkp = await cRe.GetFromJsonAsync<LookupsDto>("/api/grupos/lookups");
                vm.Supervisores = lkp?.supervisores ?? new List<OpcionEmpleadoDto>();
                vm.Tecnicos = lkp?.tecnicos ?? new List<OpcionEmpleadoDto>();
                return View(vm);
            }

            try
            {
                using var c = CreateClient();
                var payload = new GrupoCreateDto
                {
                    SupervisorId = vm.SupervisorId,
                    TecnicosIds = vm.TecnicosIds
                };

                var resp = await c.PostAsJsonAsync("/api/grupos", payload);
                if (resp.IsSuccessStatusCode)
                {
                    TempData["ok"] = true;
                    return RedirectToAction(nameof(Index), new { sup = vm.SupervisorId });
                }

                var msg = await resp.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Error al crear: {resp.StatusCode} - {msg}");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error en Create (POST)");
                ModelState.AddModelError(string.Empty, "Error inesperado al crear el grupo.");
            }

            // Reponer lookups al re-renderizar la vista
            using var c2 = CreateClient();
            var lookups = await c2.GetFromJsonAsync<LookupsDto>("/api/grupos/lookups");
            vm.Supervisores = lookups?.supervisores ?? new List<OpcionEmpleadoDto>();
            vm.Tecnicos = lookups?.tecnicos ?? new List<OpcionEmpleadoDto>();
            return View(vm);
        }

        // ---------------------------
        // Toggle estado
        // ---------------------------
        // POST /GruposUi/Toggle/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id, long? sup)
        {
            try
            {
                using var c = CreateClient();
                var resp = await c.PostAsync($"/api/grupos/{id}/toggle", content: null);
                TempData["ok"] = resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error al alternar estado del grupo {Id}", id);
                TempData["ok"] = false;
            }

            return RedirectToAction(nameof(Index), new { sup });
        }

        // ---------------------------
        // Eliminar (DELETE)
        // ---------------------------
        // POST /GruposUi/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, long? sup)
        {
            try
            {
                using var c = CreateClient();
                var resp = await c.DeleteAsync($"/api/grupos/{id}");
                TempData["ok"] = resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error al eliminar grupo {Id}", id);
                TempData["ok"] = false;
            }

            return RedirectToAction(nameof(Index), new { sup });
        }

        // ---------------------------
        // Tipos auxiliares para lookups/vm
        // ---------------------------
        private class LookupsDto
        {
            public List<OpcionEmpleadoDto> supervisores { get; set; } = new();
            public List<OpcionEmpleadoDto> tecnicos { get; set; } = new();
        }

        public class GrupoCreateVm
        {
            public long SupervisorId { get; set; }
            public List<long> TecnicosIds { get; set; } = new();

            // Para pintar selects en la vista GET/POST
            public List<OpcionEmpleadoDto> Supervisores { get; set; } = new();
            public List<OpcionEmpleadoDto> Tecnicos { get; set; } = new();
        }
    }
}
