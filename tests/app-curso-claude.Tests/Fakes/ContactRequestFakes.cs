using app_curso_claude.Data.Repositories;
using app_curso_claude.Models;
using Microsoft.Extensions.Logging;

namespace app_curso_claude.Tests.Fakes
{
    /// <summary>
    /// Keeps the contact requests in a list, so the service tests never touch SQL Server.
    /// </summary>
    public class FakeContactRequestRepository : IContactRequestRepository
    {
        public List<ContactRequest> Requests { get; } = [];

        public Task<int> CountByYearAsync(int year)
        {
            return Task.FromResult(Requests.Count(r => r.CreatedAt.Year == year));
        }

        public Task AddAsync(ContactRequest request)
        {
            Requests.Add(request);
            return Task.CompletedTask;
        }
    }

    // FakeProductRepository and FixedTimeProvider live in their own files in this folder.

    /// <summary>
    /// Records every log entry: its rendered message and the values of its placeholders.
    /// </summary>
    public class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(formatter(state, exception));

            if (state is IEnumerable<KeyValuePair<string, object?>> values)
            {
                Entries.AddRange(values.Select(value => $"{value.Key}={value.Value}"));
            }
        }
    }
}
