using app_curso_claude.Models;
using Microsoft.EntityFrameworkCore;

namespace app_curso_claude.Data.Repositories.EfCore
{
    /// <summary>
    /// Reads, writes and updates the products in SQL Server through <see cref="AppDbContext"/>.
    /// </summary>
    public class EfProductRepository(AppDbContext context) : IProductRepository
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<Product>> GetAllAsync(bool includeInactive)
        {
            return await context.Products
                .AsNoTracking()
                .Where(p => includeInactive || p.IsActive)
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
        public async Task AddAsync(Product product)
        {
            context.Products.Add(product);
            await context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<bool> DeactivateAsync(string sku)
        {
            // Logical deletion: the row stays and only its flag changes.
            var product = await context.Products
                .FirstOrDefaultAsync(p => p.Sku == sku && p.IsActive);
            if (product is null)
            {
                return false;
            }

            product.IsActive = false;
            await context.SaveChangesAsync();
            return true;
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
