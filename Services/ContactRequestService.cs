using System.Net.Mail;
using app_curso_claude.Data.Repositories;
using app_curso_claude.Models;

namespace app_curso_claude.Services
{
    /// <summary>
    /// Validates and registers the requests sent from the contact page.
    /// </summary>
    public class ContactRequestService(
        IContactRequestRepository requests,
        IProductRepository products,
        TimeProvider timeProvider,
        ILogger<ContactRequestService> logger)
    {
        public const int MinMessageLength = 10;
        public const int MaxMessageLength = 500;
        public const int MaxEmailLength = 256;

        public static readonly IReadOnlyList<string> RequestTypes = ["Consulta", "Reclamo", "Solicitud"];

        /// <summary>
        /// Registers a contact request and assigns it the next folio of the current year.
        /// </summary>
        /// <param name="type">Consulta, Reclamo or Solicitud.</param>
        /// <param name="sku">SKU the request is about; optional, but it must exist when given.</param>
        /// <param name="email">Email address to answer to.</param>
        /// <param name="message">Text of the request, 10 to 500 characters.</param>
        public async Task<ContactRequestResult> SendAsync(string? type, string? sku, string? email, string? message)
        {
            type = type?.Trim() ?? string.Empty;
            sku = string.IsNullOrWhiteSpace(sku) ? null : sku.Trim();
            email = email?.Trim() ?? string.Empty;
            message = message?.Trim() ?? string.Empty;

            var errors = new List<string>();

            if (!RequestTypes.Contains(type))
            {
                errors.Add("Type must be Consulta, Reclamo or Solicitud.");
            }

            if (!IsValidEmail(email))
            {
                errors.Add("Email is not valid.");
            }

            if (message.Length is < MinMessageLength or > MaxMessageLength)
            {
                errors.Add($"Message must be between {MinMessageLength} and {MaxMessageLength} characters.");
            }

            if (sku is not null && await products.GetAsync(sku) is null)
            {
                errors.Add($"No product found with SKU {sku}.");
            }

            if (errors.Count > 0)
            {
                return ContactRequestResult.Rejected(errors);
            }

            var now = timeProvider.GetUtcNow().UtcDateTime;
            var folio = $"SOL-{now.Year}-{await requests.CountByYearAsync(now.Year) + 1:D4}";

            await requests.AddAsync(new ContactRequest
            {
                Folio = folio,
                Type = type,
                Sku = sku,
                Email = email,
                Message = message,
                CreatedAt = now
            });

            // The email is personal data and must stay out of the logs.
            logger.LogInformation("Contact request {Folio} of type {Type} registered.", folio, type);

            return ContactRequestResult.Registered(folio);
        }

        private static bool IsValidEmail(string email)
        {
            // Comparing with the parsed address rejects forms such as "Name <user@example.com>".
            return email.Length <= MaxEmailLength
                && MailAddress.TryCreate(email, out var parsed)
                && parsed.Address == email
                && parsed.Host.Contains('.');
        }
    }
}
