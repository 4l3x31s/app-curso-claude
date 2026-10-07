using System.Net;
using System.Text.RegularExpressions;
using app_curso_claude.Data.Repositories;
using app_curso_claude.Tests.Fakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace app_curso_claude.Tests.Pages
{
    /// <summary>
    /// Drives the Products page over a fake repository, so sending its forms never writes to SQL Server.
    /// </summary>
    [Collection(AppCollection.Name)]
    public class ProductsPageTests(AppFactory factory)
    {
        private readonly FakeProductRepository repository = new();

        [Fact]
        public async Task Get_Products_ShowsTheCreationFormAndTheInactiveCheckUnchecked()
        {
            using var client = CreateClient();

            var body = await GetBodyAsync(client, "/Products");

            Assert.Contains("action=\"/Products/Create\"", body);
            Assert.Contains("Mostrar productos dados de baja", body);
            Assert.DoesNotMatch("<input[^>]*id=\"showInactive\"[^>]*checked", body);
        }

        [Fact]
        public async Task Get_Products_HidesInactiveProductsByDefault()
        {
            repository.Seed("SKU-00001");
            repository.Seed("SKU-00002", isActive: false);
            using var client = CreateClient();

            var body = await GetBodyAsync(client, "/Products");

            Assert.Contains("SKU-00001", body);
            Assert.DoesNotContain("SKU-00002", body);
        }

        [Fact]
        public async Task Get_ProductsShowingInactive_ListsThemWithoutTheDeactivateButton()
        {
            repository.Seed("SKU-00001");
            repository.Seed("SKU-00002", isActive: false);
            using var client = CreateClient();

            var body = await GetBodyAsync(client, "/Products?showInactive=true");

            Assert.Contains("SKU-00002", body);
            Assert.Matches("<input[^>]*id=\"showInactive\"[^>]*checked", body);
            Assert.Contains("value=\"SKU-00001\"", body);
            Assert.DoesNotContain("value=\"SKU-00002\"", body);
        }

        [Fact]
        public async Task Get_Products_ShowsADeactivateButtonPerActiveRow()
        {
            repository.Seed("SKU-00001");
            repository.Seed("SKU-00002");
            using var client = CreateClient();

            var body = await GetBodyAsync(client, "/Products");

            Assert.Equal(2, Regex.Matches(body, ">Dar de baja</button>").Count);
        }

        [Fact]
        public async Task Post_CreateWithValidData_StoresTheProductAndRedirectsToTheList()
        {
            using var client = CreateClient();

            using var response = await PostAsync(client, "/Products/Create", new()
            {
                ["Sku"] = "SKU-12345",
                ["Name"] = "Wireless mouse",
                ["Category"] = "Peripherals",
                ["Price"] = "19.99",
                ["InitialStock"] = "7"
            });

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            var product = Assert.Single(repository.Products);
            Assert.Equal("SKU-12345", product.Sku);
            Assert.Equal(19.99m, product.Price);
            Assert.Equal(7, product.Stock);
        }

        [Fact]
        public async Task Post_CreateWithInvalidData_ShowsTheServiceErrorsAndKeepsTheTypedValues()
        {
            using var client = CreateClient();

            using var response = await PostAsync(client, "/Products/Create", new()
            {
                ["Sku"] = "BAD-SKU",
                ["Name"] = "Wireless mouse",
                ["Category"] = "Peripherals",
                ["Price"] = "19.999",
                ["InitialStock"] = "7"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("SKU must have the format SKU-00000.", body);
            Assert.Contains("Price must have at most 2 decimals.", body);
            Assert.Contains("value=\"BAD-SKU\"", body);
            Assert.Empty(repository.Products);
        }

        [Fact]
        public async Task Post_CreateWithNonNumericPrice_ShowsAnError()
        {
            using var client = CreateClient();

            using var response = await PostAsync(client, "/Products/Create", new()
            {
                ["Sku"] = "SKU-12345",
                ["Name"] = "Wireless mouse",
                ["Category"] = "Peripherals",
                ["Price"] = "19,99",
                ["InitialStock"] = "7"
            });

            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("Price must be a number, for example 19.99.", body);
            Assert.Empty(repository.Products);
        }

        [Fact]
        public async Task Post_DeactivateProductWithoutStock_DeactivatesItAndRedirects()
        {
            var product = repository.Seed("SKU-00001", stock: 0);
            using var client = CreateClient();

            using var response = await PostAsync(client, "/Products/Deactivate", new() { ["sku"] = "SKU-00001" });

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.False(product.IsActive);
            Assert.Contains(product, repository.Products);
        }

        [Fact]
        public async Task Post_DeactivateProductWithStock_ShowsTheServiceError()
        {
            var product = repository.Seed("SKU-00001", stock: 4);
            using var client = CreateClient();

            using var response = await PostAsync(client, "/Products/Deactivate", new() { ["sku"] = "SKU-00001" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("Product SKU-00001 cannot be deactivated because it still has stock (4).", body);
            Assert.True(product.IsActive);
        }

        [Fact]
        public async Task Post_CreateWithoutAntiforgeryToken_IsRejected()
        {
            using var client = CreateClient();

            using var response = await client.PostAsync(
                "/Products/Create",
                new FormUrlEncodedContent(new Dictionary<string, string> { ["Sku"] = "SKU-12345" }));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Empty(repository.Products);
        }

        private HttpClient CreateClient()
        {
            return factory
                .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IProductRepository>();
                    services.AddSingleton<IProductRepository>(repository);
                }))
                .CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }

        private static async Task<string> GetBodyAsync(HttpClient client, string url)
        {
            using var response = await client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        // Loads the page first: the forms need its antiforgery token, and the client keeps the cookie.
        private static async Task<HttpResponseMessage> PostAsync(
            HttpClient client, string url, Dictionary<string, string> fields)
        {
            var page = await GetBodyAsync(client, "/Products");
            var token = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
            Assert.NotEqual(string.Empty, token);
            fields["__RequestVerificationToken"] = token;

            return await client.PostAsync(url, new FormUrlEncodedContent(fields));
        }
    }
}
