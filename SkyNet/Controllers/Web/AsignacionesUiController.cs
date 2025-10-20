using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyNet.Data;
using SkyNet.Models.DTOs;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;


[Route("ui/asignaciones/[action]")]
[Authorize]
public class AsignacionesUiController : Controller
{
    private readonly ILogger<AsignacionesUiController> _log;
    private readonly IHttpClientFactory _http;
    private readonly ApplicationDbContext _db;


    public AsignacionesUiController(
    ILogger<AsignacionesUiController> log,
    IHttpClientFactory http,
    ApplicationDbContext db)
    {
        _log = log;
        _http = http;
        _db = db;
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



    [HttpGet("{id:long}")]
    public async Task<IActionResult> Detalle(long id)
    {
        var c = _http.CreateClient();
        c.BaseAddress ??= new Uri($"{Request.Scheme}://{Request.Host}{Request.PathBase}/");

        SolicitudDto? sol = null;
        List<SolicitudAsignacionListadoS> data;

       
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
            data = await c.GetFromJsonAsync<List<SolicitudAsignacionListadoS>>(
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
    public async Task<IActionResult> Asignar(long solicitudId, CancellationToken ct)
    {
        var solicitud = await _db.Solicitudes
            .Where(s => s.Id == solicitudId)
            .Select(s => new SolicitudDto
            {
                Id = s.Id,
                Ticket = s.Ticket,
                Nombre = s.Nombre,
                Estado = s.Estado,
                Prioridad = s.Prioridad
            })
            .FirstOrDefaultAsync(ct);

        if (solicitud == null) return NotFound();

        var asignaciones = await _db.SolicitudAsignaciones
            .Where(a => a.FkSolicitud == solicitudId)
            .Select(a => new SolicitudAsignacionListadoS
            {
                Id = a.Id,
                FkSolicitud = a.FkSolicitud,
                FkTecnico = a.FkTecnico,
                IdGrupo = a.IdGrupo,
                Estado = (int)a.Estado,
                FechaAsignacionUtc = a.FechaAsignacionUtc,
                Fecha_Inicio = a.Fecha_Inicio,
                Notas = a.Notas,
                TecnicoNombre = _db.Empleados
                    .Where(e => e.Id == a.FkTecnico)
                    .Select(e => (e.Nombres + " " + e.Apellidos).Trim())
                    .FirstOrDefault(),
                SupervisorNombre = (
                    from g in _db.GruposSupervisoresTec
                    join s in _db.Empleados on g.FkSupervisor equals s.Id
                    where g.IdGrupo == a.IdGrupo
                    select (s.Nombres + " " + s.Apellidos).Trim()
                ).FirstOrDefault()
            })
            .ToListAsync(ct);

        ViewBag.Solicitud = solicitud;
        ViewBag.SolicitudId = solicitudId; 
        return View(asignaciones);
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Asignar(long solicitudId, string[] TecnicosIds, string? notas, DateTime? fechaVisita)
    {
        if (solicitudId <= 0 || TecnicosIds is null || TecnicosIds.Length == 0)
        {
            TempData["Error"] = "Debe seleccionar al menos un técnico.";
            return RedirectToAction("Asignar", new { solicitudId });
        }

        var c = _http.CreateClient();
        c.BaseAddress ??= new Uri($"{Request.Scheme}://{Request.Host}{Request.PathBase}/");

        // 🔹 Obtener el ID del usuario autenticado
        var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);

        int okCount = 0;
        var errores = new List<string>();

        foreach (var item in TecnicosIds)
        {
            var partes = item.Split('|');

            // 🔹 Incluir el UserId al DTO
            var dto = new SolicitudAsignacionCreateDto
            {
                IdSolicitud = solicitudId,
                IdGrupo = int.Parse(partes[0]),
                FkTecnico = long.Parse(partes[1]),
                Notas = notas,
                Fecha_Inicio = fechaVisita,
                UserId = userId    // <---- aquí lo agregamos
            };

            var resp = await c.PostAsJsonAsync($"api/solicitudes/{solicitudId}/asignaciones", dto);
            var json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                errores.Add($"Error en grupo {dto.IdGrupo}: {resp.StatusCode}");
                continue;
            }

            var data = JsonSerializer.Deserialize<JsonElement>(json);
            bool ok = data.GetProperty("ok").GetBoolean();
            string msg = data.GetProperty("msg").GetString() ?? "Sin mensaje";

            if (ok)
                okCount++;
            else
                errores.Add(msg);
        }

        if (okCount > 0)
            TempData["Ok"] = $"Solicitud exitosa";

        if (errores.Any())
            TempData["Error"] = string.Join(" | ", errores);

        return RedirectToAction("Asignar", new { solicitudId });
    }


    //public async Task<IActionResult> Supervisores()
    //{
    //    var c = _http.CreateClient();
    //    c.BaseAddress ??= new Uri($"{Request.Scheme}://{Request.Host}{Request.PathBase}/");

    //    List<SolicitudAsignacionListado> data;

    //    try
    //    {
    //        // 🔹 Consumimos el endpoint específico del supervisor
    //        data = await c.GetFromJsonAsync<List<SolicitudAsignacionListado>>(
    //            "api/solicitudes/asignaciones/supervisor",
    //            HttpContext.RequestAborted
    //        ) ?? new();
    //    }
    //    catch (Exception ex)
    //    {
    //        _log.LogError(ex, "Error al cargar asignaciones del supervisor.");
    //        ModelState.AddModelError("", "No se pudo contactar el API.");
    //        data = new();
    //    }

    //    // Si no hay datos, muestra mensaje
    //    if (data.Count == 0)
    //        ViewBag.Mensaje = "No se encontraron asignaciones para tu usuario.";

    //    return View(data);
    //}

    public IActionResult Supervisores()
    {
        // Solo retorna la vista vacía
        return View();
    }




    public IActionResult Tecnicos()
    {
        // Solo retorna la vista, el consumo de la API se hará con JS
        return View();
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> DetalleTecnico(long id)

    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/";

        var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };

        var baseUri = new Uri(baseUrl);
        foreach (var kv in Request.Cookies)
            handler.CookieContainer.Add(baseUri, new Cookie(kv.Key, kv.Value));

        using var c = new HttpClient(handler) { BaseAddress = baseUri };
        c.DefaultRequestHeaders.Accept.ParseAdd("application/json");

        HttpResponseMessage resp;
        try
        {
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






