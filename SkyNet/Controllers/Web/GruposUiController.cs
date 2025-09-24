using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SkyNet.Data;
using SkyNet.Models;

namespace SkyNet.Controllers.Web
{
    public class GruposUiController : Controller
    {
        private readonly ApplicationDbContext _db;
        public GruposUiController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(long? sup)
        {
            var q = _db.GruposSupervisoresTec
                .AsNoTracking()
                .Include(x => x.Supervisor)
                .Include(x => x.Tecnico)
                .Where(x => x.Estado);

            if (sup.HasValue && sup.Value > 0)
                q = q.Where(x => x.FkSupervisor == sup.Value);

            var lista = await q.OrderByDescending(x => x.IdGrupo).ToListAsync();

            ViewBag.Supervisores = await _db.Empleados
                .Where(e => e.Estado == 1 && e.Cargo == "Supervisor")
                .Select(e => new { e.Id, Nombre = e.Nombres + " " + e.Apellidos })
                .OrderBy(x => x.Nombre)
                .ToListAsync();

            ViewBag.SupSel = sup ?? 0L;
            return View(lista);
        }

        public async Task<IActionResult> Create()
        {
            await CargarSelects();
            return View(new Models.GrupoCreateVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GrupoCreateVm vm)
        {
            if (!ModelState.IsValid)
            {
                await CargarSelects();
                return View(vm);
            }

            var tecnicos = (vm.TecnicosIds ?? new List<long>()).Distinct().ToList();
            if (tecnicos.Count == 0)
            {
                ModelState.AddModelError(nameof(vm.TecnicosIds), "Seleccione al menos un técnico.");
                await CargarSelects();
                return View(vm);
            }

            foreach (var tecId in tecnicos)
            {
                var dup = await _db.GruposSupervisoresTec
                    .AnyAsync(g => g.FkSupervisor == vm.SupervisorId && g.FkTecnico == tecId);

                if (!dup)
                {
                    _db.GruposSupervisoresTec.Add(new GrupoSupervisorTec
                    {
                        FkSupervisor = vm.SupervisorId,
                        FkTecnico = tecId,
                        Estado = true
                    });
                }
            }

            await _db.SaveChangesAsync();
            TempData["ok"] = true;
            return RedirectToAction(nameof(Index));


        }



        [HttpPost]
        public async Task<IActionResult> Toggle(int id)
        {
            var g = await _db.GruposSupervisoresTec.FindAsync(id);
            if (g == null) return NotFound();

            g.Estado = !g.Estado;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var g = await _db.GruposSupervisoresTec.FindAsync(id);
            if (g == null) return NotFound();

            _db.GruposSupervisoresTec.Remove(g);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarSelects()
        {
            // Supervisores activos
            ViewBag.Supervisores = await _db.Empleados
                .Where(e => e.Estado == 1 && e.Cargo == "Supervisor")
                .OrderBy(e => e.Nombres).ThenBy(e => e.Apellidos)
                .Select(e => new SelectListItem { Value = e.Id.ToString(), Text = e.Nombres + " " + e.Apellidos })
                .ToListAsync();

            // IDs de técnicos ya asignados a cualquier supervisor (solo relaciones activas si usas Estado)
            var tecnicosAsignados = await _db.GruposSupervisoresTec
                .Where(g => g.Estado == true)           // <- si no usas Estado, quita esta línea
                .Select(g => g.FkTecnico)
                .Distinct()
                .ToListAsync();

            // Técnicos disponibles = activos, cargo 'Tecnico' y NO asignados aún
            ViewBag.Tecnicos = await _db.Empleados
                .Where(e => e.Estado == 1 && e.Cargo == "Tecnico" && !tecnicosAsignados.Contains(e.Id))
                .OrderBy(e => e.Nombres).ThenBy(e => e.Apellidos)
                .Select(e => new SelectListItem { Value = e.Id.ToString(), Text = e.Nombres + " " + e.Apellidos })
                .ToListAsync();
        }
    }
}
