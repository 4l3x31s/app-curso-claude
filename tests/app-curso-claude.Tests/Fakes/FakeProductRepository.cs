using app_curso_claude.Data.Repositories;
using app_curso_claude.Models;

namespace app_curso_claude.Tests.Fakes
{
    /// <summary>
    /// Keeps the products in a list, so the tests that use it never reach SQL Server.
    /// </summary>
    public class FakeProductRepository : IProductRepository
    {
        public List<Product> Products { get; } = [];

        public Task<IReadOnlyList<Product>> GetAllAsync(bool includeInactive)
        {
            IReadOnlyList<Product> result = Products
                .Where(p => includeInactive || p.IsActive)
                .OrderBy(p => p.Sku, StringComparer.Ordinal)
                .ToList();

            return Task.FromResult(result);
        }

        public Task<Product?> GetAsync(string sku)
        {
            return Task.FromResult(Products.FirstOrDefault(p => p.Sku == sku));
        }

        public Task AddAsync(Product product)
        {
            Products.Add(product);
            return Task.CompletedTask;
        }

        public Task<bool> DeactivateAsync(string sku)
        {
            var product = Products.FirstOrDefault(p => p.Sku == sku && p.IsActive);
            if (product is null)
            {
                return Task.FromResult(false);
            }

            product.IsActive = false;
            return Task.FromResult(true);
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
    }
}
