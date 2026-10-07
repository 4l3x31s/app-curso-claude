using app_curso_claude.Data.Repositories;
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
    }
}
