using app_curso_claude.Data;
using app_curso_claude.Data.Repositories;
using app_curso_claude.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace app_curso_claude.Tests.Data.Repositories
{
    [Collection(AppCollection.Name)]
    public class EfPurchaseRepositoryTests(AppFactory factory)
    {
        [Fact]
        public async Task GetByProductAsync_ProductWithSeveralPurchases_ReturnsThemNewestFirst()
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var repository = scope.ServiceProvider.GetRequiredService<IPurchaseRepository>();

            // The product is found by querying, so the test does not depend on a seeded id.
            var productId = await context.Purchases
                .GroupBy(p => p.ProductId)
                .Where(group => group.Count() >= 2)
                .Select(group => group.Key)
                .FirstAsync();

            var purchases = await repository.GetByProductAsync(productId);

            var dates = purchases.Select(p => p.PurchasedAt).ToList();
            Assert.True(dates.Count >= 2);
            Assert.Equal(dates.OrderDescending(), dates);
            Assert.All(purchases, p => Assert.Equal(productId, p.ProductId));
        }

        [Fact]
        public async Task GetByProductAsync_ProductWithPurchases_LoadsEachCustomer()
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var repository = scope.ServiceProvider.GetRequiredService<IPurchaseRepository>();
            var productId = await context.Purchases.Select(p => p.ProductId).FirstAsync();

            var purchases = await repository.GetByProductAsync(productId);

            Assert.NotEmpty(purchases);
            Assert.All(purchases, p => Assert.Equal(p.CustomerId, p.Customer?.Id));
        }

        [Fact]
        public async Task GetByProductAsync_UnknownProduct_ReturnsEmptyList()
        {
            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IPurchaseRepository>();

            var purchases = await repository.GetByProductAsync(-1);

            Assert.Empty(purchases);
        }

        [Fact]
        public async Task AddAsync_NewPurchase_SavesItAndReturnsItsAssignedId()
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var repository = scope.ServiceProvider.GetRequiredService<IPurchaseRepository>();
            var customerId = await context.Customers.OrderBy(c => c.Id).Select(c => c.Id).FirstAsync();
            var product = await context.Products.AsNoTracking().OrderBy(p => p.Sku).FirstAsync();
            var purchasedAt = new DateTime(2026, 10, 6, 15, 30, 0, DateTimeKind.Utc);

            // Never committed: disposing the transaction rolls the new purchase back.
            await using var transaction = await context.Database.BeginTransactionAsync();

            var id = await repository.AddAsync(new Purchase
            {
                CustomerId = customerId,
                ProductId = product.Id,
                Quantity = 2,
                UnitPrice = product.Price,
                PurchasedAt = purchasedAt
            });

            Assert.True(id > 0);
            var saved = (await repository.GetByProductAsync(product.Id)).Single(p => p.Id == id);
            Assert.Equal(customerId, saved.CustomerId);
            Assert.Equal(2, saved.Quantity);
            Assert.Equal(product.Price, saved.UnitPrice);
            Assert.Equal(purchasedAt, saved.PurchasedAt);
        }

        [Fact]
        public async Task GetLatestAsync_SeveralPurchases_ReturnsTheNewestOfAllProductsInOrder()
        {
            const int count = 5;
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var repository = scope.ServiceProvider.GetRequiredService<IPurchaseRepository>();
            var expectedIds = await context.Purchases
                .OrderByDescending(p => p.PurchasedAt)
                .ThenByDescending(p => p.Id)
                .Select(p => p.Id)
                .Take(count)
                .ToListAsync();

            var purchases = await repository.GetLatestAsync(count);

            Assert.NotEmpty(purchases);
            Assert.Equal(expectedIds, purchases.Select(p => p.Id));
        }

        [Fact]
        public async Task GetLatestAsync_SeveralPurchases_LoadsEachCustomerAndProduct()
        {
            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IPurchaseRepository>();

            var purchases = await repository.GetLatestAsync(5);

            Assert.NotEmpty(purchases);
            Assert.All(purchases, p => Assert.Equal(p.CustomerId, p.Customer?.Id));
            Assert.All(purchases, p => Assert.Equal(p.ProductId, p.Product?.Id));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetLatestAsync_CountNotPositive_ReturnsEmptyList(int count)
        {
            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IPurchaseRepository>();

            var purchases = await repository.GetLatestAsync(count);

            Assert.Empty(purchases);
        }
    }
}
