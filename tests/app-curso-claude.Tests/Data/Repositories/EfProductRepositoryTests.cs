using app_curso_claude.Data;
using app_curso_claude.Data.Repositories;
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

            var products = await repository.GetAllAsync();

            var skus = products.Select(p => p.Sku).ToList();
            Assert.NotEmpty(skus);
            Assert.Equal(skus.OrderBy(sku => sku, StringComparer.Ordinal), skus);
        }

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
