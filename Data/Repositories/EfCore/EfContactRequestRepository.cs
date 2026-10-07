using app_curso_claude.Models;
using Microsoft.EntityFrameworkCore;

namespace app_curso_claude.Data.Repositories.EfCore
{
    /// <summary>
    /// Stores the contact requests in SQL Server through <see cref="AppDbContext"/>.
    /// </summary>
    public class EfContactRequestRepository(AppDbContext context) : IContactRequestRepository
    {
        /// <inheritdoc />
        public async Task<int> CountByYearAsync(int year)
        {
            var start = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddYears(1);

            return await context.ContactRequests
                .CountAsync(r => r.CreatedAt >= start && r.CreatedAt < end);
        }

        /// <inheritdoc />
        public async Task AddAsync(ContactRequest request)
        {
            context.ContactRequests.Add(request);
            await context.SaveChangesAsync();
        }
    }
}
