namespace app_curso_claude.Services
{
    /// <summary>
    /// Outcome of sending a contact request: the folio it received, or why it was rejected.
    /// </summary>
    /// <param name="Folio">Folio of the registered request; null on failure.</param>
    /// <param name="Success">True when the request was registered.</param>
    /// <param name="Errors">Messages that explain a failure; empty on success.</param>
    public record ContactRequestResult(string? Folio, bool Success, IReadOnlyList<string> Errors)
        : OperationResult(Success, Errors)
    {
        /// <summary>
        /// Creates a successful result for the request registered with the given folio.
        /// </summary>
        /// <param name="folio">Folio of the registered request.</param>
        public static ContactRequestResult Registered(string folio)
        {
            return new ContactRequestResult(folio, true, []);
        }

        /// <summary>
        /// Creates a failed result with the given error messages.
        /// </summary>
        /// <param name="errors">Messages that explain the failure.</param>
        public static ContactRequestResult Rejected(IEnumerable<string> errors)
        {
            return new ContactRequestResult(null, false, errors.ToArray());
        }
    }
}
