using System.Net;
using System.Text.RegularExpressions;
using app_curso_claude.Data.Repositories;
using app_curso_claude.Models;
using app_curso_claude.Tests.Fakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace app_curso_claude.Tests.Pages
{
    /// <summary>
    /// Sends the purchase form to the real application with every repository replaced
    /// by an in-memory fake, so these tests never read or write the shared database.
    /// </summary>
    [Collection(AppCollection.Name)]
    public partial class PurchasesRegisterTests(AppFactory factory)
    {
        private const string Sku = "SKU-TEST";
        private const string InactiveSku = "SKU-INACTIVE";
        private const string PageUrl = $"/Purchases?sku={Sku}";

        private static readonly Customer Buyer = Sample.Customer(7, "Grace", "Hopper");

        private readonly FakeProductRepository _products = new(
            Sample.Product(Sku, stock: 5, id: 1),
            Sample.Product(InactiveSku, stock: 5, isActive: false, id: 2));

        private readonly FakePurchaseRepository _purchases = new(Buyer);

        [Fact]
        public async Task Get_PurchasesWithActiveSku_ShowsTheStockAndTheRegisterForm()
        {
            using var client = CreateClient();

            var body = await client.GetStringAsync(PageUrl);

            Assert.Contains("data-product-stock=\"5\"", body);
            Assert.Contains("action=\"/Purchases/Register\"", body);
            Assert.Contains("name=\"customerId\"", body);
            Assert.Contains("name=\"quantity\"", body);
            Assert.Contains("Grace Hopper", body);
        }

        [Fact]
        public async Task Get_PurchasesWithInactiveSku_ShowsTheStockButNoRegisterForm()
        {
            using var client = CreateClient();

            var body = await client.GetStringAsync($"/Purchases?sku={InactiveSku}");

            Assert.Contains("data-product-stock=\"5\"", body);
            Assert.Contains("This product is inactive and cannot be purchased.", body);
            Assert.DoesNotContain("action=\"/Purchases/Register\"", body);
        }

        [Fact]
        public async Task Post_RegisterValidPurchase_ReducesTheStockAndShowsItOnThePage()
        {
            using var client = CreateClient();

            using var response = await PostRegisterAsync(client, Sku, Buyer.Id.ToString(), "2");
            var body = await response.Content.ReadAsStringAsync();

            // The client follows the redirect back to the page of the product.
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(PageUrl, response.RequestMessage?.RequestUri?.PathAndQuery);
            Assert.Equal(3, _products.StockOf(Sku));
            Assert.Single(_purchases.Saved);
            Assert.Contains("Purchase registered.", body);
            Assert.Contains("data-product-stock=\"3\"", body);
            Assert.Contains("data-purchase-id=\"1\"", body);
        }

        [Fact]
        public async Task Post_RegisterQuantityAboveStock_ShowsTheServiceErrorAndKeepsTheStock()
        {
            using var client = CreateClient();

            using var response = await PostRegisterAsync(client, Sku, Buyer.Id.ToString(), "6");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Not enough stock: 5 available, 6 requested.", body);
            Assert.Contains("data-product-stock=\"5\"", body);
            Assert.Equal(5, _products.StockOf(Sku));
            Assert.Empty(_purchases.Saved);
        }

        [Fact]
        public async Task Post_RegisterRejectedPurchase_KeepsWhatWasEntered()
        {
            using var client = CreateClient();

            using var response = await PostRegisterAsync(client, Sku, Buyer.Id.ToString(), "6");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Contains($"<option value=\"{Buyer.Id}\" selected=\"selected\">", body);
            Assert.Contains("value=\"6\"", body);
        }

        [Fact]
        public async Task Post_RegisterWithoutCustomerOrQuantity_ShowsTheServiceErrors()
        {
            using var client = CreateClient();

            using var response = await PostRegisterAsync(client, Sku, customerId: "", quantity: "");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("The quantity must be between 1 and 100.", body);
            Assert.Empty(_purchases.Saved);
        }

        [Fact]
        public async Task Post_RegisterWithoutAntiforgeryToken_IsRejected()
        {
            using var client = CreateClient();
            using var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["sku"] = Sku,
                ["customerId"] = Buyer.Id.ToString(),
                ["quantity"] = "1"
            });

            using var response = await client.PostAsync("/Purchases/Register", form);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(5, _products.StockOf(Sku));
            Assert.Empty(_purchases.Saved);
        }

        private HttpClient CreateClient()
        {
            return factory
                .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<IProductRepository>(_products);
                    services.AddSingleton<ICustomerRepository>(new FakeCustomerRepository(Buyer));
                    services.AddSingleton<IPurchaseRepository>(_purchases);
                    services.AddSingleton<IUnitOfWork>(new FakeUnitOfWork());
                }))
                .CreateClient();
        }

        private static async Task<HttpResponseMessage> PostRegisterAsync(
            HttpClient client, string sku, string customerId, string quantity)
        {
            // The form is loaded first: it carries the antiforgery token and sets its cookie on the client.
            var page = await client.GetStringAsync(PageUrl);
            var token = AntiforgeryToken().Match(page).Groups["token"].Value;
            Assert.NotEmpty(token);

            using var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["sku"] = sku,
                ["customerId"] = customerId,
                ["quantity"] = quantity,
                ["__RequestVerificationToken"] = token
            });

            return await client.PostAsync("/Purchases/Register", form);
        }

        [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"")]
        private static partial Regex AntiforgeryToken();
    }
}
