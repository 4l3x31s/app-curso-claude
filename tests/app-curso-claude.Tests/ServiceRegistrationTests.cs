using app_curso_claude.Data.Repositories;
using app_curso_claude.Data.Repositories.EfCore;
using app_curso_claude.Services;
using Microsoft.Extensions.DependencyInjection;

namespace app_curso_claude.Tests
{
    [Collection(AppCollection.Name)]
    public class ServiceRegistrationTests(AppFactory factory)
    {
        [Fact]
        public void Services_Repositories_ResolveToTheirEfCoreImplementations()
        {
            using var scope = factory.Services.CreateScope();
            var services = scope.ServiceProvider;

            Assert.IsType<EfProductRepository>(services.GetRequiredService<IProductRepository>());
            Assert.IsType<EfCustomerRepository>(services.GetRequiredService<ICustomerRepository>());
            Assert.IsType<EfPurchaseRepository>(services.GetRequiredService<IPurchaseRepository>());
        }

        [Fact]
        public void Services_UnitOfWork_ResolvesToItsEfCoreImplementation()
        {
            using var scope = factory.Services.CreateScope();

            Assert.IsType<EfUnitOfWork>(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        }

        [Fact]
        public void Services_PurchaseService_IsScoped()
        {
            using var firstScope = factory.Services.CreateScope();
            using var secondScope = factory.Services.CreateScope();

            var first = firstScope.ServiceProvider.GetRequiredService<PurchaseService>();
            var sameScope = firstScope.ServiceProvider.GetRequiredService<PurchaseService>();
            var second = secondScope.ServiceProvider.GetRequiredService<PurchaseService>();

            Assert.Same(first, sameScope);
            Assert.NotSame(first, second);
        }

        [Fact]
        public void Services_Repositories_AreScoped()
        {
            using var firstScope = factory.Services.CreateScope();
            using var secondScope = factory.Services.CreateScope();

            var first = firstScope.ServiceProvider.GetRequiredService<IProductRepository>();
            var sameScope = firstScope.ServiceProvider.GetRequiredService<IProductRepository>();
            var second = secondScope.ServiceProvider.GetRequiredService<IProductRepository>();

            Assert.Same(first, sameScope);
            Assert.NotSame(first, second);
        }

        [Fact]
        public void Services_ProductService_IsRegistered()
        {
            using var scope = factory.Services.CreateScope();

            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ProductService>());
        }

        [Fact]
        public void Services_SummaryService_ResolvesInsideAScope()
        {
            using var scope = factory.Services.CreateScope();

            Assert.NotNull(scope.ServiceProvider.GetRequiredService<SummaryService>());
        }

        [Fact]
        public void Services_TimeProvider_ResolvesToTheSystemClock()
        {
            var timeProvider = factory.Services.GetRequiredService<TimeProvider>();

            Assert.Same(TimeProvider.System, timeProvider);
        }
    }
}
