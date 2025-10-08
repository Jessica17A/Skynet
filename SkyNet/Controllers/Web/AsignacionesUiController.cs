using Microsoft.AspNetCore.Mvc;
using SkyNet.Models.DTOs;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;


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

       
        try
        {
            sol = await c.GetFromJsonAsync<SolicitudDto>(
                $"api/solicitudes/{id}", HttpContext.RequestAborted);
        }
        catch
        {
           
        }

      
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

        
        if (!data.Any())
        {
            TempData["Info"] = "La solicitud no tiene asignaciones aún.";
            return RedirectToAction(nameof(Todas));
        }


        ViewBag.Solicitud = sol;
        return View(data); 
    }







    //[HttpGet("Asignar/{solicitudId:long}")]
    //public IActionResult Asignar(long solicitudId)
    //{
    //    ViewBag.SolicitudId = solicitudId;
    //    return View();
    //}

    [HttpGet]
    public IActionResult Asignar(long solicitudId)
    {
        ViewBag.SolicitudId = solicitudId;
        return View();
    }





    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Asignar(long solicitudId, string[] TecnicosIds, string? notas, DateTime? fechaVisita)
    {
        if (solicitudId <= 0 || TecnicosIds == null || TecnicosIds.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Debes seleccionar al menos un técnico.");
            ViewBag.SolicitudId = solicitudId;
            return View("~/Views/AsignacionesUi/Asignar.cshtml");
        }

        // Local -> UTC
        DateTime? visitaUtc = fechaVisita.HasValue
            ? DateTime.SpecifyKind(fechaVisita.Value, DateTimeKind.Local).ToUniversalTime()
            : (DateTime?)null;

        var c = _http.CreateClient();
        if (c.BaseAddress == null)
            c.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}{Request.PathBase}/");

        // >>> Reenviar cookie de autenticación <<<
        if (Request.Headers.TryGetValue("Cookie", out var cookie))
            c.DefaultRequestHeaders.Add("Cookie", cookie.ToString());

        var pares = TecnicosIds
            .Select(x => x?.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(p => p is { Length: 2 })
            .Select(p => new { IdGrupo = int.Parse(p![0]), FkTecnico = long.Parse(p![1]) })
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
            if (resp.IsSuccessStatusCode) { okCount++; continue; }

            var msg = await resp.Content.ReadAsStringAsync();
            errores.Add($"Grupo {p.IdGrupo}: {resp.StatusCode} - {msg}");
        }

        if (okCount > 0) TempData["Ok"] = "Asignación creada y estado actualizado.";
        if (errores.Count > 0) TempData["Error"] = string.Join(" | ", errores);

        return RedirectToAction("Details", "Solicitudes", new { id = solicitudId });
    }



    public IActionResult Supervisores()
    {
        // Solo retorna la vista, el consumo de la API se hará con JS
        return View();
    }


    public IActionResult Tecnicos()
    {
        // Solo retorna la vista, el consumo de la API se hará con JS
        return View();
    }


    public async Task<IActionResult> DetalleTecnico(long id)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/"; // ← barra final

        var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };

        // Copiar cookies actuales al mismo host/path
        var baseUri = new Uri(baseUrl);
        foreach (var kv in Request.Cookies)
            handler.CookieContainer.Add(baseUri, new Cookie(kv.Key, kv.Value));

        using var c = new HttpClient(handler) { BaseAddress = baseUri };
        c.DefaultRequestHeaders.Accept.ParseAdd("application/json");

        HttpResponseMessage resp;
        try
        {
            // ruta relativa OK porque BaseAddress tiene barra final
            resp = await c.GetAsync($"api/solicitudes/{id}/asignaciones/tecnico/detalle", HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            ViewBag.Mensaje = $"No se pudo contactar el API: {ex.Message}";
            return View(model: null);
        }

        if (resp.StatusCode == HttpStatusCode.Unauthorized) return Challenge();
        if (resp.StatusCode == HttpStatusCode.NotFound)
        {
            ViewBag.Mensaje = "No tienes asignación en esta solicitud.";
            return View(model: null);
        }
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            ViewBag.Mensaje = $"No se pudo cargar el detalle. HTTP {(int)resp.StatusCode} — {body}";
            return View(model: null);
        }

        var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var detalle = await resp.Content.ReadFromJsonAsync<SolicitudAsignacionDetalleDto>(jsonOpts, HttpContext.RequestAborted);

        if (detalle == null)
        {
            ViewBag.Mensaje = "No se encontró información de detalle.";
            return View(model: null);
        }

        return View(detalle);
    }





}
