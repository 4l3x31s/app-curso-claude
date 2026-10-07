using app_curso_claude.Models;

namespace app_curso_claude.Data.Repositories
{
    /// <summary>
    /// Access to the products.
    /// </summary>
    public interface IProductRepository
    {
        /// <summary>
        /// Returns the products ordered by SKU.
        /// </summary>
        /// <param name="includeInactive">
        /// True to also return the deactivated products; false to return only the active ones.
        /// </param>
        Task<IReadOnlyList<Product>> GetAllAsync(bool includeInactive);

        /// <summary>
        /// Returns the product with the given SKU, active or not, or <c>null</c> when there is none.
        /// </summary>
        /// <param name="sku">SKU to look for.</param>
        Task<Product?> GetAsync(string sku);

        /// <summary>
        /// Stores a new product.
        /// </summary>
        /// <param name="product">Product to store.</param>
        Task AddAsync(Product product);

        /// <summary>
        /// Marks the active product with the given SKU as inactive. The row is kept.
        /// </summary>
        /// <param name="sku">SKU of the product to deactivate.</param>
        /// <returns>True when a product was deactivated; false when no active product has that SKU.</returns>
        Task<bool> DeactivateAsync(string sku);

        /// <summary>
        /// Replaces the stock of the product with the given SKU.
        /// </summary>
        /// <param name="sku">SKU of the product to update.</param>
        /// <param name="newStock">Stock the product is left with.</param>
        /// <exception cref="InvalidOperationException">No product has that SKU.</exception>
        Task UpdateStockAsync(string sku, int newStock);
    }
}
