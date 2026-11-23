using Domain;

namespace RedLightGreenLight.Services
{
    public class GameManager
    {
        public List<Player> Players { get; private set; } = new List<Player>();
        public Goal? Goal { get; set; }

        public int MaxPlayers { get; private set; } = 5;
        public GameDifficulty Difficulty { get; set; } = GameDifficulty.Easy;

        public GameState State { get; set; } = GameState.Waiting;
        public int TimeLimit { get; set; } // in seconds
        public int TimeBetweenStops { get; set; } // in seconds

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

            Players.Add(new Player(id));

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

            if (Goal is not null)
            {
                _logger.LogInformation($"Replacing the current goal {Goal.Id} with {id}.");
            }

            Players.Remove(player);

            Goal = new Goal(id);

            _logger.LogInformation($"Player {id} is now the goal. Remaining players: {Players.Count}.");

            await _webSocketService.SendToDeviceByIdAsync(id, new WebSocketEvent(GameEvent.PlayerIsGoal));

            await _webSocketService.SendToWebAppAsync(new WebSocketEvent(GameEvent.PlayerIsGoal, id));
        }

        public async Task MarkPlayerAsReadyAsync(string playerId)
        {
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

            if (Players.All(p => p.State == PlayerState.Ready))
            {
                _logger.LogInformation("All players are ready. Starting the game...");
             
                await StartGameAsync();
            }
        }

        public async Task MarkPlayerAsWaitingAsync(string playerId)
        {
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

        public void RemovePlayer(string playerId)
        {
            if (Goal?.Id == playerId)
            {
                _logger.LogWarning($"The goal {playerId} disconnected. The goal is not removed automatically.");

                return;
            }

            if (!Players.Exists(p => p.Id == playerId))
            {
                _logger.LogWarning($"Cannot remove player {playerId} because they were not found.");

                return;
            }

            Players.RemoveAll(p => p.Id == playerId);

            _logger.LogInformation($"Player {playerId} was removed from the game. Players left: {Players.Count}.");

            if (IsGameStarted())
            {
                _logger.LogWarning($"Player {playerId} left while the game was running (state: {State}).");
            }
        }

        #endregion

        #region Game Management and Actions

        public async Task StartGameAsync()
        {
            _logger.LogInformation($"Trying to start the game with {Players.Count} players.");

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

            if (Players.Exists(p => p.State != PlayerState.Ready))
            {
                var notReady = Players.Where(p => p.State != PlayerState.Ready).Select(p => p.Id);

                _logger.LogWarning($"Cannot start the game because these players are not ready: {string.Join(", ", notReady)}.");

                return;
            }

            if (IsGameStarted())
            {
                _logger.LogInformation($"Game has already started (state: {State}).");

                return;
            }

            Players.ForEach(p => p.State = PlayerState.Playing);

            State = GameState.GreenLight;

            _logger.LogInformation($"Game started with {Players.Count} players and goal {Goal.Id}. Difficulty: {Difficulty}, time limit: {TimeLimit}s.");

            await _webSocketService.SendToEveryoneAsync(new WebSocketEvent(GameEvent.GameStarted));
        }

        public bool IsGameStarted()
        {
            return State != GameState.Waiting && State != GameState.Finished;
        }

        public async Task EndGameAsync()
        {
            _logger.LogInformation($"Ending the game (previous state: {State}).");

            State = GameState.Finished;

            if (Players.Any(p => p.State == PlayerState.Won))
            {
                var winner = Players.First(p => p.State == PlayerState.Won);

                Players.Where(p => p.Id != winner.Id).ToList().ForEach(p => p.State = PlayerState.Lost);

                _logger.LogInformation($"Player {winner.Id} has won the game!");

                await _webSocketService.SendToEveryoneAsync(new WebSocketEvent(GameEvent.PlayerWon, winner));

                return;
            }
            else
            {
                _logger.LogInformation("No players won the game.");

                await _webSocketService.SendToEveryoneAsync(new WebSocketEvent(GameEvent.GameOver));

                return;
            }
        }

        public async Task PlayerMovedAsync(string playerId) 
        {
            var player = Players.FirstOrDefault(p => p.Id == playerId);

            if (player is null)
            {
                _logger.LogWarning($"Movement from player {playerId} ignored because they were not found.");

                return;
            }

            if (!player.State.Equals(PlayerState.Playing))
            {
                _logger.LogWarning($"Movement from player {playerId} ignored because they are not playing (state: {player.State}).");

                return;
            }

            _logger.LogInformation($"Player {playerId} moved during {State}.");
        }

        public async Task EliminatePlayerAsync(string playerId)
        {
            var player = Players.FirstOrDefault(p => p.Id == playerId);

            if (player is null)
            {
                _logger.LogWarning($"Cannot eliminate player {playerId} because they were not found.");

                return;
            }

            if (!player.State.Equals(PlayerState.Playing))
            {
                _logger.LogWarning($"Cannot eliminate player {playerId} because they are not playing (state: {player.State}).");
                
                return;
            }

            player.State = PlayerState.Lost;

            int remaining = Players.Count(p => p.State == PlayerState.Playing);

            _logger.LogInformation($"Player {playerId} has been eliminated. Players still playing: {remaining}.");

            if (Players.All(p => p.State == PlayerState.Lost))
            {
                _logger.LogInformation("All players have lost. Ending the game.");

                await EndGameAsync();
                
                return;
            }

            await _webSocketService.SendToDeviceByIdAsync(playerId, new WebSocketEvent(GameEvent.PlayerEliminated));

            await _webSocketService.SendToWebAppAsync(new WebSocketEvent(GameEvent.PlayerEliminated, playerId));
        }

        public async Task PlayerWon(string playerId)
        {
            var player = Players.FirstOrDefault(p => p.Id == playerId);

            if (player is null)
            {
                _logger.LogWarning($"Player {playerId} cannot win because they were not found.");

                return;
            }

            if (!player.State.Equals(PlayerState.Playing))
            {
                _logger.LogWarning($"Player {playerId} cannot win because they are not playing (state: {player.State}).");

                return;
            }

            player.State = PlayerState.Won;

            _logger.LogInformation($"Player {playerId} reached the goal.");

            await EndGameAsync();
        }

        #endregion

        public void Reset()
        {
            _logger.LogInformation($"Resetting the game. Removing {Players.Count} players and the goal.");

            Players.Clear();
            Goal = null;

            MaxPlayers = 5;
            Difficulty = GameDifficulty.Easy;

            State = GameState.Waiting;
            TimeLimit = 0;
            TimeBetweenStops = 0;

            _logger.LogInformation("Game has been reset.");
        }
    }
}