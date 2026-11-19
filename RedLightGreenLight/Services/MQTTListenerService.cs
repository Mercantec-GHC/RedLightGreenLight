using System.Text.Json;
using Domain;
using MQTTnet;

namespace RedLightGreenLight.Services
{
    /// <summary>
    /// A background service that listens for messages from the devices on the Mosquitto broker.
    /// Devices publish game events to "devices/{deviceId}/events" and their connection status
    /// ("online"/"offline", with "offline" set as Last Will) to "devices/{deviceId}/status".
    /// </summary>
    public class MQTTListenerService : BackgroundService
    {
        private const string EventsTopic = "devices/+/events";
        private const string StatusTopic = "devices/+/status";

        private readonly string _host;
        private readonly int _port;
        private readonly GameManager _gameManager;

        public MQTTListenerService(IConfiguration configuration, GameManager gameManager)
        {
            _host = configuration["MQTT:Host"] ?? "localhost";
            _port = configuration.GetValue("MQTT:Port", 1883);
            _gameManager = gameManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new MqttClientFactory();

            using var client = factory.CreateMqttClient();

            client.ApplicationMessageReceivedAsync += e =>
            {
                HandleMessage(e.ApplicationMessage.Topic, e.ApplicationMessage.ConvertPayloadToString());

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
                        await client.ConnectAsync(options, stoppingToken);

                        await client.SubscribeAsync(subscribeOptions, stoppingToken);

                        Console.WriteLine($"MQTT listener connected to {_host}:{_port}.");
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"MQTT listener could not connect to {_host}:{_port}: {ex.Message}");
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

            if (client.IsConnected)
            {
                await client.DisconnectAsync();
            }
        }

        private void HandleMessage(string topic, string payload)
        {
            // Topic format: devices/{deviceId}/{type}
            string[] parts = topic.Split('/');

            if (parts.Length != 3)
            {
                return;
            }

            string deviceId = parts[1];

            switch (parts[2])
            {
                case "status":
                    HandleStatus(deviceId, payload.Trim());
                    break;

                case "events":
                    HandleEvent(deviceId, payload);
                    break;
            }
        }

        private void HandleStatus(string deviceId, string status)
        {
            switch (status)
            {
                case "online":
                    // TODO: Call GameManager when a device connects.
                    Console.WriteLine($"Device {deviceId} connected.");
                    break;

                case "offline":
                    // TODO: Call GameManager when a device disconnects.
                    Console.WriteLine($"Device {deviceId} disconnected.");
                    break;

                default:
                    Console.WriteLine($"Device {deviceId} sent unknown status: {status}");
                    break;
            }
        }

        private void HandleEvent(string deviceId, string payload)
        {
            WebSocketEvent? webSocketEvent;

            try
            {
                webSocketEvent = JsonSerializer.Deserialize<WebSocketEvent>(payload, WebSocketService.JsonOptions);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Device {deviceId} sent invalid payload: {payload} ({ex.Message})");

                return;
            }

            if (webSocketEvent is null)
            {
                return;
            }

            switch (webSocketEvent.Event)
            {
                case GameEvent.Goal:
                    // TODO: Call GameManager to register this device as the goal.
                    Console.WriteLine($"Device {deviceId} registered as the goal.");
                    break;

                case GameEvent.PlayerMoved:
                    _gameManager.PlayerMoved(deviceId);
                    break;

                default:
                    // TODO: Call GameManager with the event.
                    Console.WriteLine($"Device {deviceId} sent {webSocketEvent.Event} at {webSocketEvent.Date:O}");
                    break;
            }
        }
    }
}
