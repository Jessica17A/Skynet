using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkyNet.Models;
using SkyNet.Models.DTOs;
using System.Net.Http;
using System.Net.Http.Json;

namespace SkyNet.Controllers.Web
{
    [Authorize]
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

      //lisstado
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

   
        // GET /GruposUi/MisTecnicos
        public async Task<IActionResult> MisTecnicos()
        {
            try
            {
                using var c = CreateClient();
              
                var data = await c.GetFromJsonAsync<List<GrupoItemDto>>("/api/grupos?mine=true");
                return View(data ?? new List<GrupoItemDto>());
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error cargando MisTecnicos");
                return View(new List<GrupoItemDto>());
            }
        }

        // GET /GruposUi/Create
        public async Task<IActionResult> Create([FromServices] SkyNet.Data.ApplicationDbContext _db)
        {
            // Supervisores activos
            var supervisores = await _db.Empleados
                .Where(e => (e.Cargo == "Supervisor" || e.Cargo == "Supervisór"))
                .OrderBy(e => e.Nombres).ThenBy(e => e.Apellidos)
                .Select(e => new OpcionEmpleadoDto
                {
                    Id = e.Id,
                    Nombre = e.Nombres + " " + e.Apellidos
                })
                .ToListAsync();

            // 🔹 Técnicos activos o inactivos (da igual) cuyo grupo tenga Estado = 0 o no tengan grupo
            var tecnicos = await _db.Empleados
                .Where(e => (e.Cargo == "Tecnico" || e.Cargo == "Técnico"))
                .Where(e => !_db.GruposSupervisoresTec
                    .Any(g => g.FkTecnico == e.Id && g.Estado == true))  // sólo excluir si está en grupo activo
                .OrderBy(e => e.Nombres).ThenBy(e => e.Apellidos)
                .Select(e => new OpcionEmpleadoDto
                {
                    Id = e.Id,
                    Nombre = e.Nombres + " " + e.Apellidos
                })
                .ToListAsync();

            var vm = new GrupoCreateVm
            {
                Supervisores = supervisores,
                Tecnicos = tecnicos
            };

            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
    [FromServices] SkyNet.Data.ApplicationDbContext _db,
    GrupoCreateVm model)
        {
            if (model.SupervisorId <= 0 || model.TecnicosIds.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Debe seleccionar un supervisor y al menos un técnico.");
            }

            if (!ModelState.IsValid)
            {
                // Recargar combos si hay error
                model.Supervisores = await _db.Empleados
                    .Where(e => e.Estado != 0 && e.Cargo == "Supervisor")
                    .Select(e => new OpcionEmpleadoDto
                    {
                        Id = e.Id,
                        Nombre = e.Nombres + " " + e.Apellidos
                    })
                    .ToListAsync();

                var ocupados = await _db.GruposSupervisoresTec
                    .Where(g => g.Estado == true)
                    .Select(g => g.FkTecnico)
                    .Distinct()
                    .ToListAsync();

                model.Tecnicos = await _db.Empleados
                    .Where(e => e.Estado != 0 && e.Cargo == "Tecnico" && !ocupados.Contains(e.Id))
                    .Select(e => new OpcionEmpleadoDto
                    {
                        Id = e.Id,
                        Nombre = e.Nombres + " " + e.Apellidos
                    })
                    .ToListAsync();

                return View(model);
            }

            // Guardar los técnicos seleccionados en la tabla intermedia
            foreach (var tecId in model.TecnicosIds.Distinct())
            {
                _db.GruposSupervisoresTec.Add(new GrupoSupervisorTec
                {
                    FkSupervisor = model.SupervisorId,
                    FkTecnico = tecId,
                    Estado = true,
                    FechaCreacionUtc = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            TempData["ok"] = true;

            return RedirectToAction(nameof(Index));
        }





        // eliminar
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





       
        // GET /GruposUi/Asignar?solicitudId=123
        [HttpGet]
        public IActionResult Asignar(long solicitudId)
        {
            ViewBag.SolicitudId = solicitudId;
            
            return View();
        }




        // POST /GruposUi/Asignar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asignar(long solicitudId, string[] TecnicosIds, string? notas, DateTime? fechaVisita)
        {
            if (solicitudId <= 0 || TecnicosIds == null || TecnicosIds.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Debes seleccionar al menos un técnico.");
                ViewBag.SolicitudId = solicitudId;
                return View(); 
            }

           
            DateTime? visitaUtc = null;
            if (fechaVisita.HasValue)
                visitaUtc = DateTime.SpecifyKind(fechaVisita.Value, DateTimeKind.Local).ToUniversalTime();

            var errores = new List<string>();
            int okCount = 0;

            try
            {
                using var c = CreateClient();

                var pares = TecnicosIds
                    .Select(x => x?.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .Where(x => x != null && x.Length == 2)
                    .Select(x => new { IdGrupo = int.Parse(x![0]), FkTecnico = long.Parse(x![1]) })
                    .ToList();

                foreach (var p in pares)
                {
                    var payload = new SolicitudAsignacionCreateDto
                    {
                        IdSolicitud = solicitudId,
                        IdGrupo = p.IdGrupo,
                        FkTecnico = p.FkTecnico,
                        Notas = notas,
                        Fecha_Inicio = visitaUtc 
                    };

                    var resp = await c.PostAsJsonAsync($"/api/solicitudes/{solicitudId}/asignaciones", payload);

                    if (resp.IsSuccessStatusCode)
                    {
                        okCount++;
                        continue;
                    }

                  
                    if ((int)resp.StatusCode == StatusCodes.Status200OK)
                    {
                        okCount++;
                        continue;
                    }

                    var msg = await resp.Content.ReadAsStringAsync();
                    errores.Add($"Grupo {p.IdGrupo}: {resp.StatusCode} - {msg}");
                }

                if (okCount > 0)
                {
               
                    var patchReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/solicitudes/{solicitudId}/estado")
                    {
                        Content = JsonContent.Create(new { estado = 3 })
                    };
                    var patchResp = await c.SendAsync(patchReq);
                    if (!patchResp.IsSuccessStatusCode)
                    {
                        var msg = await patchResp.Content.ReadAsStringAsync();
                        errores.Add($"Se asignó técnico(s), pero falló el cambio de estado: {patchResp.StatusCode} - {msg}");
                    }
                }

                if (errores.Count > 0)
                {
                    ModelState.AddModelError(string.Empty, "Resultado parcial:\n" + string.Join("\n", errores));
                    ViewBag.SolicitudId = solicitudId;
                    return View();
                }

                TempData["ok"] = true;
                return RedirectToAction("Details", "Solicitudes", new { id = solicitudId });
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error asignando técnicos a solicitud {Id}", solicitudId);
                ModelState.AddModelError(string.Empty, "Error inesperado al asignar.");
                ViewBag.SolicitudId = solicitudId;
                return View();
            }
        }
    }



}

