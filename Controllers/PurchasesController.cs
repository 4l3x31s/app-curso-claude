using app_curso_claude.Data.Repositories;
using app_curso_claude.Models;
using app_curso_claude.Services;
using Microsoft.AspNetCore.Mvc;

namespace app_curso_claude.Controllers
{
    public class PurchasesController(
        IProductRepository products,
        ICustomerRepository customers,
        IPurchaseRepository purchases,
        PurchaseService purchaseService) : Controller
    {
        /// <summary>TempData key of the message shown after a purchase is registered.</summary>
        public const string RegisteredMessageKey = "PurchaseRegistered";

        public async Task<IActionResult> Index(string? sku)
        {
            return View(await BuildModelAsync(sku));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string? sku, int customerId, int quantity)
        {
            var result = await purchaseService.RegisterAsync(sku ?? string.Empty, customerId, quantity);

            if (!result.Success)
            {
                // The page is shown again with what was entered, so it can be corrected.
                var model = await BuildModelAsync(sku, result.Errors, customerId, quantity);

                return View(nameof(Index), model);
            }

            // Redirecting keeps a page refresh from registering the purchase twice.
            TempData[RegisteredMessageKey] = "Purchase registered.";

            return RedirectToAction(nameof(Index), new { sku = sku?.Trim() });
        }

        private async Task<PurchasesIndexViewModel> BuildModelAsync(
            string? sku,
            IReadOnlyList<string>? errors = null,
            int? customerId = null,
            int? quantity = null)
        {
            var requestedSku = string.IsNullOrWhiteSpace(sku) ? null : sku.Trim();
            var selectedProduct = requestedSku is null ? null : await products.GetAsync(requestedSku);

            return new PurchasesIndexViewModel
            {
                // Deactivated products keep their purchase history, so they stay in the select.
                Products = await products.GetAllAsync(includeInactive: true),
                RequestedSku = requestedSku,
                SelectedProduct = selectedProduct,
                Purchases = selectedProduct is null ? [] : await purchases.GetByProductAsync(selectedProduct.Id),
                // The customers are only needed by the form, which is shown for a selected product.
                Customers = selectedProduct is null ? [] : await customers.GetAllAsync(),
                Errors = errors ?? [],
                CustomerId = customerId,
                Quantity = quantity
            };
        }
    }
}

namespace app_curso_claude.Models
{
    /// <summary>
    /// Data of the Purchases page: the products to choose from, the purchases of the chosen one
    /// and the form that registers a new purchase of it.
    /// </summary>
    public class PurchasesIndexViewModel
    {
        /// <summary>Every product, for the select.</summary>
        public required IReadOnlyList<Product> Products { get; init; }

        /// <summary>SKU asked for in the query string; <c>null</c> when none was sent.</summary>
        public string? RequestedSku { get; init; }

        /// <summary>Product with the requested SKU; <c>null</c> when no SKU was sent or it is unknown.</summary>
        public Product? SelectedProduct { get; init; }

        /// <summary>Purchases of the selected product, newest first.</summary>
        public required IReadOnlyList<Purchase> Purchases { get; init; }

        /// <summary>Customers to choose from in the form; empty when no product is selected.</summary>
        public IReadOnlyList<Customer> Customers { get; init; } = [];

        /// <summary>Reasons why the last purchase was not registered; empty when there was no failure.</summary>
        public IReadOnlyList<string> Errors { get; init; } = [];

        /// <summary>Customer chosen in the rejected form, to show it again.</summary>
        public int? CustomerId { get; init; }

        /// <summary>Quantity entered in the rejected form, to show it again.</summary>
        public int? Quantity { get; init; }
    }
}
