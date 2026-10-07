namespace app_curso_claude.Models
{
    /// <summary>
    /// Values typed in the contact request form. Every rule is checked by the service, not here.
    /// </summary>
    public class ContactRequestForm
    {
        public string? Type { get; set; }

        public string? Sku { get; set; }

        public string? Email { get; set; }

        public string? Message { get; set; }
    }
}
