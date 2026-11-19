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
        private const string BroadcastTopic = "device/all/events";

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

        public Task SendToDeviceByIdAsync(string deviceId, WebSocketEvent webSocketEvent)
        {
            return PublishAsync($"device/{deviceId}/events", webSocketEvent);
        }

        public async Task SendToEveryoneAsync(WebSocketEvent webSocketEvent)
        {
            await PublishAsync(BroadcastTopic, webSocketEvent);

            await SendToWebAppAsync(webSocketEvent);
        }

        public async Task SendToWebAppAsync(WebSocketEvent webSocketEvent)
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

        private async Task PublishAsync(string topic, WebSocketEvent webSocketEvent)
        {
            await EnsureConnected();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(JsonSerializer.Serialize(webSocketEvent, JsonOptions))
                .Build();

            await _mqttClient.PublishAsync(message);
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
