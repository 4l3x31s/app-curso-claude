using app_curso_claude.Models;
using Bogus;
using Microsoft.EntityFrameworkCore;

namespace app_curso_claude.Data
{
    /// <summary>
    /// Number of rows inserted into each table by a <see cref="DbSeeder"/> run.
    /// </summary>
    public record SeedResult(int Customers, int Products, int Purchases);

    /// <summary>
    /// Fills the customers, products and purchases tables with generated sample data.
    /// </summary>
    public static class DbSeeder
    {
        private const int RowsPerTable = 500;

        // Fixed seeds and reference date keep the generated data identical between runs.
        private const int CustomerSeed = 20260101;
        private const int ProductSeed = 20260102;
        private const int PurchaseSeed = 20260103;
        private const string Locale = "es";

        private static readonly DateTime ReferenceDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Inserts the sample rows. A table that already has rows is left untouched, and
        /// purchases are only generated when both customers and products exist.
        /// </summary>
        /// <param name="context">The database context to seed.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The number of rows inserted per table.</returns>
        public static async Task<SeedResult> SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);

            var customers = 0;
            if (!await context.Customers.AnyAsync(cancellationToken))
            {
                context.Customers.AddRange(CreateCustomerFaker().Generate(RowsPerTable));
                customers = await context.SaveChangesAsync(cancellationToken);
            }

            var products = 0;
            if (!await context.Products.AnyAsync(cancellationToken))
            {
                context.Products.AddRange(CreateProductFaker().Generate(RowsPerTable));
                products = await context.SaveChangesAsync(cancellationToken);
            }

            var purchases = 0;
            if (!await context.Purchases.AnyAsync(cancellationToken))
            {
                // Read back in key order so the same rows are picked on every run.
                var customerRows = await context.Customers
                    .AsNoTracking()
                    .OrderBy(c => c.Id)
                    .Select(c => new CustomerRow(c.Id, c.CreatedAt))
                    .ToListAsync(cancellationToken);

                var productRows = await context.Products
                    .AsNoTracking()
                    .OrderBy(p => p.Id)
                    .Select(p => new ProductRow(p.Id, p.Price))
                    .ToListAsync(cancellationToken);

                if (customerRows.Count > 0 && productRows.Count > 0)
                {
                    context.Purchases.AddRange(CreatePurchases(customerRows, productRows));
                    purchases = await context.SaveChangesAsync(cancellationToken);
                }
            }

            return new SeedResult(customers, products, purchases);
        }

        private static Faker<Customer> CreateCustomerFaker() =>
            new Faker<Customer>(Locale)
                .UseSeed(CustomerSeed)
                .UseDateTimeReference(ReferenceDate)
                .RuleFor(c => c.FirstName, f => Truncate(f.Name.FirstName(), 100))
                .RuleFor(c => c.LastName, f => Truncate(f.Name.LastName(), 100))
                // The fixed-width row number keeps every email unique.
                .RuleFor(c => c.Email, (f, c) =>
                    $"{Truncate(f.Internet.UserName(c.FirstName, c.LastName), 64)}.{f.IndexFaker + 1:D4}@example.com".ToLowerInvariant())
                .RuleFor(c => c.Phone, f => Truncate(f.Phone.PhoneNumber(), 30))
                .RuleFor(c => c.Address, f => Truncate(f.Address.StreetAddress(), 200))
                .RuleFor(c => c.City, f => Truncate(f.Address.City(), 100))
                .RuleFor(c => c.Country, f => Truncate(f.Address.Country(), 100))
                .RuleFor(c => c.CreatedAt, f => f.Date.Past(3));

        private static Faker<Product> CreateProductFaker() =>
            new Faker<Product>(Locale)
                .UseSeed(ProductSeed)
                .UseDateTimeReference(ReferenceDate)
                // The row number keeps every SKU unique.
                .RuleFor(p => p.Sku, f => $"SKU-{f.IndexFaker + 1:D5}")
                .RuleFor(p => p.Name, f => Truncate(f.Commerce.ProductName(), 200))
                .RuleFor(p => p.Description, f => Truncate(f.Commerce.ProductDescription(), 1000))
                .RuleFor(p => p.Category, f => Truncate(f.Commerce.Categories(1)[0], 100))
                .RuleFor(p => p.Price, f => f.Finance.Amount(1m, 2000m, 2))
                .RuleFor(p => p.Stock, f => f.Random.Int(0, 500))
                .RuleFor(p => p.CreatedAt, f => f.Date.Past(3));

        private static List<Purchase> CreatePurchases(List<CustomerRow> customers, List<ProductRow> products)
        {
            var faker = new Faker(Locale) { Random = new Randomizer(PurchaseSeed) };
            var purchases = new List<Purchase>(RowsPerTable);

            for (var i = 0; i < RowsPerTable; i++)
            {
                var customer = faker.PickRandom(customers);
                var product = faker.PickRandom(products);

                purchases.Add(new Purchase
                {
                    CustomerId = customer.Id,
                    ProductId = product.Id,
                    Quantity = faker.Random.Int(1, 5),
                    UnitPrice = product.Price,
                    // Never earlier than the moment the customer was created.
                    PurchasedAt = faker.Date.Between(customer.CreatedAt, ReferenceDate)
                });
            }

            return purchases;
        }

        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength];

        private sealed record CustomerRow(int Id, DateTime CreatedAt);

        private sealed record ProductRow(int Id, decimal Price);
    }
}
