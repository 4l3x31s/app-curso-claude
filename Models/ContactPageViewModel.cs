namespace app_curso_claude.Models
{
    public class ContactPageViewModel
    {
        public required ContactDetailsViewModel Details { get; init; }

        public ContactRequestForm Form { get; init; } = new();

        public required IReadOnlyList<string> RequestTypes { get; init; }

        // Folio of the request that was just registered, or null when none was.
        public string? Folio { get; init; }

        public IReadOnlyList<string> Errors { get; init; } = [];
    }
}
