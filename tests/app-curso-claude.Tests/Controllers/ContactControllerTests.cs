using app_curso_claude.Controllers;
using app_curso_claude.Models;
using app_curso_claude.Services;
using app_curso_claude.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;

namespace app_curso_claude.Tests.Controllers
{
    public class ContactControllerTests
    {
        private readonly FakeContactRequestRepository _requests = new();
        private readonly ContactController _controller;

        public ContactControllerTests()
        {
            var service = new ContactRequestService(
                _requests,
                new FakeProductRepository("SKU-00001"),
                new FixedTimeProvider(new DateTimeOffset(2026, 3, 15, 10, 30, 0, TimeSpan.Zero)),
                NullLogger<ContactRequestService>.Instance);

            _controller = new ContactController(service)
            {
                TempData = new TempDataDictionary(new DefaultHttpContext(), new NoTempDataProvider())
            };
        }

        [Fact]
        public void Index_Get_ShowsTheContactDetailsAndAnEmptyForm()
        {
            var model = ModelOf(_controller.Index());

            Assert.Equal("support@example.com", model.Details.Email);
            Assert.Equal(["Consulta", "Reclamo", "Solicitud"], model.RequestTypes);
            Assert.Null(model.Folio);
            Assert.Empty(model.Errors);
            Assert.Null(model.Form.Message);
        }

        [Fact]
        public async Task Index_PostValidForm_RedirectsAndTheNextGetShowsTheFolio()
        {
            var form = new ContactRequestForm
            {
                Type = "Consulta",
                Sku = "SKU-00001",
                Email = "ana@example.com",
                Message = "I would like to know more."
            };

            var redirect = Assert.IsType<RedirectToActionResult>(await _controller.Index(form));
            var model = ModelOf(_controller.Index());

            Assert.Equal(nameof(ContactController.Index), redirect.ActionName);
            Assert.Equal("SOL-2026-0001", model.Folio);
            Assert.Single(_requests.Requests);
        }

        [Fact]
        public async Task Index_PostInvalidForm_ShowsTheServiceErrorsAndKeepsTheTypedValues()
        {
            var form = new ContactRequestForm
            {
                Type = "Consulta",
                Email = "ana@example.com",
                Message = "short"
            };

            var model = ModelOf(await _controller.Index(form));

            Assert.Equal(["Message must be between 10 and 500 characters."], model.Errors);
            Assert.Null(model.Folio);
            Assert.Same(form, model.Form);
            Assert.Equal("support@example.com", model.Details.Email);
            Assert.Empty(_requests.Requests);
        }

        private static ContactPageViewModel ModelOf(IActionResult result)
        {
            return Assert.IsType<ContactPageViewModel>(Assert.IsType<ViewResult>(result).Model);
        }

        // The dictionary keeps its values in memory during a test; nothing needs to be persisted.
        private class NoTempDataProvider : ITempDataProvider
        {
            public IDictionary<string, object> LoadTempData(HttpContext context)
            {
                return new Dictionary<string, object>();
            }

            public void SaveTempData(HttpContext context, IDictionary<string, object> values)
            {
            }
        }
    }
}
