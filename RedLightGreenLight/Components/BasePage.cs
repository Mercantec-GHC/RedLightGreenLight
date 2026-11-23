using Domain;
using Microsoft.AspNetCore.Components;
using RedLightGreenLight.Services;

namespace RedLightGreenLight.Components
{
    public abstract class BasePage : ComponentBase, IDisposable
    {
        [Inject]
        protected WebSocketService WebSocketService { get; set; } = default!;

        [Inject]
        protected ILoggerFactory LoggerFactory { get; set; } = default!;

        private ILogger? _logger;

        protected ILogger Logger => _logger ??= LoggerFactory.CreateLogger(GetType());

        private bool _subscribed;

        public override Task SetParametersAsync(ParameterView parameters)
        {
            // Subscribing here instead of in OnInitialized means pages can override OnInitialized without calling base.
            if (!_subscribed)
            {
                WebSocketService.OnSignalR += HandleSignalR;

                _subscribed = true;

                Logger.LogInformation($"Page {GetType().Name} opened and is listening for live updates.");
            }

            return base.SetParametersAsync(parameters);
        }

        protected virtual Task OnWebSocketEventAsync(WebSocketEvent webSocketEvent)
        {
            return Task.CompletedTask;
        }

        private Task HandleSignalR(WebSocketEvent webSocketEvent)
        {
            return InvokeAsync(async () =>
            {
                try
                {
                    await OnWebSocketEventAsync(webSocketEvent);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Page {GetType().Name} failed to handle {webSocketEvent.Event}.");

                    throw;
                }

                StateHasChanged();
            });
        }

        public virtual void Dispose()
        {
            if (_subscribed)
            {
                WebSocketService.OnSignalR -= HandleSignalR;

                _subscribed = false;

                Logger.LogInformation($"Page {GetType().Name} closed and stopped listening for live updates.");
            }
        }
    }
}
