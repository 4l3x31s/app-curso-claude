using System.Data;
using Microsoft.EntityFrameworkCore;

namespace app_curso_claude.Data.Repositories.EfCore
{
    /// <summary>
    /// Runs the work in a SQL Server transaction of the request's <see cref="AppDbContext"/>,
    /// the same context the repositories use.
    /// </summary>
    public class EfUnitOfWork(AppDbContext context) : IUnitOfWork
    {
        /// <inheritdoc />
        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
        {
            // Whoever already opened a transaction owns it: the work joins it and the owner decides the outcome.
            if (context.Database.CurrentTransaction is not null)
            {
                return await operation();
            }

            // Repeatable read keeps the rows read by the operation locked until the end, so a
            // value read here (a stock) cannot be changed by another transaction before it is written back.
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);

            // An exception skips the commit, and disposing the transaction then rolls it back.
            var result = await operation();
            await transaction.CommitAsync();

            return result;
        }
    }
}
