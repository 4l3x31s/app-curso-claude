using app_curso_claude.Services;
using app_curso_claude.Tests.Fakes;

namespace app_curso_claude.Tests.Services
{
    public class ProductServiceTests
    {
        private static readonly DateTimeOffset Now = new(2026, 10, 6, 15, 30, 0, TimeSpan.Zero);

        private readonly FakeProductRepository repository = new();
        private readonly ProductService service;

        public ProductServiceTests()
        {
            service = new ProductService(repository, new FixedTimeProvider(Now));
        }

        [Fact]
        public async Task CreateAsync_ValidData_StoresAnActiveProductWithEmptyDescription()
        {
            var result = await service.CreateAsync("SKU-12345", "Wireless mouse", "Peripherals", 19.99m, 7);

            Assert.True(result.Success);
            Assert.Empty(result.Errors);
            var product = Assert.Single(repository.Products);
            Assert.Equal("SKU-12345", product.Sku);
            Assert.Equal("Wireless mouse", product.Name);
            Assert.Equal("Peripherals", product.Category);
            Assert.Equal(19.99m, product.Price);
            Assert.Equal(7, product.Stock);
            Assert.Equal(string.Empty, product.Description);
            Assert.True(product.IsActive);
            Assert.Equal(Now.UtcDateTime, product.CreatedAt);
        }

        [Fact]
        public async Task CreateAsync_BoundaryValues_AreAccepted()
        {
            var result = await service.CreateAsync(
                "SKU-00000", new string('n', 200), new string('c', 100), 0.01m, 0);

            Assert.True(result.Success);
            Assert.Single(repository.Products);
        }

        [Fact]
        public async Task CreateAsync_SurroundingSpaces_AreTrimmedBeforeStoring()
        {
            var result = await service.CreateAsync(" SKU-12345 ", "  Mouse  ", "  Peripherals ", 5m, 1);

            Assert.True(result.Success);
            var product = Assert.Single(repository.Products);
            Assert.Equal("SKU-12345", product.Sku);
            Assert.Equal("Mouse", product.Name);
            Assert.Equal("Peripherals", product.Category);
        }

        [Theory]
        [InlineData("")]
        [InlineData("12345")]
        [InlineData("SKU-1234")]
        [InlineData("SKU-123456")]
        [InlineData("SKU-1234A")]
        [InlineData("sku-12345")]
        [InlineData("SKU_12345")]
        public async Task CreateAsync_SkuWithWrongFormat_Fails(string sku)
        {
            var result = await service.CreateAsync(sku, "Wireless mouse", "Peripherals", 19.99m, 7);

            AssertSingleError("SKU must have the format SKU-00000.", result);
        }

        [Fact]
        public async Task CreateWithNameWithoutLetters_IsRejected()
        {
            var result = await service.CreateAsync("SKU-12345", "12345", "Peripherals", 19.99m, 7);

            Assert.False(result.Success);
            Assert.NotEmpty(result.Errors);
            Assert.Empty(repository.Products);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(201)]
        public async Task CreateAsync_NameLengthOutOfRange_Fails(int length)
        {
            var result = await service.CreateAsync("SKU-12345", new string('n', length), "Peripherals", 19.99m, 7);

            AssertSingleError("Name must have between 3 and 200 characters.", result);
        }

        [Fact]
        public async Task CreateAsync_NameOfOnlySpaces_Fails()
        {
            var result = await service.CreateAsync("SKU-12345", "     ", "Peripherals", 19.99m, 7);

            AssertSingleError("Name must have between 3 and 200 characters.", result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(101)]
        public async Task CreateAsync_CategoryLengthOutOfRange_Fails(int length)
        {
            var result = await service.CreateAsync("SKU-12345", "Wireless mouse", new string('c', length), 19.99m, 7);

            AssertSingleError("Category must have between 3 and 100 characters.", result);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-0.01")]
        public async Task CreateAsync_PriceNotGreaterThanZero_Fails(string price)
        {
            var result = await service.CreateAsync("SKU-12345", "Wireless mouse", "Peripherals", decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture), 7);

            AssertSingleError("Price must be greater than zero.", result);
        }

        [Fact]
        public async Task CreateAsync_PriceWithMoreThanTwoDecimals_Fails()
        {
            var result = await service.CreateAsync("SKU-12345", "Wireless mouse", "Peripherals", 19.999m, 7);

            AssertSingleError("Price must have at most 2 decimals.", result);
        }

        [Fact]
        public async Task CreateAsync_NegativeInitialStock_Fails()
        {
            var result = await service.CreateAsync("SKU-12345", "Wireless mouse", "Peripherals", 19.99m, -1);

            AssertSingleError("Initial stock must be zero or more.", result);
        }

        [Fact]
        public async Task CreateAsync_SeveralInvalidValues_ReportsEveryError()
        {
            var result = await service.CreateAsync("bad", "ab", "c", 0m, -5);

            Assert.False(result.Success);
            Assert.Equal(5, result.Errors.Count);
            Assert.Empty(repository.Products);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CreateAsync_DuplicatedSku_Fails(bool existingIsActive)
        {
            repository.Seed("SKU-12345", isActive: existingIsActive);

            var result = await service.CreateAsync("SKU-12345", "Wireless mouse", "Peripherals", 19.99m, 7);

            Assert.False(result.Success);
            Assert.Equal(["A product with SKU SKU-12345 already exists."], result.Errors);
            Assert.Single(repository.Products);
        }

        [Fact]
        public async Task DeactivateAsync_ActiveProductWithoutStock_MarksItInactiveAndKeepsIt()
        {
            var product = repository.Seed("SKU-12345", stock: 0);

            var result = await service.DeactivateAsync("SKU-12345");

            Assert.True(result.Success);
            Assert.False(product.IsActive);
            Assert.Contains(product, repository.Products);
        }

        [Fact]
        public async Task DeactivateAsync_ProductWithStock_FailsAndKeepsItActive()
        {
            var product = repository.Seed("SKU-12345", stock: 3);

            var result = await service.DeactivateAsync("SKU-12345");

            Assert.False(result.Success);
            Assert.Equal(["Product SKU-12345 cannot be deactivated because it still has stock (3)."], result.Errors);
            Assert.True(product.IsActive);
        }

        [Fact]
        public async Task DeactivateAsync_UnknownSku_Fails()
        {
            var result = await service.DeactivateAsync("SKU-99999");

            Assert.False(result.Success);
            Assert.Equal(["No product found with SKU SKU-99999."], result.Errors);
        }

        [Fact]
        public async Task DeactivateAsync_AlreadyInactiveProduct_Fails()
        {
            repository.Seed("SKU-12345", stock: 0, isActive: false);

            var result = await service.DeactivateAsync("SKU-12345");

            Assert.False(result.Success);
            Assert.Equal(["Product SKU-12345 is already deactivated."], result.Errors);
        }

        private void AssertSingleError(string expected, OperationResult result)
        {
            Assert.False(result.Success);
            Assert.Equal([expected], result.Errors);
            Assert.Empty(repository.Products);
        }
    }
}
