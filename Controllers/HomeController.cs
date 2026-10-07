using app_curso_claude.Models;
using app_curso_claude.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace app_curso_claude.Controllers
{
    public class HomeController(SummaryService summary) : Controller
    {
        public async Task<IActionResult> Index()
        {
            return View(await summary.GetAsync());
        }
        //TODO: se debe modificar este codigo 
        public IActionResult Privacy()
        {
            return View();
        }
        //TODO: prueba de agente
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
