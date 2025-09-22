// Controllers/UsuariosController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyNet.Data;                    // tu DbContext
using SkyNet.Models.Usuarios;

public class UsuariosController : Controller
{
    private readonly ApplicationDbContext _context;
    public UsuariosController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        // base: users + (left join) empleados
        var qBase =
            from u in _context.Users.AsNoTracking()
            join e in _context.Empleados.AsNoTracking() on u.Id equals e.UserId into ue
            from e in ue.DefaultIfEmpty()
            select new
            {
                u.Id,
                u.UserName,
                u.Email,
                Empleado = e
            };

        var baseList = await qBase.ToListAsync();

        // 1) Trae todos los userIds que aparecerán en el grid
        var userIds = baseList.Select(x => x.Id).ToList();

        // 2) Trae (en una sola consulta) los roles de esos usuarios
        //    y pásalos a memoria (ToListAsync). El GroupBy y string.Join
        //    se hacen EN C# (no en SQL), evitando el error.
        var rolesRaw = await (
            from ur in _context.UserRoles.AsNoTracking()
            join r in _context.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where userIds.Contains(ur.UserId)
            select new { ur.UserId, RoleName = r.Name }
        ).ToListAsync();

        var rolesDict = rolesRaw
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => string.Join(", ", g.Select(x => x.RoleName))
            );

        // 3) Proyecta al ViewModel
        var model = baseList.Select(x => new UsuarioListadoVm
        {
            UserId = x.Id,
            UserName = x.UserName ?? "",
            Email = x.Email ?? "",
            Roles = rolesDict.TryGetValue(x.Id, out var r) ? r : "",

            EmpleadoId = x.Empleado != null ? (long?)x.Empleado.Id : null,  // <- long?
            Nombres = x.Empleado?.Nombres,
            Apellidos = x.Empleado?.Apellidos,
            Cargo = x.Empleado?.Cargo,
            Estado = x.Empleado?.Estado
        }).ToList();

        return View(model);
    }


    // (opcionales) acciones que llamarás desde la vista:
    // public async Task<IActionResult> ToggleEstado(string id) { ... }
    // public async Task<IActionResult> ResetPassword(string id) { ... }
}
