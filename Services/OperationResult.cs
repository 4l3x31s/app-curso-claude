namespace app_curso_claude.Services
{
    /// <summary>
    /// Outcome of a business operation: whether it succeeded and, when it did not, why.
    /// </summary>
    /// <param name="Success">True when the operation completed.</param>
    /// <param name="Errors">Messages that explain a failure; empty on success.</param>
    public record OperationResult(bool Success, IReadOnlyList<string> Errors)
    {
        /// <summary>
        /// Creates a successful result without errors.
        /// </summary>
        public static OperationResult Ok()
        {
            return new OperationResult(true, []);
        }

        /// <summary>
        /// Creates a failed result with the given error messages.
        /// </summary>
        /// <param name="errors">Messages that explain the failure.</param>
        public static OperationResult Fail(params string[] errors)
        {
            // Copied so that later changes to the caller's array do not alter the result.
            return new OperationResult(false, errors.ToArray());
        }
    }
}
