using app_curso_claude.Models;
using Microsoft.EntityFrameworkCore;

namespace app_curso_claude.Data.Repositories.EfCore
{
    /// <summary>
    /// Reads the products from SQL Server through <see cref="AppDbContext"/>.
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
    }
}
