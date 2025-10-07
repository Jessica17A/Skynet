// Controllers/TecnicoDashboardController.cs
using System.Data;
using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SkyNet.Models.DTOs;

namespace SkyNet.Controllers
{
    [Authorize(Roles = "Tecnico")]
    public class TecnicoDashboardController : Controller
    {
        private readonly string _cn;
        public TecnicoDashboardController(IConfiguration cfg)
        {
            _cn = cfg.GetConnectionString("DefaultConnection")!;
        }

        [HttpGet]
        public IActionResult Index() => View();

        // KPIs mantienen el mismo SP y DTO (sin cambios)
        [HttpGet]
        public async Task<IActionResult> Kpis()
        {
            var aspNetUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(aspNetUserId)) return Unauthorized();

            using var cn = new SqlConnection(_cn);
            var p = new DynamicParameters();
            p.Add("@AspNetUserId", aspNetUserId, DbType.String, size: 450);
            p.Add("@FechasEnUtc", 0, DbType.Boolean);

            var kpis = await cn.QueryFirstOrDefaultAsync<DashboardTecnicoDto>(
                "dbo.usp_VisitasTecnico_KPIs_Estados", p, commandType: CommandType.StoredProcedure);

            return Json(kpis ?? new DashboardTecnicoDto());
        }
        // TecnicoDashboardController.cs (solo acción Hoy)
        [HttpGet]
        public async Task<IActionResult> Hoy(int? estado, bool finalizadasTodas = false)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(uid)) return Unauthorized();

            using var cn = new SqlConnection(_cn);
            var p = new DynamicParameters();
            p.Add("@AspNetUserId", uid, DbType.String, size: 450);
            p.Add("@FechasEnUtc", 0, DbType.Boolean);
            p.Add("@EstadoFiltro", estado, DbType.Byte);
            p.Add("@FinalizadasTodas", finalizadasTodas, DbType.Boolean); // << nuevo
            p.Add("@Top", 100, DbType.Int32);

            var list = await cn.QueryAsync<DashboardTecnicoDto>(
                "dbo.usp_VisitasTecnico_Hoy_Listar", p, commandType: CommandType.StoredProcedure);

            return Json(list);
        }

    }
}
