namespace app_curso_claude.Tests.Fakes
{
    /// <summary>
    /// Clock that returns the same instant until the test sets another one.
    /// </summary>
    public class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
