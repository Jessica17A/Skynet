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




    [HttpGet("Detalle/{solicitudId:long}")]
    public async Task<IActionResult> Detalle(long solicitudId)
    {
        var c = _http.CreateClient();

      
        if (c.BaseAddress is null)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/";
            c.BaseAddress = new Uri(baseUrl); 
        }

        // Usa ruta relativa (sin slash inicial)
        var sol = await c.GetFromJsonAsync<SolicitudDto>($"api/solicitudes/{solicitudId}");
        if (sol == null) return NotFound();

        ViewBag.Solicitud = sol;
        return View(); 
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
