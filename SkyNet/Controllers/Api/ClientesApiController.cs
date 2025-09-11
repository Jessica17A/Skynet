using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyNet.Data;
using SkyNet.Models;
using SkyNet.Models.DTOs;

namespace SkyNet.Controllers.Api;

[ApiController]
[Route("api/clientes")]
[Produces("application/json")]
public class ClientesApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ClientesApiController(ApplicationDbContext db) => _db = db;

    // GET: /api/clientes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClienteDTO>>> GetAll(CancellationToken ct)
    {
        var data = await _db.Clientes.AsNoTracking()
            .Select(c => new ClienteDTO { Id = c.Id, Nombre = c.Nombre, Email = c.Email })
            .ToListAsync(ct);
        return Ok(data);
    }

    // GET: /api/clientes/{id}
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ClienteDTO>> GetById(long id, CancellationToken ct)
    {
        var dto = await _db.Clientes.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ClienteDTO { Id = x.Id, Nombre = x.Nombre, Email = x.Email })
            .FirstOrDefaultAsync(ct);

        return dto is null ? NotFound() : Ok(dto);
    }

    // POST: /api/clientes
    [HttpPost]
    public async Task<ActionResult<ClienteDTO>> Create([FromBody] ClienteCrearDTO dto, CancellationToken ct)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest(new { error = "El nombre es obligatorio" });

        var entidad = new Cliente
        {
            Nombre = dto.Nombre.Trim(),
            Email  = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim()
        };

        _db.Clientes.Add(entidad);
        await _db.SaveChangesAsync(ct);

        var outDto = new ClienteDTO { Id = entidad.Id, Nombre = entidad.Nombre, Email = entidad.Email };
        return CreatedAtAction(nameof(GetById), new { id = outDto.Id }, outDto);
    }

    // PUT: /api/clientes/{id}
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] ClienteEditarDTO dto, CancellationToken ct)
    {
        var entidad = await _db.Clientes.FindAsync([id], ct);
        if (entidad is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(dto?.Nombre)) entidad.Nombre = dto!.Nombre.Trim();
        if (!string.IsNullOrWhiteSpace(dto?.Email))  entidad.Email  = dto!.Email!.Trim();

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
