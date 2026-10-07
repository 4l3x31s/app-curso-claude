namespace app_curso_claude.Tests.Fakes
{
    /// <summary>
    /// Clock that always returns the same instant.
    /// </summary>
    public class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
