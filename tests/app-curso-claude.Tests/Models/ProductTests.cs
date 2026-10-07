using app_curso_claude.Models;

namespace app_curso_claude.Tests.Models
{
    public class ProductTests
    {
        [Fact]
        public void Constructor_NewProduct_IsActiveByDefault()
        {
            var product = new Product
            {
                Sku = "SKU-TEST",
                Name = "Test product",
                Description = "A product used by the tests.",
                Category = "Tests",
            };

            Assert.True(product.IsActive);
        }
    }
}
