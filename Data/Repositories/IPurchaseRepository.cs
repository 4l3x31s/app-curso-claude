using app_curso_claude.Models;

namespace app_curso_claude.Data.Repositories
{
    /// <summary>
    /// Access to the purchases.
    /// </summary>
    public interface IPurchaseRepository
    {
        /// <summary>
        /// Returns the purchases of a product, newest first, each with its customer loaded.
        /// </summary>
        /// <param name="productId">Id of the product whose purchases are returned.</param>
        Task<IReadOnlyList<Purchase>> GetByProductAsync(int productId);

        /// <summary>
        /// Saves a new purchase and returns the id assigned to it.
        /// </summary>
        /// <param name="purchase">Purchase to save; its customer and product are given by their ids.</param>
        Task<int> AddAsync(Purchase purchase);

        /// <summary>
        /// Returns the most recent purchases of all products, newest first, each with its customer and product loaded.
        /// </summary>
        /// <param name="count">Maximum number of purchases to return; zero or less returns none.</param>
        Task<IReadOnlyList<Purchase>> GetLatestAsync(int count);
    }
}
