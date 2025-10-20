using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SkyNet.Controllers
{
    public class LayoutPrincipalController : Controller
    {
       
        public IActionResult Index()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Administrador"))
                    return RedirectToAction("Index", "AdministradorDashboard");

                if (User.IsInRole("Supervisor"))
                    return RedirectToAction("Index", "SupervisorDashboard");

                if (User.IsInRole("Tecnico"))
                    return RedirectToAction("Index", "TecnicoDashboard");


                return RedirectToAction("AccessDenied", "Account");

            }

            return View();
        }

        public IActionResult Nosotros()
        {
            return View();
        }

       
    }
}
