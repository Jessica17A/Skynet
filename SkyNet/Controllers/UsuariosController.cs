
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyNet.Data;                   
using SkyNet.Models.Usuarios;

public class UsuariosController : Controller
{
    private readonly ApplicationDbContext _context;
    public UsuariosController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
       
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

     
        var userIds = baseList.Select(x => x.Id).ToList();

   
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

        
        var model = baseList.Select(x => new UsuarioListadoVm
        {
            UserId = x.Id,
            UserName = x.UserName ?? "",
            Email = x.Email ?? "",
            Roles = rolesDict.TryGetValue(x.Id, out var r) ? r : "",

            EmpleadoId = x.Empleado != null ? (long?)x.Empleado.Id : null,  
            Nombres = x.Empleado?.Nombres,
            Apellidos = x.Empleado?.Apellidos,
            Cargo = x.Empleado?.Cargo,
            Estado = x.Empleado?.Estado
        }).ToList();

        return View(model);
    }


   
}
