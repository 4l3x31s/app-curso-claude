namespace app_curso_claude.Models
{
    public class ContactDetailsViewModel
    {
        public required string Email { get; init; }

        public required string Phone { get; init; }

        // A tel: URI allows only ASCII digits and an optional leading "+",
        // so the display formatting (spaces, parentheses, dashes) is stripped.
        public string PhoneHref
        {
            get
            {
                var trimmed = Phone.Trim();
                var digits = new string(trimmed.Where(char.IsAsciiDigit).ToArray());

                return "tel:" + (trimmed.StartsWith('+') ? "+" : string.Empty) + digits;
            }
        }

        public required string BusinessHours { get; init; }
    }
}
