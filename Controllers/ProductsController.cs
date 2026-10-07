using System.Globalization;
using app_curso_claude.Data.Repositories;
using app_curso_claude.Models;
using app_curso_claude.Services;
using Microsoft.AspNetCore.Mvc;

namespace app_curso_claude.Controllers
{
    public class ProductsController(IProductRepository products, ProductService productService) : Controller
    {
        private const string SuccessMessageKey = "ProductsSuccessMessage";

        public async Task<IActionResult> Index(bool showInactive = false)
        {
            return View(await BuildModelAsync(showInactive, new ProductCreateForm(), []));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateForm form, bool showInactive = false)
        {
            var errors = new List<string>();

            // The browser sends numbers with a dot, so they are read the same way under any server culture.
            if (!decimal.TryParse(form.Price, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var price))
            {
                errors.Add("Price must be a number, for example 19.99.");
            }

            if (!int.TryParse(form.InitialStock, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var initialStock))
            {
                errors.Add("Initial stock must be a whole number.");
            }

            if (errors.Count == 0)
            {
                var result = await productService.CreateAsync(
                    form.Sku ?? string.Empty, form.Name ?? string.Empty, form.Category ?? string.Empty, price, initialStock);
                if (result.Success)
                {
                    TempData[SuccessMessageKey] = "Product created.";
                    return RedirectToAction(nameof(Index), new { showInactive });
                }

                errors.AddRange(result.Errors);
            }

            return View(nameof(Index), await BuildModelAsync(showInactive, form, errors));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(string? sku, bool showInactive = false)
        {
            var result = await productService.DeactivateAsync(sku ?? string.Empty);
            if (result.Success)
            {
                TempData[SuccessMessageKey] = "Product deactivated.";
                return RedirectToAction(nameof(Index), new { showInactive });
            }

            return View(nameof(Index), await BuildModelAsync(showInactive, new ProductCreateForm(), result.Errors));
        }

        private async Task<ProductsIndexViewModel> BuildModelAsync(
            bool showInactive, ProductCreateForm form, IReadOnlyList<string> errors)
        {
            return new ProductsIndexViewModel
            {
                Products = await products.GetAllAsync(includeInactive: showInactive),
                ShowInactive = showInactive,
                Form = form,
                Errors = errors,
                SuccessMessage = TempData[SuccessMessageKey] as string
            };
        }
    }
}

namespace app_curso_claude.Models
{
    /// <summary>
    /// Data of the Products page: the listed products, the creation form and the outcome of the last operation.
    /// </summary>
    public class ProductsIndexViewModel
    {
        /// <summary>Products to list, ordered by SKU.</summary>
        public required IReadOnlyList<Product> Products { get; init; }

        /// <summary>True when the deactivated products are listed too.</summary>
        public bool ShowInactive { get; init; }

        /// <summary>Values of the creation form, kept when the creation fails.</summary>
        public required ProductCreateForm Form { get; init; }

        /// <summary>Errors of the last operation; empty when there are none.</summary>
        public required IReadOnlyList<string> Errors { get; init; }

        /// <summary>Message of the last successful operation; <c>null</c> when there is none.</summary>
        public string? SuccessMessage { get; init; }
    }

    /// <summary>
    /// Values typed in the form that creates a product, as text until the controller parses them.
    /// </summary>
    public class ProductCreateForm
    {
        public string? Sku { get; set; }

        public string? Name { get; set; }

        public string? Category { get; set; }

        public string? Price { get; set; }

        public string? InitialStock { get; set; }
    }
}
