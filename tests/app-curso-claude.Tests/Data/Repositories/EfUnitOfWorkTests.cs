using System.Data;
using app_curso_claude.Data;
using app_curso_claude.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace app_curso_claude.Tests.Data.Repositories
{
    [Collection(AppCollection.Name)]
    public class EfUnitOfWorkTests(AppFactory factory)
    {
        [Fact]
        public async Task ExecuteInTransactionAsync_NoOpenTransaction_RunsTheOperationInsideARepeatableReadOne()
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // The operation writes nothing, so the transaction it commits is empty.
            var isolationLevel = await unitOfWork.ExecuteInTransactionAsync(
                () => Task.FromResult(context.Database.CurrentTransaction?.GetDbTransaction().IsolationLevel));

            Assert.Equal(IsolationLevel.RepeatableRead, isolationLevel);
            Assert.Null(context.Database.CurrentTransaction);
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_OperationThrows_PropagatesAndClosesTheTransaction()
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // The operation writes nothing before it fails.
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => unitOfWork.ExecuteInTransactionAsync<int>(
                    () => throw new InvalidOperationException("Operation failed.")));

            Assert.Null(context.Database.CurrentTransaction);
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_InsideAnOpenTransaction_JoinsItAndLeavesTheOutcomeToItsOwner()
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var products = scope.ServiceProvider.GetRequiredService<IProductRepository>();
            var product = await context.Products.AsNoTracking().OrderBy(p => p.Sku).FirstAsync();

            // Never committed: disposing it rolls the stock change back.
            await using (var transaction = await context.Database.BeginTransactionAsync())
            {
                await unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    await products.UpdateStockAsync(product.Sku, product.Stock + 1);
                    return true;
                });

                Assert.Same(transaction, context.Database.CurrentTransaction);
            }

            var storedStock = await context.Products
                .Where(p => p.Id == product.Id)
                .Select(p => p.Stock)
                .SingleAsync();
            Assert.Equal(product.Stock, storedStock);
        }
    }
}
