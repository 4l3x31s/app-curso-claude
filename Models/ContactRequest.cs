namespace app_curso_claude.Models
{
    public class ContactRequest
    {
        public int Id { get; set; }

        public required string Folio { get; set; }

        public required string Type { get; set; }

        public string? Sku { get; set; }

        public required string Email { get; set; }

        public required string Message { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
