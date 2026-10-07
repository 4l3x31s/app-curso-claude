using app_curso_claude.Data.Repositories;
using app_curso_claude.Models;

namespace app_curso_claude.Services
{
    /// <summary>
    /// Figures shown on the Home page.
    /// </summary>
    /// <param name="ActiveProducts">Number of active products.</param>
    /// <param name="UnitsInStock">Units in stock across the active products.</param>
    /// <param name="InventoryValue">Sum of price times stock across the active products.</param>
    /// <param name="LatestPurchases">Most recent purchases of all products, newest first.</param>
    public record Summary(
        int ActiveProducts,
        int UnitsInStock,
        decimal InventoryValue,
        IReadOnlyList<Purchase> LatestPurchases);

    /// <summary>
    /// Builds the summary of the Home page from the products and the purchases.
    /// </summary>
    public class SummaryService(IProductRepository products, IPurchaseRepository purchases)
    {
        /// <summary>
        /// Number of purchases listed in the summary.
        /// </summary>
        public const int LatestPurchasesCount = 5;

        /// <summary>
        /// Returns the current summary. Inactive products do not count towards any figure.
        /// </summary>
        public async Task<Summary> GetAsync()
        {
            var activeProducts = await products.GetAllAsync(includeInactive: false);

            return new Summary(
                activeProducts.Count,
                activeProducts.Sum(p => p.Stock),
                activeProducts.Sum(p => p.Price * p.Stock),
                await purchases.GetLatestAsync(LatestPurchasesCount));
        }
    }
}
