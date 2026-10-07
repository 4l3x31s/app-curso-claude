using app_curso_claude.Services;
using app_curso_claude.Tests.Fakes;

namespace app_curso_claude.Tests.Services
{
    public class PurchaseServiceTests
    {
        private const string Sku = "SKU-TEST";
        private const int CustomerId = 7;

        private static readonly DateTimeOffset Now = new(2026, 10, 6, 15, 30, 0, TimeSpan.Zero);

        private readonly FakeCustomerRepository _customers = new(Sample.Customer(CustomerId));
        private readonly FakePurchaseRepository _purchases = new();
        private readonly FakeUnitOfWork _unitOfWork = new();

        [Fact]
        public async Task RegisterAsync_ValidPurchase_ReducesTheStock()
        {
            var products = new FakeProductRepository(Sample.Product(Sku, stock: 10));

            var result = await CreateService(products).RegisterAsync(Sku, CustomerId, 3);

            Assert.True(result.Success);
            Assert.Empty(result.Errors);
            Assert.Equal(7, products.StockOf(Sku));
        }

        [Fact]
        public async Task RegisterAsync_ValidPurchase_SavesItWithTheProductPriceAndTheCurrentTime()
        {
            var products = new FakeProductRepository(Sample.Product(Sku, stock: 10, price: 19.99m, id: 42));

            await CreateService(products).RegisterAsync(Sku, CustomerId, 3);

            var purchase = Assert.Single(_purchases.Saved);
            Assert.Equal(42, purchase.ProductId);
            Assert.Equal(CustomerId, purchase.CustomerId);
            Assert.Equal(3, purchase.Quantity);
            Assert.Equal(19.99m, purchase.UnitPrice);
            Assert.Equal(Now.UtcDateTime, purchase.PurchasedAt);
        }

        [Fact]
        public async Task RegisterAsync_ValidPurchase_SavesPurchaseAndStockInOneUnitOfWork()
        {
            var products = new FakeProductRepository(Sample.Product(Sku, stock: 10));

            await CreateService(products).RegisterAsync(Sku, CustomerId, 3);

            Assert.Equal(1, _unitOfWork.Executions);
            Assert.Equal(1, products.StockUpdates);
            Assert.Single(_purchases.Saved);
        }

        [Fact]
        public async Task RegisterAsync_SkuWithSurroundingSpaces_RegistersThePurchase()
        {
            var products = new FakeProductRepository(Sample.Product(Sku, stock: 10));

            var result = await CreateService(products).RegisterAsync($"  {Sku} ", CustomerId, 1);

            Assert.True(result.Success);
            Assert.Equal(9, products.StockOf(Sku));
        }

        [Fact]
        public async Task RegisterAsync_QuantityAboveStock_FailsWithoutTouchingTheStock()
        {
            var products = new FakeProductRepository(Sample.Product(Sku, stock: 5));

            var result = await CreateService(products).RegisterAsync(Sku, CustomerId, 6);

            Assert.False(result.Success);
            Assert.Equal(["Not enough stock: 5 available, 6 requested."], result.Errors);
            Assert.Equal(5, products.StockOf(Sku));
            Assert.Equal(0, products.StockUpdates);
            Assert.Empty(_purchases.Saved);
        }

        [Fact]
        public async Task RegisterAsync_QuantityEqualToStock_LeavesTheStockAtZero()
        {
            var products = new FakeProductRepository(Sample.Product(Sku, stock: 5));

            var result = await CreateService(products).RegisterAsync(Sku, CustomerId, 5);

            Assert.True(result.Success);
            Assert.Equal(0, products.StockOf(Sku));
            Assert.Single(_purchases.Saved);
        }

