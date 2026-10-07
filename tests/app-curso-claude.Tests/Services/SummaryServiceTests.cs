using app_curso_claude.Data.Repositories;
using app_curso_claude.Models;
using app_curso_claude.Services;

namespace app_curso_claude.Tests.Services
{
    public class SummaryServiceTests
    {
        private static readonly DateTime OlderPurchaseDate = new(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime NewerPurchaseDate = new(2026, 2, 20, 15, 30, 0, DateTimeKind.Utc);

        [Fact]
        public async Task GetAsync_ProductsWithAnInactiveOne_CountsOnlyTheActiveProducts()
        {
            var service = CreateService();

            var summary = await service.GetAsync();

            Assert.Equal(2, summary.ActiveProducts);
        }

        [Fact]
        public async Task GetAsync_ProductsWithAnInactiveOne_SumsTheStockOfTheActiveProducts()
        {
            var service = CreateService();

            var summary = await service.GetAsync();

            Assert.Equal(15, summary.UnitsInStock);
        }

        [Fact]
        public async Task GetAsync_ProductsWithAnInactiveOne_ValuesTheInventoryAsPriceTimesStock()
        {
            var service = CreateService();

            var summary = await service.GetAsync();

            Assert.Equal(9500.00m, summary.InventoryValue);
        }

        [Fact]
        public async Task GetAsync_InactiveProductWithStock_LeavesItOutOfTheTotals()
        {
            var products = SampleProducts();
            products.Single(p => !p.IsActive).Stock = 40;
            var service = new SummaryService(
                new FakeProductRepository(products),
                new FakePurchaseRepository(SamplePurchases(products)));

            var summary = await service.GetAsync();

            Assert.Equal(15, summary.UnitsInStock);
            Assert.Equal(9500.00m, summary.InventoryValue);
        }

        [Fact]
        public async Task GetAsync_PurchasesOnDifferentDates_ReturnsThemNewestFirst()
        {
            var service = CreateService();

            var summary = await service.GetAsync();

            Assert.Equal(
                [NewerPurchaseDate, OlderPurchaseDate],
                summary.LatestPurchases.Select(p => p.PurchasedAt));
            Assert.Equal(
                ["SKU-00002", "SKU-00001"],
                summary.LatestPurchases.Select(p => p.Product.Sku));
        }

        [Fact]
        public async Task GetAsync_Always_AsksForTheFiveLatestPurchases()
        {
            var products = SampleProducts();
            var purchases = new FakePurchaseRepository(SamplePurchases(products));
            var service = new SummaryService(new FakeProductRepository(products), purchases);

            await service.GetAsync();

            Assert.Equal(5, purchases.LastRequestedCount);
        }

        [Fact]
        public async Task GetAsync_NoProductsAndNoPurchases_ReturnsAnEmptySummary()
        {
            var service = new SummaryService(new FakeProductRepository([]), new FakePurchaseRepository([]));

            var summary = await service.GetAsync();

            Assert.Equal(0, summary.ActiveProducts);
            Assert.Equal(0, summary.UnitsInStock);
            Assert.Equal(0m, summary.InventoryValue);
            Assert.Empty(summary.LatestPurchases);
        }

        private static SummaryService CreateService()
        {
            var products = SampleProducts();

            return new SummaryService(
                new FakeProductRepository(products),
                new FakePurchaseRepository(SamplePurchases(products)));
        }

        private static List<Product> SampleProducts()
        {
            return
            [
                new Product
                {
                    Id = 1,
                    Sku = "SKU-00001",
                    Name = "Teclado mecánico",
                    Description = "Teclado mecánico",
                    Category = "Periféricos",
                    Price = 350.00m,
                    Stock = 10,
                    IsActive = true
                },
                new Product
                {
                    Id = 2,
                    Sku = "SKU-00002",
                    Name = "Monitor 24 pulgadas",
                    Description = "Monitor 24 pulgadas",
                    Category = "Pantallas",
                    Price = 1200.00m,
                    Stock = 5,
                    IsActive = true
                },
                new Product
                {
                    Id = 3,
                    Sku = "SKU-00003",
                    Name = "Mouse óptico",
                    Description = "Mouse óptico",
                    Category = "Periféricos",
                    Price = 80.00m,
                    Stock = 0,
                    IsActive = false
                }
            ];
        }

        private static List<Purchase> SamplePurchases(List<Product> products)
        {
            var customer = new Customer
            {
                Id = 1,
                FirstName = "Ana",
                LastName = "Rojas",
                Email = "ana.rojas@example.com",
                Phone = "000-0000",
                Address = "Sample street 1",
                City = "La Paz",
                Country = "Bolivia"
            };

            // Listed oldest first on purpose, so the order of the summary is not the insertion order.
            return
            [
                NewPurchase(1, customer, products[0], OlderPurchaseDate),
                NewPurchase(2, customer, products[1], NewerPurchaseDate)
            ];
        }

        private static Purchase NewPurchase(int id, Customer customer, Product product, DateTime purchasedAt)
        {
            return new Purchase
            {
                Id = id,
                CustomerId = customer.Id,
                Customer = customer,
                ProductId = product.Id,
                Product = product,
                Quantity = 1,
                UnitPrice = product.Price,
                PurchasedAt = purchasedAt
            };
        }

        private sealed class FakeProductRepository(IReadOnlyList<Product> products) : IProductRepository
        {
            public Task<IReadOnlyList<Product>> GetAllAsync(bool includeInactive)
            {
                return Task.FromResult<IReadOnlyList<Product>>(
                    products.Where(p => includeInactive || p.IsActive).OrderBy(p => p.Sku).ToList());
            }

            public Task<Product?> GetAsync(string sku)
            {
                return Task.FromResult(products.FirstOrDefault(p => p.Sku == sku));
            }

            // The summary only reads; the write members exist to satisfy the contract.
            public Task AddAsync(Product product) => throw new NotSupportedException();

            public Task<bool> DeactivateAsync(string sku) => throw new NotSupportedException();

            public Task UpdateStockAsync(string sku, int newStock) => throw new NotSupportedException();
        }

        private sealed class FakePurchaseRepository(IReadOnlyList<Purchase> purchases) : IPurchaseRepository
        {
            public int? LastRequestedCount { get; private set; }

            public Task<IReadOnlyList<Purchase>> GetByProductAsync(int productId)
            {
                return Task.FromResult<IReadOnlyList<Purchase>>(
                    Newest(purchases.Where(p => p.ProductId == productId)).ToList());
            }

            public Task<IReadOnlyList<Purchase>> GetLatestAsync(int count)
            {
                LastRequestedCount = count;

                return Task.FromResult<IReadOnlyList<Purchase>>(Newest(purchases).Take(count).ToList());
            }

            // The summary only reads; AddAsync exists to satisfy the contract.
            public Task<int> AddAsync(Purchase purchase) => throw new NotSupportedException();

            private static IEnumerable<Purchase> Newest(IEnumerable<Purchase> source)
            {
                return source.OrderByDescending(p => p.PurchasedAt).ThenByDescending(p => p.Id);
            }
        }
    }
}
