using app_curso_claude.Filters;
using app_curso_claude.Models;
using app_curso_claude.Services;
using Microsoft.AspNetCore.Mvc;

namespace app_curso_claude.Controllers
{
    [RequireFeature(FeatureFlags.Contact)]
    public class ContactController(ContactRequestService contactRequests) : Controller
    {
        public const string FolioKey = "ContactRequestFolio";

        public IActionResult Index()
        {
            return View(BuildPage(new ContactRequestForm(), TempData[FolioKey] as string, []));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ContactRequestForm form)
        {
            var result = await contactRequests.SendAsync(form.Type, form.Sku, form.Email, form.Message);

            if (!result.Success)
            {
                return View(BuildPage(form, null, result.Errors));
            }

            // Redirecting after the post keeps a page refresh from sending the request twice.
            TempData[FolioKey] = result.Folio;
            return RedirectToAction(nameof(Index));
        }

        private static ContactPageViewModel BuildPage(ContactRequestForm form, string? folio, IReadOnlyList<string> errors)
        {
            return new ContactPageViewModel
            {
                // Hardcoded, clearly fictional sample data until a real data source exists.
                Details = new ContactDetailsViewModel
                {
                    Email = "support@example.com",
                    Phone = "+1 (555) 010-0199",
                    BusinessHours = "Monday to Friday, 9:00 AM - 5:00 PM"
                },
                Form = form,
                RequestTypes = ContactRequestService.RequestTypes,
                Folio = folio,
                Errors = errors
            };
        }
    }
}
