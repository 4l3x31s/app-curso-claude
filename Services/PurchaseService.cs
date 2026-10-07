using app_curso_claude.Data.Repositories;
using app_curso_claude.Models;

namespace app_curso_claude.Services
{
    /// <summary>
    /// Business rules for registering purchases.
    /// </summary>
    public class PurchaseService(
        IProductRepository products,
        ICustomerRepository customers,
        IPurchaseRepository purchases,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        /// <summary>Smallest quantity a purchase may have.</summary>
        public const int MinQuantity = 1;

        /// <summary>Largest quantity a purchase may have.</summary>
        public const int MaxQuantity = 100;

        // Shared by every instance because the service is scoped: each request gets its own.
        // It lets one registration at a time read the stock and write it back.
        private static readonly SemaphoreSlim RegistrationGate = new(1, 1);

        /// <summary>
        /// Registers the purchase of a product by a customer and takes the quantity out of the stock.
        /// </summary>
        /// <param name="sku">SKU of the product bought; it must exist and be active.</param>
        /// <param name="customerId">Id of the customer who buys; it must exist.</param>
        /// <param name="quantity">Units bought, from <see cref="MinQuantity"/> to <see cref="MaxQuantity"/> and no more than the stock.</param>
        /// <returns>Success, or a failure with the reasons; a failure saves nothing.</returns>
        public async Task<OperationResult> RegisterAsync(string sku, int customerId, int quantity)
        {
            var requestedSku = sku?.Trim() ?? string.Empty;
            var errors = new List<string>();

            if (requestedSku.Length == 0)
            {
                errors.Add("The SKU is required.");
            }

            if (quantity is < MinQuantity or > MaxQuantity)
            {
                errors.Add($"The quantity must be between {MinQuantity} and {MaxQuantity}.");
            }

            if (errors.Count > 0)
            {
                return OperationResult.Fail([.. errors]);
            }

            await RegistrationGate.WaitAsync();
            try
            {
                // The stock is read and written back in the same transaction as the purchase.
                return await unitOfWork.ExecuteInTransactionAsync(
                    () => RegisterInTransactionAsync(requestedSku, customerId, quantity));
            }
            finally
            {
                RegistrationGate.Release();
            }
        }

        private async Task<OperationResult> RegisterInTransactionAsync(string sku, int customerId, int quantity)
        {
            var product = await products.GetAsync(sku);
            if (product is null)
            {
                return OperationResult.Fail($"No product found with SKU {sku}.");
            }

            if (!product.IsActive)
            {
                return OperationResult.Fail($"The product {sku} is inactive and cannot be purchased.");
            }

            if (await customers.GetAsync(customerId) is null)
            {
                return OperationResult.Fail("The customer does not exist.");
            }

            if (quantity > product.Stock)
            {
                return OperationResult.Fail($"Not enough stock: {product.Stock} available, {quantity} requested.");
            }

            await purchases.AddAsync(new Purchase
            {
                CustomerId = customerId,
                ProductId = product.Id,
                Quantity = quantity,
                UnitPrice = product.Price,
                PurchasedAt = timeProvider.GetUtcNow().UtcDateTime
            });
            await products.UpdateStockAsync(sku, product.Stock - quantity);

            return OperationResult.Ok();
        }
    }
}
