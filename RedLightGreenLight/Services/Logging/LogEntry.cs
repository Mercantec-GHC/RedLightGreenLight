namespace RedLightGreenLight.Services.Logging
{
    public record LogEntry(DateTime Date, LogLevel Level, string Category, string Message);
}