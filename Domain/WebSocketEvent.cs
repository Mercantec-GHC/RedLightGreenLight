using System.Text.Json.Serialization;

namespace Domain
{
    public class WebSocketEvent
    {
        public GameEvent Event { get; }
        public DateTime Date { get; }
        public object? Data { get; }

        public WebSocketEvent(GameEvent @event, object? data = null)
        {
            Event = @event;
            Date = DateTime.UtcNow;
            Data = data;
        }
    }
}