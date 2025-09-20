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
                return RedirectToAction("Index",
                    User.IsInRole("Administrador") ? "Home" : "LayoutPrincipal" /*o "Vendedor"*/);
            }
            return View(); // invitados ven la página principal pública
        }

        public IActionResult Nosotros()
        {
            return View();
        }

       
    }
}
