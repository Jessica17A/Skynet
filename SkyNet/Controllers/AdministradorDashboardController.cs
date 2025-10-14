using System.Data;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkyNet.Data;
using SkyNet.Models.DTOs;

namespace SkyNet.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdministradorDashboardController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdministradorDashboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Index() => View();

  
        [HttpGet]
        public async Task<IActionResult> Kpis()
        {
            var cn = _db.Database.GetDbConnection();

            var p = new DynamicParameters();
            p.Add("@FechasEnUtc", 0, DbType.Boolean);

            var result = await cn.QueryFirstOrDefaultAsync<DashboardAdminDto>(
                "dbo.usp_VisitasGlobales_KPIs_Estados",
                p,
                commandType: CommandType.StoredProcedure
            );

            return Json(result ?? new DashboardAdminDto());
        }
    }
}
