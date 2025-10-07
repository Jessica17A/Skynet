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





    //public async Task<IActionResult> DetalleTecnico(long id)
    //{
    //    var c = _http.CreateClient();
    //    c.BaseAddress ??= new Uri($"{Request.Scheme}://{Request.Host}{Request.PathBase}/");

    //    // 1) Traer la solicitud (igual que en Detalle general)
    //    SolicitudDto? sol = null;
    //    try
    //    {
    //        sol = await c.GetFromJsonAsync<SolicitudDto>(
    //            $"api/solicitudes/{id}", HttpContext.RequestAborted);
    //    }
    //    catch { }

    //    if (sol == null)
    //    {
    //        TempData["Error"] = "No se pudo cargar el detalle de la solicitud.";
    //        return RedirectToAction("Index", "Solicitudes");
    //    }

    //    // 2) Traer SOLO la asignación del técnico logueado
    //    List<SolicitudAsignacionListado> data;
    //    try
    //    {
    //        // si prefieres 1 solo registro, cambia el endpoint a .../tecnico/detalle y ajusta la vista
    //        data = await c.GetFromJsonAsync<List<SolicitudAsignacionListado>>(
    //            $"api/solicitudes/{id}/asignaciones/tecnico", HttpContext.RequestAborted
    //        ) ?? new();
    //    }
    //    catch
    //    {
    //        ModelState.AddModelError("", "No se pudo contactar el API.");
    //        data = new();
    //    }

    //    if (!data.Any())
    //    {
    //        TempData["Info"] = "No tienes asignación en esta solicitud.";
    //        return RedirectToAction(nameof(Tecnicos)); // vuelve al listado del técnico
    //    }

    //    ViewBag.Solicitud = sol;
    //    ViewBag.SolicitudId = id;
    //    return View();              // si tu vista consume con JS
    //                                // return View("DetalleTecnico", data); // si tu vista espera el Model en servidor
    //}


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
