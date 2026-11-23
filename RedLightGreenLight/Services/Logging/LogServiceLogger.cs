namespace RedLightGreenLight.Services.Logging
{
    public class LogServiceLogger : ILogger
    {
        private readonly string _category;
        private readonly LogService _logService;

        public LogServiceLogger(string category, LogService logService)
        {
            _category = category;
            _logService = logService;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string message = formatter(state, exception);

            if (exception is not null)
            {
                message += $" ({exception.Message})";
            }

            _logService.Add(new LogEntry(DateTime.UtcNow, logLevel, _category, message));
        }
    }
}