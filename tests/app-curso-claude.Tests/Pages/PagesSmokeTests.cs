using System.Net;
using app_curso_claude.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace app_curso_claude.Tests.Pages
{
    [Collection(AppCollection.Name)]
    public class PagesSmokeTests(AppFactory factory)
    {
        private const string UnknownSku = "SKU-DOES-NOT-EXIST";

        [Theory]
        [InlineData("/")]
        [InlineData("/Products")]
        [InlineData("/Purchases")]
        [InlineData("/Contact")]
        public async Task Get_Page_ReturnsOk(string url)
        {
            using var client = factory.CreateClient();

            using var response = await client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Get_Products_ShowsTheFirstSeededSku()
        {
            var body = await GetBodyAsync("/Products");

            Assert.Contains("SKU-00001", body);
        }

        [Fact]
        public async Task Get_Home_ShowsTheThreeSummaryCards()
        {
            var body = await GetBodyAsync("/");

            Assert.Contains("data-summary=\"active-products\"", body);
            Assert.Contains("data-summary=\"units-in-stock\"", body);
            Assert.Contains("data-summary=\"inventory-value\"", body);
            Assert.Contains("Active products", body);
            Assert.Contains("Units in stock", body);
            Assert.Contains("Inventory value", body);
        }

        [Fact]
        public async Task Get_Home_KeepsTheTwoNavigationCards()
        {
            var body = await GetBodyAsync("/");

            Assert.Contains("View contact details", body);
            Assert.Contains("View addresses", body);
        }

        [Fact]
        public async Task Get_Home_ListsTheLatestPurchasesNewestFirstWithoutContactData()
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var latest = await context.Purchases
                .OrderByDescending(p => p.PurchasedAt)
                .ThenByDescending(p => p.Id)
                .Select(p => new
                {
                    p.Id,
                    p.Customer.FirstName,
                    p.Customer.LastName,
                    p.Customer.Email,
                    p.Customer.Phone,
                    p.Customer.Address
                })
                .Take(5)
                .ToListAsync();
            Assert.NotEmpty(latest);

            var body = WebUtility.HtmlDecode(await GetBodyAsync("/"));

            var positions = latest
                .Select(p => body.IndexOf($"data-purchase-id=\"{p.Id}\"", StringComparison.Ordinal))
                .ToList();
            Assert.DoesNotContain(-1, positions);
            Assert.Equal(positions.Order(), positions);
            Assert.All(latest, p =>
            {
                Assert.Contains($"{p.FirstName} {p.LastName}", body);
                Assert.DoesNotContain(p.Email, body);
                Assert.DoesNotContain(p.Phone, body);
                Assert.DoesNotContain(p.Address, body);
            });
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/Products")]
        public async Task Get_Page_ShowsTheMenuLinksToBothPages(string url)
        {
            var body = await GetBodyAsync(url);

            Assert.Contains("href=\"/Products\"", body);
            Assert.Contains("href=\"/Purchases\"", body);
        }

        [Fact]
        public async Task Get_PurchasesWithoutSku_ShowsOnlyTheProductSelect()
        {
            var body = await GetBodyAsync("/Purchases");

            Assert.Contains("<select", body);
            Assert.Contains("SKU-00001", body);
            Assert.DoesNotContain("<table", body);
        }

        [Fact]
        public async Task Get_PurchasesWithExistingSku_ListsThatProductsPurchasesNewestFirst()
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // The product is found by querying, so the test does not depend on a seeded id.
            var productId = await context.Purchases
                .GroupBy(p => p.ProductId)
                .Where(group => group.Count() >= 2)
                .Select(group => group.Key)
                .FirstAsync();
            var sku = await context.Products
                .Where(p => p.Id == productId)
                .Select(p => p.Sku)
                .SingleAsync();
            var expectedIds = await context.Purchases
                .Where(p => p.ProductId == productId)
                .OrderByDescending(p => p.PurchasedAt)
                .ThenByDescending(p => p.Id)
                .Select(p => p.Id)
                .ToListAsync();

            var body = await GetBodyAsync($"/Purchases?sku={Uri.EscapeDataString(sku)}");

            // Each row carries its purchase id, so the order on the page can be compared.
            var positions = expectedIds
                .Select(id => body.IndexOf($"data-purchase-id=\"{id}\"", StringComparison.Ordinal))
                .ToList();
            Assert.DoesNotContain(-1, positions);
            Assert.Equal(positions.Order(), positions);
        }

        [Fact]
        public async Task Get_PurchasesWithExistingSku_ShowsCustomerNameButNoContactData()
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var purchase = await context.Purchases
                .OrderBy(p => p.Id)
                .Select(p => new
                {
                    p.Product.Sku,
                    p.Customer.FirstName,
                    p.Customer.LastName,
                    p.Customer.Email,
                    p.Customer.Phone,
                    p.Customer.Address
                })
                .FirstAsync();

            var body = WebUtility.HtmlDecode(
                await GetBodyAsync($"/Purchases?sku={Uri.EscapeDataString(purchase.Sku)}"));

            Assert.Contains($"{purchase.FirstName} {purchase.LastName}", body);
            Assert.DoesNotContain(purchase.Email, body);
            Assert.DoesNotContain(purchase.Phone, body);
            Assert.DoesNotContain(purchase.Address, body);
        }

        [Fact]
        public async Task Get_PurchasesWithUnknownSku_ShowsANoticeAndNoPurchases()
        {
            var body = await GetBodyAsync($"/Purchases?sku={UnknownSku}");

            Assert.Contains($"No product found with SKU {UnknownSku}.", body);
            Assert.DoesNotContain("<table", body);
        }

        [Fact]
        public async Task Get_PurchasesOfProductWithoutPurchases_ShowsTheEmptyState()
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sku = await context.Products
                .Where(p => !p.Purchases.Any())
                .OrderBy(p => p.Sku)
                .Select(p => p.Sku)
                .FirstOrDefaultAsync();
            Assert.True(sku is not null, "The seeded data has no product without purchases to test with.");

            var body = await GetBodyAsync($"/Purchases?sku={Uri.EscapeDataString(sku)}");

            Assert.Contains("This product has no purchases yet.", body);
            Assert.DoesNotContain("<table", body);
        }

        private async Task<string> GetBodyAsync(string url)
        {
            using var client = factory.CreateClient();
            using var response = await client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
