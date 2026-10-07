using app_curso_claude.Data;
using app_curso_claude.Models;
using Microsoft.EntityFrameworkCore;

namespace app_curso_claude.Tests.Data
{
    public class AppDbContextModelTests
    {
        [Fact]
        public void Model_ProductIsActive_HasDatabaseDefaultTrue()
        {
            // Building the model reads no data: the connection is never opened.
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=unused;Database=unused")
                .Options;
            using var context = new AppDbContext(options);

            var property = context.Model
                .FindEntityType(typeof(Product))!
                .FindProperty(nameof(Product.IsActive))!;

            Assert.False(property.IsNullable);
            Assert.Equal(true, property.GetDefaultValue());
        }
    }
}
