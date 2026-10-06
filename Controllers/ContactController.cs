using app_curso_claude.Models;
using Microsoft.AspNetCore.Mvc;

namespace app_curso_claude.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            // Hardcoded, clearly fictional sample data until a real data source exists.
            var model = new ContactDetailsViewModel
            {
                Email = "support@example.com",
                Phone = "+1 (555) 010-0199",
                BusinessHours = "Monday to Friday, 9:00 AM - 5:00 PM"
            };

            return View(model);
        }
    }
}
