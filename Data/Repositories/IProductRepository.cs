using app_curso_claude.Models;

namespace app_curso_claude.Data.Repositories
{
    /// <summary>
    /// Access to the products.
    /// </summary>
    public interface IProductRepository
    {
        /// <summary>
        /// Returns every product, active or not, ordered by SKU.
        /// </summary>
        Task<IReadOnlyList<Product>> GetAllAsync();

        /// <summary>
        /// Returns the product with the given SKU, or <c>null</c> when there is none.
        /// </summary>
        /// <param name="sku">SKU to look for.</param>
        Task<Product?> GetAsync(string sku);

        /// <summary>
        /// Replaces the stock of the product with the given SKU.
        /// </summary>
        /// <param name="sku">SKU of the product to update.</param>
        /// <param name="newStock">Stock the product is left with.</param>
        /// <exception cref="InvalidOperationException">No product has that SKU.</exception>
        Task UpdateStockAsync(string sku, int newStock);
    }
}
