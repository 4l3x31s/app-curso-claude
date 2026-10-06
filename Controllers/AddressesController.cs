using app_curso_claude.Models;
using Microsoft.AspNetCore.Mvc;

namespace app_curso_claude.Controllers
{
    public class AddressesController : Controller
    {
        public IActionResult Index()
        {
            // Hardcoded, fictional sample data; there is no data store yet.
            IReadOnlyList<AddressViewModel> addresses =
            [
                new AddressViewModel
                {
                    Label = "Head office",
                    Street = "100 Example Avenue",
                    City = "Sampleton",
                    Country = "Freedonia"
                },
                new AddressViewModel
                {
                    Label = "Warehouse",
                    Street = "42 Placeholder Road",
                    City = "Testville",
                    Country = "Ruritania"
                },
                new AddressViewModel
                {
                    Label = "Support center",
                    Street = "7 Demo Street",
                    City = "Mockingham",
                    Country = "Genovia"
                }
            ];

            return View(addresses);
        }
    }
}
