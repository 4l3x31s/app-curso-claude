using app_curso_claude.Models;
using Microsoft.EntityFrameworkCore;

namespace app_curso_claude.Data.Repositories.EfCore
{
    /// <summary>
    /// Reads the customers from SQL Server through <see cref="AppDbContext"/>.
    /// </summary>
    public class EfCustomerRepository(AppDbContext context) : ICustomerRepository
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<Customer>> GetAllAsync()
        {
            return await context.Customers
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Customer?> GetAsync(int id)
        {
            return await context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}
