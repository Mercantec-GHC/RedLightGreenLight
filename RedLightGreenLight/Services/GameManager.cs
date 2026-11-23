using Domain;

namespace RedLightGreenLight.Services
{
    public class GameManager
    {
        private List<Player> players { get; set; } = new List<Player>();
        public IReadOnlyList<Player> Players => players.AsReadOnly();

        public Goal? Goal { get; private set; }

        public int MaxPlayers { get; set; } = 5;
        public GameDifficulty Difficulty { get; set; } = GameDifficulty.Easy;

        public GameState State { get; set; } = GameState.Waiting;

        private readonly WebSocketService _webSocketService;
        private readonly ILogger<GameManager> _logger;

        public GameManager(WebSocketService webSocketService, ILogger<GameManager> logger)
        {
            _webSocketService = webSocketService;
            _logger = logger;

            _logger.LogInformation($"Game manager created. Max players: {MaxPlayers}, difficulty: {Difficulty}.");
        }

        #region Player Management

        public void AddPlayer(string id)
        {
            _logger.LogInformation($"Player {id} is trying to join the game.");

            if (Goal?.Id == id || players.Exists(p => p.Id == id))
            {
                _logger.LogInformation($"Device {id} is already in the game.");

                return;
            }

            if (IsGameStarted())
            {
                _logger.LogWarning($"Player {id} cannot join because the game has already started (state: {State}).");

                return;
            }

            if (Players.Count >= MaxPlayers)
            {
                _logger.LogWarning($"Player {id} cannot join because the maximum of {MaxPlayers} players is reached.");

                return;
            }

            players.Add(new Player(id));

            _logger.LogInformation($"Player {id} joined the game. Players: {Players.Count}/{MaxPlayers}.");
        }

        public async Task ConvertPlayerToGoalAsync(string id)
        {
            _logger.LogInformation($"Converting player {id} to the goal.");

            var player = Players.FirstOrDefault(p => p.Id == id);

            if (player is null)
            {
                _logger.LogWarning($"Player {id} cannot be the goal because they are not in the game.");

                return;
            }

            players.Remove(player);

            Goal = new Goal(id);

            _logger.LogInformation($"Player {id} is now the goal. Remaining players: {Players.Count}.");

            await _webSocketService.SendToDeviceByIdAsync(id, new WebSocketEvent(GameEvent.PlayerIsGoal));

            await _webSocketService.SendToWebAppAsync(new WebSocketEvent(GameEvent.PlayerIsGoal, id));
        }

        public async Task MarkPlayerAsReadyAsync(string playerId)
        {
            if (IsGameStarted())
            {
                _logger.LogWarning($"Cannot mark player {playerId} as ready because the game is running.");

                return;
            }

            var player = Players.FirstOrDefault(p => p.Id == playerId);

            if (player is null)
            {
                _logger.LogWarning($"Cannot mark player {playerId} as ready because they were not found.");

                return;
            }

            player.State = PlayerState.Ready;

            int readyCount = Players.Count(p => p.State == PlayerState.Ready);

            _logger.LogInformation($"Player {playerId} is ready. Ready players: {readyCount}/{Players.Count}.");

            await _webSocketService.SendToWebAppAsync(new WebSocketEvent(GameEvent.PlayerReady, playerId));

            if (readyCount != Players.Count)
            {
                return;
            }

            _logger.LogInformation("All players are ready. Starting the game...");

            await StartGameAsync();
        }

        public async Task MarkPlayerAsWaitingAsync(string playerId)
        {
            if (IsGameStarted())
            {
                _logger.LogWarning($"Cannot mark player {playerId} as waiting because the game is running.");

                return;
            }

            var player = Players.FirstOrDefault(p => p.Id == playerId);

            if (player is null)
            {
                _logger.LogWarning($"Cannot mark player {playerId} as waiting because they were not found.");

                return;
            }

            player.State = PlayerState.Waiting;

            _logger.LogInformation($"Player {playerId} is no longer ready.");

            await _webSocketService.SendToWebAppAsync(new WebSocketEvent(GameEvent.PlayerWaiting, playerId));
        }

        public async Task RemovePlayerAsync(string playerId)
        {
            if (Goal?.Id == playerId)
            {
                _logger.LogWarning($"The goal {playerId} disconnected. The goal is not removed automatically.");

                return;
            }

            if (players.RemoveAll(p => p.Id == playerId) == 0)
            {
                _logger.LogWarning($"Cannot remove player {playerId} because they were not found.");

                return;
            }

            _logger.LogInformation($"Player {playerId} left the game (state: {State}). Players: {Players.Count}/{MaxPlayers}.");

            await EndGameIfNoPlayersLeftAsync();
        }

