namespace Domain
{
    public class WebSocketEvent
    {
        public GameEvent Event { get; }
        public DateTime Date { get; }
        public object? Data { get; }

        public WebSocketEvent(GameEvent _event, object? data = null)
        {
            Event = _event;
            Date = DateTime.UtcNow;
            Data = data;
        }
    }
}
