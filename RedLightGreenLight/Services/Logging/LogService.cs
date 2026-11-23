using System.Collections.Concurrent;

namespace RedLightGreenLight.Services.Logging
{
    public class LogService : ILoggerProvider
    {
        private readonly ConcurrentQueue<LogEntry> _entries = new();

        public event Action<LogEntry>? OnLog;

        public IReadOnlyList<LogEntry> Entries => _entries.ToList();

        public void Add(LogEntry entry)
        {
            _entries.Enqueue(entry);

            OnLog?.Invoke(entry);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new LogServiceLogger(categoryName, this);
        }

        public void Dispose()
        {
        }
    }
}