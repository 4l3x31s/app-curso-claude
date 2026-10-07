using app_curso_claude.Data.Repositories;
using app_curso_claude.Models;
using Microsoft.AspNetCore.Mvc;

namespace app_curso_claude.Controllers
{
    public class PurchasesController(IProductRepository products, IPurchaseRepository purchases) : Controller
    {
        public async Task<IActionResult> Index(string? sku)
        {
            var requestedSku = string.IsNullOrWhiteSpace(sku) ? null : sku.Trim();
            var selectedProduct = requestedSku is null ? null : await products.GetAsync(requestedSku);

            var model = new PurchasesIndexViewModel
            {
                // Deactivated products keep their purchase history, so they stay in the select.
                Products = await products.GetAllAsync(includeInactive: true),
                RequestedSku = requestedSku,
                SelectedProduct = selectedProduct,
                Purchases = selectedProduct is null ? [] : await purchases.GetByProductAsync(selectedProduct.Id)
            };

            return View(model);
        }
    }
}

namespace app_curso_claude.Models
{
    /// <summary>
    /// Data of the Purchases page: the products to choose from and the purchases of the chosen one.
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
    }
}
