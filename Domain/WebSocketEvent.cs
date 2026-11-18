namespace Domain
{
    public record WebSocketEvent(GameEvent Event, DateTime Date, object? Data = null);
}
