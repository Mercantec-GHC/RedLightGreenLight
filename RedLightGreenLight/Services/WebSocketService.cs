using System.Text.Json;
using System.Text.Json.Serialization;
using Domain;
using MQTTnet;

namespace RedLightGreenLight.Services
{
    public class WebSocketService : IAsyncDisposable
    {
        internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _mqttOptions;
        private readonly SemaphoreSlim _connectLock = new(1, 1);
        private readonly string _topic = "game/events";

        /// <summary>
        /// Blazor components subscribe to this and call InvokeAsync(StateHasChanged),
        /// which pushes the update to the browser over the existing SignalR circuit.
        /// </summary>
        public event Func<WebSocketEvent, Task>? OnSignalR;

        public WebSocketService(IConfiguration configuration)
        {
            string host = configuration["MQTT:Host"] ?? "localhost";
            int port = configuration.GetValue("MQTT:Port", 1883);

            _mqttClient = new MqttClientFactory().CreateMqttClient();

            _mqttOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(host, port)
                .WithClientId($"RedLightGreenLight-{Guid.NewGuid()}")
                .Build();
        }

        public async Task SendMosquittoAsync(WebSocketEvent webSocketEvent)
        {
            await EnsureConnected();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(_topic)
                .WithPayload(JsonSerializer.Serialize(webSocketEvent, JsonOptions))
                .Build();

            await _mqttClient.PublishAsync(message);
        }

        public async Task SendSignalRAsync(WebSocketEvent webSocketEvent)
        {
            var handlers = OnSignalR;

            if (handlers is null)
            {
                return;
            }

            foreach (Func<WebSocketEvent, Task> handler in handlers.GetInvocationList())
            {
                try
                {
                    await handler(webSocketEvent);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SignalR subscriber failed: {ex.Message}");
                }
            }
        }

        private async Task EnsureConnected()
        {
            if (_mqttClient.IsConnected)
            {
                return;
            }

            await _connectLock.WaitAsync();

            try
            {
                if (!_mqttClient.IsConnected)
                {
                    await _mqttClient.ConnectAsync(_mqttOptions);
                }
            }
            finally
            {
                _connectLock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_mqttClient.IsConnected)
            {
                await _mqttClient.DisconnectAsync();
            }

            _mqttClient.Dispose();

            _connectLock.Dispose();
        }
    }
}
