using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SkyNet.Controllers
{
    public class LayoutPrincipal : Controller
    {
        [AllowAnonymous]
        public IActionResult Index()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Administrador"))
                    return RedirectToAction("Index", "Home");

                if (User.IsInRole("Supervisor"))
                    return RedirectToAction("Index", "Home");

                if (User.IsInRole("Tecnico"))
                    return RedirectToAction("Index", "TecnicoDashboard");

                // fallback
                return RedirectToAction("Index", "LayoutPrincipal");
            }

            return View(); // si no está autenticado
        }





        public IActionResult Nosotros()
        {
            return View();
        }

       
    }
}
