using Domain;

namespace RedLightGreenLight.Services
{
    public class GameHandler : BackgroundService
    {
        private readonly GameManager _gameManager;
        private readonly ILogger<GameHandler> _logger;

        public GameHandler(GameManager gameManager, ILogger<GameHandler> logger)
        {
            _gameManager = gameManager;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(250, stoppingToken);

                    if (!_gameManager.IsGameStarted())
                    {
                        continue;
                    }

                    _logger.LogInformation("Game handler is running the game.");

                    while (_gameManager.IsGameStarted())
                    {
                        await ShowLightAsync(GameState.GreenLight, GetGreenLightDuration(), stoppingToken);

                        await ShowLightAsync(GameState.YellowLight, TimeSpan.FromSeconds(1), stoppingToken);

                        await ShowLightAsync(GameState.RedLight, GetRedLightDuration(), stoppingToken);
                    }

                    _logger.LogInformation("Game handler stopped running the game.");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Game handler failed");
                }
            }
        }

        private async Task ShowLightAsync(GameState light, TimeSpan duration, CancellationToken stoppingToken)
        {
            await _gameManager.ChangeLightAsync(light);

            DateTime lightEndsAt = DateTime.UtcNow + duration;

            while (DateTime.UtcNow < lightEndsAt && _gameManager.IsGameStarted())
            {
                await Task.Delay(100, stoppingToken);
            }
        }

        private TimeSpan GetGreenLightDuration()
        {
            return _gameManager.Difficulty switch
            {
                GameDifficulty.Hard => RandomSeconds(2, 5),
                GameDifficulty.Medium => RandomSeconds(3, 6),
                _ => RandomSeconds(4, 7)
            };
        }

        private TimeSpan GetRedLightDuration()
        {
            return _gameManager.Difficulty switch
            {
                GameDifficulty.Hard => RandomSeconds(3, 6),
                GameDifficulty.Medium => RandomSeconds(3, 5),
                _ => RandomSeconds(2, 4)
            };
        }

        private static TimeSpan RandomSeconds(double min, double max)
        {
            return TimeSpan.FromSeconds(min + Random.Shared.NextDouble() * (max - min));
        }
    }
}