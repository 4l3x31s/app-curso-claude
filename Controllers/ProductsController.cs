using app_curso_claude.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace app_curso_claude.Controllers
{
    public class ProductsController(IProductRepository products) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var model = await products.GetAllAsync();

            return View(model);
        }
    }
}
