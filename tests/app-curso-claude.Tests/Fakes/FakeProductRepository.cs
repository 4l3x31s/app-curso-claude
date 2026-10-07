using app_curso_claude.Data.Repositories;
using app_curso_claude.Models;

namespace app_curso_claude.Tests.Fakes
{
    /// <summary>
    /// Keeps the products in a list, so the tests that use it never reach SQL Server.
    /// Reads return copies, as a real repository returns untracked rows, and every call
    /// takes <see cref="Latency"/> so simultaneous callers overlap.
    /// </summary>
    public sealed class FakeProductRepository(params Product[] products) : IProductRepository
    {
        /// <summary>Products stored so far, in the order they were added.</summary>
        public List<Product> Products { get; } = [.. products];

        /// <summary>Time every call takes, as a round trip to a database would.</summary>
        public TimeSpan Latency { get; init; }

        /// <summary>Number of times the stock was written.</summary>
        public int StockUpdates { get; private set; }

        public int StockOf(string sku)
        {
            return Products.First(p => p.Sku == sku).Stock;
        }

        public async Task<IReadOnlyList<Product>> GetAllAsync(bool includeInactive)
        {
            await Task.Delay(Latency);

            return Products
                .Where(p => includeInactive || p.IsActive)
                .OrderBy(p => p.Sku, StringComparer.Ordinal)
                .Select(Copy)
                .ToList();
        }

        public async Task<Product?> GetAsync(string sku)
        {
            await Task.Delay(Latency);

            var product = Products.FirstOrDefault(p => p.Sku == sku);
            return product is null ? null : Copy(product);
        }

        public async Task AddAsync(Product product)
        {
            await Task.Delay(Latency);

            Products.Add(product);
        }

        public async Task<bool> DeactivateAsync(string sku)
        {
            await Task.Delay(Latency);

            var product = Products.FirstOrDefault(p => p.Sku == sku && p.IsActive);
            if (product is null)
            {
                return false;
            }

            product.IsActive = false;
            return true;
        }

        public async Task UpdateStockAsync(string sku, int newStock)
        {
            await Task.Delay(Latency);

            Products.First(p => p.Sku == sku).Stock = newStock;
            StockUpdates++;
        }

        /// <summary>
        /// Creates a repository that knows only the given SKUs.
        /// </summary>
        public static FakeProductRepository WithSkus(params string[] skus)
        {
            var repository = new FakeProductRepository();
            foreach (var sku in skus)
            {
                repository.Seed(sku);
            }

            return repository;
        }

        /// <summary>
        /// Adds a product that already exists before the test acts.
        /// </summary>
        public Product Seed(string sku, int stock = 0, bool isActive = true)
        {
            var product = new Product
            {
                Sku = sku,
                Name = "Existing product",
                Description = string.Empty,
                Category = "Existing",
                Price = 10.00m,
                Stock = stock,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = isActive
            };
            Products.Add(product);
            return product;
        }

        private static Product Copy(Product product)
        {
            return new Product
            {
                Id = product.Id,
                Sku = product.Sku,
                Name = product.Name,
                Description = product.Description,
                Category = product.Category,
                Price = product.Price,
                Stock = product.Stock,
                CreatedAt = product.CreatedAt,
                IsActive = product.IsActive
            };
        }
    }
}