        #endregion

        #region Game Management and Actions

        public async Task StartGameAsync()
        {
            _logger.LogInformation($"Trying to start the game with {Players.Count} players.");

            if (IsGameStarted())
            {
                _logger.LogInformation($"Game has already started (state: {State}).");

                return;
            }

            if (Goal is null)
            {
                _logger.LogWarning("Cannot start a game without a goal.");

                return;
            }

            if (Players.Count < 2)
            {
                _logger.LogWarning($"Cannot start a game with less than 2 players (currently {Players.Count}).");

                return;
            }

            var notReady = Players.Where(p => p.State != PlayerState.Ready).Select(p => p.Id).ToList();

            if (notReady.Count > 0)
            {
                _logger.LogWarning($"Cannot start the game because these players are not ready: {string.Join(", ", notReady)}.");

                return;
            }

            players.ForEach(p => p.State = PlayerState.Playing);

            State = GameState.GreenLight;

            _logger.LogInformation($"Game started with {Players.Count} players and goal {Goal.Id}.");

            await _webSocketService.SendToEveryoneAsync(new WebSocketEvent(GameEvent.GameStarted));
        }

        public bool IsGameStarted()
        {
            return State != GameState.Waiting && State != GameState.Finished;
        }

        public async Task ChangeLightAsync(GameState light)
        {
            if (!IsGameStarted())
            {
                return;
            }

            GameEvent lightEvent = light switch
            {
                GameState.GreenLight => GameEvent.GreenLight,
                GameState.YellowLight => GameEvent.YellowLight,
                GameState.RedLight => GameEvent.RedLight,
                _ => throw new ArgumentException($"{light} is not a light.", nameof(light))
            };

            State = light;

            _logger.LogInformation($"Light changed to {light}.");

            await _webSocketService.SendToEveryoneAsync(new WebSocketEvent(lightEvent));
        }

        public async Task EndGameAsync()
        {
            _logger.LogInformation($"Ending the game (previous state: {State}).");

            State = GameState.Finished;

            var winner = Players.FirstOrDefault(p => p.State == PlayerState.Won);

            if (winner is null)
            {
                _logger.LogInformation("No players won the game.");

                await _webSocketService.SendToEveryoneAsync(new WebSocketEvent(GameEvent.GameOver));

                return;
            }

            Players.Where(p => p != winner).ToList().ForEach(p => p.State = PlayerState.Lost);

            _logger.LogInformation($"Player {winner.Id} has won the game!");

            await _webSocketService.SendToEveryoneAsync(new WebSocketEvent(GameEvent.PlayerWon, winner));
        }

        public async Task PlayerMovedAsync(string playerId)
        {
            var player = Players.FirstOrDefault(p => p.Id == playerId);

            if (player?.State != PlayerState.Playing)
            {
                _logger.LogWarning($"Movement from player {playerId} ignored because they are not playing.");

                return;
            }

            _logger.LogInformation($"Player {playerId} moved during {State}.");
        }

        public async Task EliminatePlayerAsync(string playerId)
        {
            var player = Players.FirstOrDefault(p => p.Id == playerId);

            if (player?.State != PlayerState.Playing)
            {
                _logger.LogWarning($"Cannot eliminate player {playerId} because they are not playing.");

                return;
            }

            player.State = PlayerState.Lost;

            _logger.LogInformation($"Player {playerId} has been eliminated.");

            await _webSocketService.SendToDeviceByIdAsync(playerId, new WebSocketEvent(GameEvent.PlayerEliminated));

            await _webSocketService.SendToWebAppAsync(new WebSocketEvent(GameEvent.PlayerEliminated, playerId));

            await EndGameIfNoPlayersLeftAsync();
        }

        public async Task PlayerWon(Player player)
        {
            if (!player.State.Equals(PlayerState.Playing))
            {
                _logger.LogWarning($"Player {player.Id} cannot win because they are not playing (state: {player.State}).");

                return;
            }

            player.State = PlayerState.Won;

            _logger.LogInformation($"Player {player.Id} reached the goal.");

            await EndGameAsync();
        }

        private async Task EndGameIfNoPlayersLeftAsync()
        {
            if (!IsGameStarted() || players.Exists(p => p.State == PlayerState.Playing))
            {
                return;
            }

            _logger.LogInformation("No players are left. Ending the game.");

            await EndGameAsync();
        }

        #endregion

        public void Reset()
        {
            _logger.LogInformation($"Resetting the game. Removing {Players.Count} players and the goal.");

            players.Clear();
            Goal = null;

            MaxPlayers = 5;
            Difficulty = GameDifficulty.Easy;

            State = GameState.Waiting;

            _logger.LogInformation("Game has been reset.");
        }
    }
}