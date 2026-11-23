using Domain;
using Microsoft.AspNetCore.Components;
using RedLightGreenLight.Services;

namespace RedLightGreenLight.Components
{
    public abstract class BasePage : ComponentBase, IDisposable
    {
        [Inject]
        protected WebSocketService WebSocketService { get; set; } = default!;

        private bool _subscribed;

        public override Task SetParametersAsync(ParameterView parameters)
        {
            // Subscribing here instead of in OnInitialized means pages can override OnInitialized without calling base.
            if (!_subscribed)
            {
                WebSocketService.OnSignalR += HandleSignalR;

                _subscribed = true;
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
                await OnWebSocketEventAsync(webSocketEvent);

                StateHasChanged();
            });
        }

        public virtual void Dispose()
        {
            if (_subscribed)
            {
                WebSocketService.OnSignalR -= HandleSignalR;

                _subscribed = false;
            }
        }
    }
}
