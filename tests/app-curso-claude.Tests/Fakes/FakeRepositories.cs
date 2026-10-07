using app_curso_claude.Data.Repositories;
using app_curso_claude.Models;

namespace app_curso_claude.Tests.Fakes
{
    /// <summary>
    /// Keeps the products in memory. Reads return copies, as a real repository returns
    /// untracked rows, and every call takes <see cref="Latency"/> so simultaneous callers overlap.
    /// </summary>
    public sealed class FakeProductRepository(params Product[] products) : IProductRepository
    {
        private readonly Dictionary<string, Product> _products = products.ToDictionary(p => p.Sku);

        /// <summary>Time every call takes, as a round trip to a database would.</summary>
        public TimeSpan Latency { get; init; }

        /// <summary>Number of times the stock was written.</summary>
        public int StockUpdates { get; private set; }

        public int StockOf(string sku)
        {
            return _products[sku].Stock;
        }

        public async Task<IReadOnlyList<Product>> GetAllAsync()
        {
            await Task.Delay(Latency);

            return _products.Values
                .OrderBy(p => p.Sku, StringComparer.Ordinal)
                .Select(Copy)
                .ToList();
        }

        public async Task<Product?> GetAsync(string sku)
        {
            await Task.Delay(Latency);

            return _products.TryGetValue(sku, out var product) ? Copy(product) : null;
        }

        public async Task UpdateStockAsync(string sku, int newStock)
        {
            await Task.Delay(Latency);

            _products[sku].Stock = newStock;
            StockUpdates++;
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

    /// <summary>
    /// Keeps the customers in memory.
    /// </summary>
    public sealed class FakeCustomerRepository(params Customer[] customers) : ICustomerRepository
    {
        public Task<IReadOnlyList<Customer>> GetAllAsync()
        {
            return Task.FromResult<IReadOnlyList<Customer>>(customers.OrderBy(c => c.Id).ToList());
        }

        public Task<Customer?> GetAsync(int id)
        {
            return Task.FromResult(customers.FirstOrDefault(c => c.Id == id));
        }
    }

    /// <summary>
    /// Keeps the purchases in memory and numbers them from 1. The customers it is given
    /// are attached to the purchases it returns, as the real repository loads them.
    /// </summary>
    public sealed class FakePurchaseRepository(params Customer[] customers) : IPurchaseRepository
    {
        private readonly List<Purchase> _purchases = [];

        /// <summary>Purchases saved so far, in the order they were added.</summary>
        public IReadOnlyList<Purchase> Saved => _purchases;

        public Task<IReadOnlyList<Purchase>> GetByProductAsync(int productId)
        {
            var purchases = _purchases
                .Where(p => p.ProductId == productId)
                .OrderByDescending(p => p.PurchasedAt)
                .ThenByDescending(p => p.Id)
                .ToList();

            foreach (var purchase in purchases)
            {
                purchase.Customer = customers.First(c => c.Id == purchase.CustomerId);
            }

            return Task.FromResult<IReadOnlyList<Purchase>>(purchases);
        }

        public async Task<int> AddAsync(Purchase purchase)
        {
            await Task.Yield();

            purchase.Id = _purchases.Count + 1;
            _purchases.Add(purchase);

            return purchase.Id;
        }
    }

    /// <summary>
    /// Runs the work directly and counts how many units of work were started.
    /// </summary>
    public sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int Executions { get; private set; }

        public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
        {
            Executions++;

            return operation();
        }
    }

    /// <summary>
    /// A clock stopped at a known instant.
    /// </summary>
    public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }
    }

    /// <summary>
    /// Builds sample entities with the required members already filled in.
    /// </summary>
    public static class Sample
    {
        public static Product Product(string sku, int stock, decimal price = 10m, bool isActive = true, int id = 1)
        {
            return new Product
            {
                Id = id,
                Sku = sku,
                Name = $"Product {sku}",
                Description = "Sample product",
                Category = "Samples",
                Price = price,
                Stock = stock,
                IsActive = isActive
            };
        }

        public static Customer Customer(int id, string firstName = "Ada", string lastName = "Lovelace")
        {
            return new Customer
            {
                Id = id,
                FirstName = firstName,
                LastName = lastName,
                Email = $"customer{id}@example.com",
                Phone = "000-0000",
                Address = "1 Sample Street",
                City = "Sample City",
                Country = "Sampleland"
            };
        }
    }
}
