using System.Text.Json;
using Domain;
using MQTTnet;

namespace RedLightGreenLight.Services
{
    public class MQTTListenerService : BackgroundService
    {
        private const string EventsTopic = "server/+/events";
        private const string StatusTopic = "server/+/status";

        private readonly string _host;
        private readonly int _port;
        private readonly GameManager _gameManager;
        private readonly WebSocketService _webSocketService;
        private readonly ILogger<MQTTListenerService> _logger;

        public MQTTListenerService(IConfiguration configuration, GameManager gameManager, WebSocketService webSocketService, ILogger<MQTTListenerService> logger)
        {
            _host = configuration["MQTT:Host"] ?? "localhost";
            _port = configuration.GetValue("MQTT:Port", 1883);
            _gameManager = gameManager;
            _webSocketService = webSocketService;
            _logger = logger;

            _logger.LogInformation($"MQTT listener configured for broker {_host}:{_port}.");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MQTT listener is starting.");

            var factory = new MqttClientFactory();

            using var client = factory.CreateMqttClient();

            client.ApplicationMessageReceivedAsync += async e =>
            {
                string topic = e.ApplicationMessage.Topic;

                _logger.LogInformation($"MQTT message received on {topic}.");

                try
                {
                    await HandleMessage(topic, e.ApplicationMessage.ConvertPayloadToString());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Unhandled error while handling MQTT message on {topic}.");
                }
            };

            client.DisconnectedAsync += e =>
            {
                if (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning($"MQTT listener lost the connection to {_host}:{_port}. Reason: {e.Reason}.");
                }

                return Task.CompletedTask;
            };

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(_host, _port)
                .WithClientId($"RedLightGreenLight-Listener-{Guid.NewGuid()}")
                .Build();

            var subscribeOptions = factory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(EventsTopic)
                .WithTopicFilter(StatusTopic)
                .Build();

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!client.IsConnected)
                {
                    try
                    {
                        _logger.LogInformation($"MQTT listener connecting to {_host}:{_port}...");

                        await client.ConnectAsync(options, stoppingToken);

                        await client.SubscribeAsync(subscribeOptions, stoppingToken);

                        _logger.LogInformation($"MQTT listener connected to {_host}:{_port} and subscribed to {EventsTopic} and {StatusTopic}.");
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"MQTT listener could not connect to {_host}:{_port}: {ex.Message}. Retrying in 5 seconds.");
                    }
                }

                try
                {
                    await Task.Delay(5000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("MQTT listener is stopping.");

            if (client.IsConnected)
            {
                await client.DisconnectAsync();

                _logger.LogInformation($"MQTT listener disconnected from {_host}:{_port}.");
            }
        }

        private async Task HandleMessage(string topic, string payload)
        {
            string[] parts = topic.Split('/');

            if (parts.Length != 3)
            {
                _logger.LogWarning($"Ignored MQTT message on unexpected topic {topic}.");

                return;
            }

            string deviceId = parts[1];

            switch (parts[2])
            {
                case "status":
                    await HandleStatus(deviceId, payload.Trim());
                    break;

                case "events":
                    await HandleEvent(deviceId, payload);
                    break;

                default:
                    _logger.LogWarning($"Ignored MQTT message from device {deviceId} with unknown type {parts[2]}.");
                    break;
            }
        }

        private async Task HandleStatus(string deviceId, string status)
        {
            _logger.LogInformation($"Device {deviceId} reported status {status}.");

            switch (status)
            {
                case "online":
                    _gameManager.AddPlayer(deviceId);

                    _logger.LogInformation($"Device {deviceId} connected.");

                    await _webSocketService.SendToWebAppAsync(new WebSocketEvent(GameEvent.PlayerJoined, deviceId));

                    break;

                case "offline":
                    await _gameManager.RemovePlayerAsync(deviceId);

                    _logger.LogInformation($"Device {deviceId} disconnected.");

                    await _webSocketService.SendToWebAppAsync(new WebSocketEvent(GameEvent.PlayerLeft, deviceId));

                    break;

                default:
                    _logger.LogWarning($"Device {deviceId} sent unknown status: {status}");

                    break;
            }
        }

        private async Task HandleEvent(string deviceId, string payload)
        {
            WebSocketEvent? webSocketEvent;

            _logger.LogInformation($"Attempting to deserialize event from device {deviceId} with payload: {payload}");

            try
            {
                webSocketEvent = JsonSerializer.Deserialize<WebSocketEvent>(payload, WebSocketService.JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning($"Device {deviceId} sent invalid payload: {payload} ({ex.Message})");

                return;
            }

            if (webSocketEvent is null)
            {
                _logger.LogWarning($"Couldn't deserialize event from device {deviceId} with payload: {payload}");

                return;
            }

            _logger.LogInformation($"Device {deviceId} sent {webSocketEvent.Event}.");

            switch (webSocketEvent.Event)
            {
                case GameEvent.Goal:
                    await _gameManager.ConvertPlayerToGoalAsync(deviceId);

                    _logger.LogInformation($"Device {deviceId} registered as the goal.");
                    
                    break;

                case GameEvent.PlayerReady:
                    await _gameManager.MarkPlayerAsReadyAsync(deviceId);

                    _logger.LogInformation($"Device {deviceId} is ready at {webSocketEvent.Date:O}");

                    break;

                case GameEvent.PlayerWaiting:
                    await _gameManager.MarkPlayerAsWaitingAsync(deviceId);

                    _logger.LogInformation($"Device {deviceId} is waiting at {webSocketEvent.Date:O}");
                    
                    break;

                case GameEvent.PlayerMoved:
                    await _gameManager.PlayerMovedAsync(deviceId);

                    _logger.LogInformation($"Device {deviceId} moved at {webSocketEvent.Date:O}");

                    break;

                case GameEvent.PlayerEliminated:
                    await _gameManager.EliminatePlayerAsync(deviceId);

                    _logger.LogInformation($"Device {deviceId} eliminated at {webSocketEvent.Date:O}");
                    
                    break;

                default:
                    _logger.LogWarning($"Device {deviceId} sent {webSocketEvent.Event}, which the server doesn't handle.");

                    break;
            }
        }
    }
}