using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace app_curso_claude.Tests
{
    /// <summary>
    /// Hosts the real application for the tests. It runs in the Development environment
    /// so the database credentials load from user secrets, and it only reads from SQL Server.
    /// </summary>
    public class AppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
        }
    }

    /// <summary>
    /// Shares one application host between every test class that needs SQL Server.
    /// </summary>
    [CollectionDefinition(Name)]
    public class AppCollection : ICollectionFixture<AppFactory>
    {
        public const string Name = "App";
    }
}
