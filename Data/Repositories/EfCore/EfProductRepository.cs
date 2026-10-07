using app_curso_claude.Models;
using Microsoft.EntityFrameworkCore;

namespace app_curso_claude.Data.Repositories.EfCore
{
    /// <summary>
    /// Reads and updates the products in SQL Server through <see cref="AppDbContext"/>.
    /// </summary>
    public class EfProductRepository(AppDbContext context) : IProductRepository
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<Product>> GetAllAsync()
        {
            return await context.Products
                .AsNoTracking()
                .OrderBy(p => p.Sku)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Product?> GetAsync(string sku)
        {
            return await context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Sku == sku);
        }

        /// <inheritdoc />
        public async Task UpdateStockAsync(string sku, int newStock)
        {
            var updatedRows = await context.Products
                .Where(p => p.Sku == sku)
                .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Stock, newStock));

            if (updatedRows == 0)
            {
                throw new InvalidOperationException($"No product found with SKU {sku}.");
            }
        }
    }
}
