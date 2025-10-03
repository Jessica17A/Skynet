using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using SkyNet.Models.DTOs;


public class AsignacionesUiController : Controller
{
    private readonly ILogger<AsignacionesUiController> _log;
    private readonly IHttpClientFactory _http;

    public AsignacionesUiController(ILogger<AsignacionesUiController> log, IHttpClientFactory http)
    {
        _log = log; _http = http;
    }

    [HttpGet("Index")]
    public IActionResult Index() => View();

    public async Task<IActionResult> Todas()
    {
        var c = _http.CreateClient();
        c.BaseAddress ??= new Uri($"{Request.Scheme}://{Request.Host}{Request.PathBase}/");

        List<SolicitudAsignacionListado> data;
        try
        {
            data = await c.GetFromJsonAsync<List<SolicitudAsignacionListado>>(
                "api/solicitudes/asignaciones", HttpContext.RequestAborted
            ) ?? new();
        }
        catch
        {
            // Manejo simple de error
            ModelState.AddModelError("", "No se pudo contactar el API.");
            data = new();
        }

        return View(data);
    }




    public async Task<IActionResult> Detalle(long id)
    {
        var c = _http.CreateClient();
        c.BaseAddress ??= new Uri($"{Request.Scheme}://{Request.Host}{Request.PathBase}/");

        SolicitudDto? sol = null;
        List<SolicitudAsignacionListado> data;

        // 1) Trae la solicitud para el encabezado/stepper
        try
        {
            sol = await c.GetFromJsonAsync<SolicitudDto>(
                $"api/solicitudes/{id}", HttpContext.RequestAborted);
        }
        catch
        {
            // ignora; validamos después
        }

        // 2) Trae las asignaciones
        try
        {
            data = await c.GetFromJsonAsync<List<SolicitudAsignacionListado>>(
                $"api/solicitudes/{id}/asignaciones", HttpContext.RequestAborted
            ) ?? new();
        }
        catch
        {
            ModelState.AddModelError("", "No se pudo contactar el API.");
            data = new();
        }

        if (sol == null)
        {
            TempData["Error"] = "No se pudo cargar el detalle de la solicitud.";
            return RedirectToAction("Index", "Solicitudes");
        }

        // Si quieres mostrar una vista vacía cuando no hay asignaciones
        if (!data.Any())
        {
            TempData["Info"] = "La solicitud no tiene asignaciones aún.";
            return RedirectToAction("Details", "Solicitudes", new { id });
        }


        ViewBag.Solicitud = sol;
        return View(data); // El Model de la vista será la lista de asignaciones
    }






    // GET: /AsignacionesUi/Asignar?solicitudId=123
    [HttpGet("Asignar")]
    public IActionResult Asignar(long solicitudId)
    {
        ViewBag.SolicitudId = solicitudId;
        return View(); 
    }

    
    [HttpPost("Asignar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Asignar(long solicitudId, string[] TecnicosIds, string? notas, DateTime? fechaVisita)
    {
        if (solicitudId <= 0 || TecnicosIds == null || TecnicosIds.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Debes seleccionar al menos un técnico.");
            ViewBag.SolicitudId = solicitudId;
            return View("~/Views/AsignacionesUi/Asignar.cshtml");
        }

        // Fecha local -> UTC
        DateTime? visitaUtc = null;
        if (fechaVisita.HasValue)
            visitaUtc = DateTime.SpecifyKind(fechaVisita.Value, DateTimeKind.Local).ToUniversalTime();

       
        var c = _http.CreateClient();
        if (c.BaseAddress == null)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/";
            c.BaseAddress = new Uri(baseUrl);   
        }

        var pares = TecnicosIds
            .Select(x => x?.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(x => x != null && x.Length == 2)
            .Select(x => new { IdGrupo = int.Parse(x![0]), FkTecnico = long.Parse(x![1]) })
            .ToList();

        var errores = new List<string>();
        int okCount = 0;

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

            var resp = await c.PostAsJsonAsync($"api/solicitudes/{solicitudId}/asignaciones", payload);

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
            var patch = await c.PatchAsync($"api/solicitudes/{solicitudId}/estado",
                new StringContent("{\"estado\":3}", System.Text.Encoding.UTF8, "application/json"));
            if (!patch.IsSuccessStatusCode)
            {
                var msg = await patch.Content.ReadAsStringAsync();
                errores.Add($"Se asignó técnico(s), pero falló el cambio de estado: {patch.StatusCode} - {msg}");
            }
        }

        if (errores.Count > 0)
        {
            ModelState.AddModelError(string.Empty, "Resultado parcial:\n" + string.Join("\n", errores));
            ViewBag.SolicitudId = solicitudId;
            return View("~/Views/AsignacionesUi/Asignar.cshtml");
        }

        TempData["ok"] = true;
        return RedirectToAction("Detalle", "AsignacionesUi", new { solicitudId });
    }

}
