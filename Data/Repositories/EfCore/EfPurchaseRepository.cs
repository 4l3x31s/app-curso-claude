using app_curso_claude.Models;
using Microsoft.EntityFrameworkCore;

namespace app_curso_claude.Data.Repositories.EfCore
{
    /// <summary>
    /// Reads and saves the purchases in SQL Server through <see cref="AppDbContext"/>.
    /// </summary>
    public class EfPurchaseRepository(AppDbContext context) : IPurchaseRepository
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<Purchase>> GetByProductAsync(int productId)
        {
            return await context.Purchases
                .AsNoTracking()
                .Include(p => p.Customer)
                .Where(p => p.ProductId == productId)
                .OrderByDescending(p => p.PurchasedAt)
                // The id breaks ties so purchases made at the same instant keep a stable order.
                .ThenByDescending(p => p.Id)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<int> AddAsync(Purchase purchase)
        {
            context.Purchases.Add(purchase);
            await context.SaveChangesAsync();

            return purchase.Id;
        }
    }
}
