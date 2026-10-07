using System.Text.RegularExpressions;
using app_curso_claude.Data.Repositories;
using app_curso_claude.Models;

namespace app_curso_claude.Services
{
    /// <summary>
    /// Business rules to create products and to deactivate them.
    /// </summary>
    public partial class ProductService(IProductRepository products, TimeProvider timeProvider)
    {
        private const int MinTextLength = 3;
        private const int MaxNameLength = 200;
        private const int MaxCategoryLength = 100;

        /// <summary>
        /// Creates an active product with an empty description.
        /// </summary>
        /// <param name="sku">Unique SKU with the format <c>SKU-00000</c>.</param>
        /// <param name="name">Name, from 3 to 200 characters.</param>
        /// <param name="category">Category, from 3 to 100 characters.</param>
        /// <param name="price">Price, greater than zero and with at most 2 decimals.</param>
        /// <param name="initialStock">Initial stock, zero or more.</param>
        /// <returns>A successful result, or a failed one with every rule that was not met.</returns>
        public async Task<OperationResult> CreateAsync(
            string sku, string name, string category, decimal price, int initialStock)
        {
            sku = (sku ?? string.Empty).Trim();
            name = (name ?? string.Empty).Trim();
            category = (category ?? string.Empty).Trim();

            var errors = new List<string>();
            var skuHasValidFormat = SkuFormat().IsMatch(sku);

            if (!skuHasValidFormat)
            {
                errors.Add("SKU must have the format SKU-00000.");
            }

            if (name.Length < MinTextLength || name.Length > MaxNameLength)
            {
                errors.Add($"Name must have between {MinTextLength} and {MaxNameLength} characters.");
            }

            if (category.Length < MinTextLength || category.Length > MaxCategoryLength)
            {
                errors.Add($"Category must have between {MinTextLength} and {MaxCategoryLength} characters.");
            }

            if (price <= 0)
            {
                errors.Add("Price must be greater than zero.");
            }
            else if (decimal.Round(price, 2) != price)
            {
                errors.Add("Price must have at most 2 decimals.");
            }

            if (initialStock < 0)
            {
                errors.Add("Initial stock must be zero or more.");
            }

            // Only a well-formed SKU is looked up: a malformed one cannot be stored anyway.
            if (skuHasValidFormat && await products.GetAsync(sku) is not null)
            {
                errors.Add($"A product with SKU {sku} already exists.");
            }

            if (errors.Count > 0)
            {
                return OperationResult.Fail([.. errors]);
            }

            await products.AddAsync(new Product
            {
                Sku = sku,
                Name = name,
                Description = string.Empty,
                Category = category,
                Price = price,
                Stock = initialStock,
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
                IsActive = true
            });

            return OperationResult.Ok();
        }

        /// <summary>
        /// Deactivates a product. The product must exist, be active and have no stock.
        /// </summary>
        /// <param name="sku">SKU of the product to deactivate.</param>
        /// <returns>A successful result, or a failed one with the rule that was not met.</returns>
        public async Task<OperationResult> DeactivateAsync(string sku)
        {
            sku = (sku ?? string.Empty).Trim();

            var product = await products.GetAsync(sku);
            if (product is null)
            {
                return OperationResult.Fail($"No product found with SKU {sku}.");
            }

            if (!product.IsActive)
            {
                return OperationResult.Fail($"Product {sku} is already deactivated.");
            }

            if (product.Stock != 0)
            {
                return OperationResult.Fail(
                    $"Product {sku} cannot be deactivated because it still has stock ({product.Stock}).");
            }

            // False means another request deactivated it between the check and the write.
            if (!await products.DeactivateAsync(sku))
            {
                return OperationResult.Fail($"Product {sku} is already deactivated.");
            }

            return OperationResult.Ok();
        }

        [GeneratedRegex(@"^SKU-[0-9]{5}\z")]
        private static partial Regex SkuFormat();
    }
}