        [Fact]
        public async Task RegisterAsync_InactiveProduct_FailsWithoutSavingAPurchase()
        {
            var products = new FakeProductRepository(Sample.Product(Sku, stock: 10, isActive: false));

            var result = await CreateService(products).RegisterAsync(Sku, CustomerId, 1);

            Assert.False(result.Success);
            Assert.Equal([$"The product {Sku} is inactive and cannot be purchased."], result.Errors);
            Assert.Equal(10, products.StockOf(Sku));
            Assert.Empty(_purchases.Saved);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(101)]
        public async Task RegisterAsync_QuantityOutOfRange_FailsWithoutTouchingTheStock(int quantity)
        {
            var products = new FakeProductRepository(Sample.Product(Sku, stock: 500));

            var result = await CreateService(products).RegisterAsync(Sku, CustomerId, quantity);

            Assert.False(result.Success);
            Assert.Equal(["The quantity must be between 1 and 100."], result.Errors);
            Assert.Equal(500, products.StockOf(Sku));
            Assert.Empty(_purchases.Saved);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        public async Task RegisterAsync_QuantityAtTheLimits_RegistersThePurchase(int quantity)
        {
            var products = new FakeProductRepository(Sample.Product(Sku, stock: 500));

            var result = await CreateService(products).RegisterAsync(Sku, CustomerId, quantity);

            Assert.True(result.Success);
            Assert.Equal(500 - quantity, products.StockOf(Sku));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task RegisterAsync_BlankSku_Fails(string sku)
        {
            var products = new FakeProductRepository(Sample.Product(Sku, stock: 10));

            var result = await CreateService(products).RegisterAsync(sku, CustomerId, 1);

            Assert.False(result.Success);
            Assert.Equal(["The SKU is required."], result.Errors);
            Assert.Empty(_purchases.Saved);
        }

        [Fact]
        public async Task RegisterAsync_BlankSkuAndQuantityOutOfRange_ReportsBothErrors()
        {
            var products = new FakeProductRepository(Sample.Product(Sku, stock: 10));

            var result = await CreateService(products).RegisterAsync("", CustomerId, 0);

            Assert.False(result.Success);
            Assert.Equal(["The SKU is required.", "The quantity must be between 1 and 100."], result.Errors);
        }

        [Fact]
        public async Task RegisterAsync_UnknownSku_Fails()
        {
            var products = new FakeProductRepository(Sample.Product(Sku, stock: 10));

            var result = await CreateService(products).RegisterAsync("SKU-MISSING", CustomerId, 1);

            Assert.False(result.Success);
            Assert.Equal(["No product found with SKU SKU-MISSING."], result.Errors);
            Assert.Empty(_purchases.Saved);
        }

        [Fact]
        public async Task RegisterAsync_UnknownCustomer_FailsWithoutTouchingTheStock()
        {
            var products = new FakeProductRepository(Sample.Product(Sku, stock: 10));

            var result = await CreateService(products).RegisterAsync(Sku, CustomerId + 1, 1);

            Assert.False(result.Success);
            Assert.Equal(["The customer does not exist."], result.Errors);
            Assert.Equal(10, products.StockOf(Sku));
            Assert.Empty(_purchases.Saved);
        }

        [Fact]
        public async Task RegisterAsync_SimultaneousRegistrations_DoNotOverwriteEachOthersStock()
        {
            // The latency keeps every read and write open long enough for the six callers to overlap.
            var products = new FakeProductRepository(Sample.Product(Sku, stock: 10))
            {
                Latency = TimeSpan.FromMilliseconds(20)
            };

            // Each request gets its own service instance, as the scoped registration does.
            // Six purchases of 2 units compete for 10 units: only five fit.
            var results = await Task.WhenAll(
                Enumerable.Range(0, 6).Select(_ => CreateService(products).RegisterAsync(Sku, CustomerId, 2)));

            Assert.Equal(5, results.Count(result => result.Success));
            Assert.Equal(0, products.StockOf(Sku));
            Assert.Equal(5, _purchases.Saved.Count);
        }

        private PurchaseService CreateService(FakeProductRepository products)
        {
            return new PurchaseService(products, _customers, _purchases, _unitOfWork, new FixedTimeProvider(Now));
        }
    }
}
