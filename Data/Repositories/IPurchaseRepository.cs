using app_curso_claude.Models;

namespace app_curso_claude.Data.Repositories
{
    /// <summary>
    /// Read access to the purchases.
    /// </summary>
    public interface IPurchaseRepository
    {
        /// <summary>
        /// Returns the purchases of a product, newest first, each with its customer loaded.
        /// </summary>
        /// <param name="productId">Id of the product whose purchases are returned.</param>
        Task<IReadOnlyList<Purchase>> GetByProductAsync(int productId);
    }
}
