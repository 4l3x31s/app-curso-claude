using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace app_curso_claude.Tests.Pages
{
    /// <summary>
    /// Both states of the "Features:Contact" flag. Only GET requests are sent, so nothing is written.
    /// </summary>
    [Collection(AppCollection.Name)]
    public class ContactFeatureFlagTests(AppFactory factory)
    {
        private const string ContactLink = "href=\"/Contact\"";

        [Fact]
        public async Task Get_ContactWithTheFlagOff_ReturnsNotFound()
        {
            using var app = WithContactFlag(false);
            using var client = app.CreateClient();

            using var response = await client.GetAsync("/Contact");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Get_HomeWithTheFlagOff_ShowsNoLinkToContact()
        {
            using var app = WithContactFlag(false);

            var body = await GetBodyAsync(app, "/");

            Assert.DoesNotContain(ContactLink, body);
        }

        [Fact]
        public async Task Get_ContactWithTheFlagOn_ShowsTheContactDetailsAndTheForm()
        {
            using var app = WithContactFlag(true);

            var body = await GetBodyAsync(app, "/Contact");

            Assert.Contains("support@example.com", body);
            Assert.Contains("<form", body);
            Assert.Contains("name=\"Form.Type\"", body);
            Assert.Contains("name=\"Form.Sku\"", body);
            Assert.Contains("name=\"Form.Email\"", body);
            Assert.Contains("name=\"Form.Message\"", body);
            Assert.Contains(">Solicitud</option>", body);
        }

        [Fact]
        public async Task Get_HomeWithTheFlagOn_ShowsTheContactLinkInTheMenu()
        {
            using var app = WithContactFlag(true);

            var body = await GetBodyAsync(app, "/");

            Assert.Contains(ContactLink, body);
        }

        [Fact]
        public async Task Get_ProductsWithTheFlagOff_HidesTheContactLinkFromTheMenu()
        {
            using var app = WithContactFlag(false);

            var body = await GetBodyAsync(app, "/Products");

            Assert.DoesNotContain(ContactLink, body);
        }

        private WebApplicationFactory<Program> WithContactFlag(bool enabled)
        {
            return factory.WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Features:Contact"] = enabled ? "true" : "false"
                    })));
        }

        private static async Task<string> GetBodyAsync(WebApplicationFactory<Program> app, string url)
        {
            using var client = app.CreateClient();
            using var response = await client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
