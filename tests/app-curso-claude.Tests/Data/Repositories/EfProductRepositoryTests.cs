using app_curso_claude.Data;
using app_curso_claude.Data.Repositories;
using app_curso_claude.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace app_curso_claude.Tests.Data.Repositories
{
    [Collection(AppCollection.Name)]
    public class EfProductRepositoryTests(AppFactory factory)
    {
        [Fact]
        public async Task GetAllAsync_SeededProducts_ReturnsThemOrderedBySku()
        {
            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

            var products = await repository.GetAllAsync(includeInactive: true);

            var skus = products.Select(p => p.Sku).ToList();
            Assert.NotEmpty(skus);
            Assert.Equal(skus.OrderBy(sku => sku, StringComparer.Ordinal), skus);
        }

        [Fact]
        public async Task GetAllAsync_ExcludingInactive_ReturnsOnlyActiveProducts()
        {
            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

            var products = await repository.GetAllAsync(includeInactive: false);

            Assert.NotEmpty(products);
            Assert.All(products, product => Assert.True(product.IsActive));
        }

        [Fact]
        public async Task AddAsync_NewProduct_StoresItUntilTheTransactionIsRolledBack()
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
            var sku = NewTestSku();

            // Never committed: disposing the transaction rolls the insert back.
            await using (await context.Database.BeginTransactionAsync())
            {
                await repository.AddAsync(NewProduct(sku, isActive: true));

                var stored = await repository.GetAsync(sku);
                Assert.NotNull(stored);
                Assert.True(stored.IsActive);
            }

            Assert.Null(await repository.GetAsync(sku));
        }

        [Fact]
        public async Task DeactivateAsync_ActiveProduct_KeepsTheRowAndMarksItInactive()
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
            var sku = NewTestSku();

            // Never committed: disposing the transaction rolls every write back.
            await using (await context.Database.BeginTransactionAsync())
            {
                await repository.AddAsync(NewProduct(sku, isActive: true));

                var deactivated = await repository.DeactivateAsync(sku);

                Assert.True(deactivated);
                var stored = await repository.GetAsync(sku);
                Assert.NotNull(stored);
                Assert.False(stored.IsActive);
                Assert.DoesNotContain(await repository.GetAllAsync(includeInactive: false), p => p.Sku == sku);
                Assert.Contains(await repository.GetAllAsync(includeInactive: true), p => p.Sku == sku);
            }
        }

        [Fact]
        public async Task DeactivateAsync_InactiveOrUnknownProduct_ReturnsFalse()
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
            var sku = NewTestSku();

            // Never committed: disposing the transaction rolls the insert back.
            await using (await context.Database.BeginTransactionAsync())
            {
                await repository.AddAsync(NewProduct(sku, isActive: false));

                Assert.False(await repository.DeactivateAsync(sku));
                Assert.False(await repository.DeactivateAsync("SKU-DOES-NOT-EXIST"));
            }
        }

        // A random SKU outside the SKU-00000 format, so it cannot collide with a real product.
        private static string NewTestSku() => $"TEST-{Guid.NewGuid():N}"[..32];

        private static Product NewProduct(string sku, bool isActive) => new()
        {
            Sku = sku,
            Name = "Repository test product",
            Description = string.Empty,
            Category = "Tests",
            Price = 1.00m,
            Stock = 0,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = isActive
        };

        [Fact]
        public async Task GetAsync_ExistingSku_ReturnsThatProduct()
        {
            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

            var product = await repository.GetAsync("SKU-00001");

            Assert.NotNull(product);
            Assert.Equal("SKU-00001", product.Sku);
        }

        [Fact]
        public async Task GetAsync_UnknownSku_ReturnsNull()
        {
            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

            var product = await repository.GetAsync("SKU-DOES-NOT-EXIST");

            Assert.Null(product);
        }

        [Fact]
        public async Task UpdateStockAsync_ExistingSku_StoresTheNewStock()
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
            var product = await repository.GetAsync("SKU-00001");
            Assert.NotNull(product);

            // Never committed: disposing the transaction rolls the stock change back.
            await using var transaction = await context.Database.BeginTransactionAsync();

            await repository.UpdateStockAsync(product.Sku, product.Stock + 1);

            var updated = await repository.GetAsync(product.Sku);
            Assert.Equal(product.Stock + 1, updated?.Stock);
        }

        [Fact]
        public async Task UpdateStockAsync_UnknownSku_Throws()
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

            // Never committed, although an unknown SKU matches no row to change.
            await using var transaction = await context.Database.BeginTransactionAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.UpdateStockAsync("SKU-DOES-NOT-EXIST", 1));
        }
    }
}
