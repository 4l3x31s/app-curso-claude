using app_curso_claude.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace app_curso_claude.Tests.Data.Repositories
{
    [Collection(AppCollection.Name)]
    public class EfCustomerRepositoryTests(AppFactory factory)
    {
        [Fact]
        public async Task GetAllAsync_SeededCustomers_ReturnsThemOrderedById()
        {
            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();

            var customers = await repository.GetAllAsync();

            var ids = customers.Select(c => c.Id).ToList();
            Assert.NotEmpty(ids);
            Assert.Equal(ids.Order(), ids);
        }

        [Fact]
        public async Task GetAsync_ExistingId_ReturnsThatCustomer()
        {
            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();
            var existingId = (await repository.GetAllAsync())[0].Id;

            var customer = await repository.GetAsync(existingId);

            Assert.NotNull(customer);
            Assert.Equal(existingId, customer.Id);
        }

        [Fact]
        public async Task GetAsync_UnknownId_ReturnsNull()
        {
            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();

            var customer = await repository.GetAsync(-1);

            Assert.Null(customer);
        }
    }
}
