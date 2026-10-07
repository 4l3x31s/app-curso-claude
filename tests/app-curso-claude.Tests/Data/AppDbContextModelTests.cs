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

        [Fact]
        public void Model_ContactRequest_EveryTextColumnHasAMaxLengthAndTheFolioIsUnique()
        {
            // Building the model reads no data: the connection is never opened.
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=unused;Database=unused")
                .Options;
            using var context = new AppDbContext(options);

            var entity = context.Model.FindEntityType(typeof(ContactRequest))!;

            var textColumns = entity.GetProperties().Where(p => p.ClrType == typeof(string)).ToList();
            Assert.Equal(5, textColumns.Count);
            Assert.All(textColumns, column => Assert.NotNull(column.GetMaxLength()));
            Assert.Equal(500, entity.FindProperty(nameof(ContactRequest.Message))!.GetMaxLength());
            Assert.True(entity.FindProperty(nameof(ContactRequest.Sku))!.IsNullable);

            var folioIndex = Assert.Single(entity.GetIndexes());
            Assert.Equal(nameof(ContactRequest.Folio), Assert.Single(folioIndex.Properties).Name);
            Assert.True(folioIndex.IsUnique);
        }
    }
}
