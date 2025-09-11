using Microsoft.AspNetCore.Mvc;

namespace SkyNet.Controllers
{
    public class LayoutPrincipal : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
