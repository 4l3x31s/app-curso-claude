using app_curso_claude.Models;

namespace app_curso_claude.Data.Repositories
{
    /// <summary>
    /// Read access to the products.
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
    }
}
