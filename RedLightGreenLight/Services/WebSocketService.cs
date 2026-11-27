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
        private const string BroadcastTopic = "server/all/events";
        private readonly ILogger<WebSocketService> _logger;
        private readonly string _host;
        private readonly int _port;

        public event Func<WebSocketEvent, Task>? OnSignalR;

        public WebSocketService(IConfiguration configuration, ILogger<WebSocketService> logger)
        {
            _logger = logger;

            _host = configuration["MQTT:Host"] ?? "localhost";
            _port = configuration.GetValue("MQTT:Port", 1883);

            _mqttClient = new MqttClientFactory().CreateMqttClient();

            _mqttClient.DisconnectedAsync += e =>
            {
                if (e.ClientWasConnected)
                {
                    _logger.LogWarning($"WebSocketService lost the MQTT connection to {_host}:{_port}. Reason: {e.Reason}.");
                }

                return Task.CompletedTask;
            };

            _mqttOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(_host, _port)
                .WithClientId($"RedLightGreenLight-{Guid.NewGuid()}")
                .Build();

            _logger.LogInformation($"WebSocketService configured for MQTT broker {_host}:{_port}.");
        }

        public Task SendToDeviceByIdAsync(string deviceId, WebSocketEvent webSocketEvent)
        {
            _logger.LogInformation($"Sending {webSocketEvent.Event} to device {deviceId}.");

            return PublishAsync($"server/{deviceId}/events", webSocketEvent);
        }

        public async Task SendToEveryoneAsync(WebSocketEvent webSocketEvent)
        {
            _logger.LogInformation($"Sending {webSocketEvent.Event} to everyone (all devices and the web app).");

            await PublishAsync(BroadcastTopic, webSocketEvent);

            await SendToWebAppAsync(webSocketEvent);
        }

        public async Task SendToWebAppAsync(WebSocketEvent webSocketEvent)
        {
            var handlers = OnSignalR;

            if (handlers is null)
            {
                _logger.LogInformation($"No web app pages are listening. {webSocketEvent.Event} was not delivered to the web app.");

                return;
            }

            Delegate[] subscribers = handlers.GetInvocationList();

            _logger.LogInformation($"Sending {webSocketEvent.Event} to {subscribers.Length} web app page(s).");

            foreach (Func<WebSocketEvent, Task> handler in subscribers)
            {
                try
                {
                    await handler(webSocketEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"SignalR subscriber failed while handling {webSocketEvent.Event}.");
                }
            }
        }

        private async Task PublishAsync(string topic, WebSocketEvent webSocketEvent)
        {
            try
            {
                await EnsureConnected();

                string payload = JsonSerializer.Serialize(webSocketEvent, JsonOptions);

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .Build();

                await _mqttClient.PublishAsync(message);

                _logger.LogInformation($"Published to {topic}: {payload}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to publish {webSocketEvent.Event} to {topic}.");

                throw;
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
                    _logger.LogInformation($"WebSocketService connecting to MQTT broker {_host}:{_port}...");

                    await _mqttClient.ConnectAsync(_mqttOptions);

                    _logger.LogInformation($"WebSocketService connected to MQTT broker {_host}:{_port}.");
                }
            }
            finally
            {
                _connectLock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            _logger.LogInformation("WebSocketService is shutting down.");

            if (_mqttClient.IsConnected)
            {
                await _mqttClient.DisconnectAsync();
            }

            _mqttClient.Dispose();

            _connectLock.Dispose();
        }
    }
}