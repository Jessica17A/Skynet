using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using SkyNet.Models.DTOs;

namespace SkyNet.Controllers.Web;

public class ClientesUiController : Controller
{
    private readonly IHttpClientFactory _factory;
    public ClientesUiController(IHttpClientFactory factory) => _factory = factory;

    // LISTA
    public async Task<IActionResult> Index()
    {
        var http = _factory.CreateClient();
        http.BaseAddress ??= new Uri($"{Request.Scheme}://{Request.Host}/");

        var lista = await http.GetFromJsonAsync<List<ClienteDTO>>("api/clientes") ?? new();
        return View(lista);
    }

    // DETALLE
    public async Task<IActionResult> Detalle(long id)
    {
        var http = _factory.CreateClient();
        http.BaseAddress ??= new Uri($"{Request.Scheme}://{Request.Host}/");

        var cli = await http.GetFromJsonAsync<ClienteDTO>($"api/clientes/{id}");
        return cli is null ? NotFound() : View(cli);
    }

    // GET: crear (formulario)
    [HttpGet]
    public IActionResult Create()
        => View(new ClienteCrearDTO());

    // POST: crear (envía al API)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClienteCrearDTO form)
    {
        if (!ModelState.IsValid)
            return View(form);

        var http = _factory.CreateClient();
        http.BaseAddress ??= new Uri($"{Request.Scheme}://{Request.Host}/");

        var resp = await http.PostAsJsonAsync("api/clientes", form);

        if (resp.IsSuccessStatusCode)
        {
            TempData["Ok"] = "Cliente creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // Mostrar error que devuelve el API (si viene)
        try
        {
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
