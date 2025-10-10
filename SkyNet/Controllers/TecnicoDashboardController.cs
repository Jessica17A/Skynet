// Controllers/TecnicoDashboardController.cs
using System.Data;
using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyNet.Data;
using SkyNet.Models.DTOs;

namespace SkyNet.Controllers
{
    [Authorize(Roles = "Tecnico")]
    public class TecnicoDashboardController : Controller
    {
        private readonly ApplicationDbContext _db;

        public TecnicoDashboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Index() => View();

        // ======== KPIs =========
        [HttpGet]
        public async Task<IActionResult> Kpis()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var cn = _db.Database.GetDbConnection();

            var p = new DynamicParameters();
            p.Add("@AspNetUserId", userId, DbType.String, size: 450);
            p.Add("@FechasEnUtc", 0, DbType.Boolean);

            var result = await cn.QueryFirstOrDefaultAsync<DashboardTecnicoDto>(
                "dbo.usp_VisitasTecnico_KPIs_Estados",
                p,
                commandType: CommandType.StoredProcedure
            );

            return Json(result ?? new DashboardTecnicoDto());
        }

        // ======== Visitas de Hoy =========
        [HttpGet]
        public async Task<IActionResult> Hoy(int? estado, bool finalizadasTodas = false)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var cn = _db.Database.GetDbConnection();

            var p = new DynamicParameters();
            p.Add("@AspNetUserId", userId, DbType.String, size: 450);
            p.Add("@FechasEnUtc", 0, DbType.Boolean);
            p.Add("@EstadoFiltro", estado, DbType.Byte);
            p.Add("@FinalizadasTodas", finalizadasTodas, DbType.Boolean);
            p.Add("@Top", 100, DbType.Int32);

            var list = await cn.QueryAsync<DashboardTecnicoDto>(
                "dbo.usp_VisitasTecnico_Hoy_Listar",
                p,
                commandType: CommandType.StoredProcedure
            );

            return Json(list);
        }
    }
}
