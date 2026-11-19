using Domain;

namespace RedLightGreenLight.Services
{
    public class GameManager
    {
        public List<Player> Players { get; private set; } = new List<Player>();
        public Goal? Goal { get; set; }

        public int MaxPlayers { get; private set; } = 5;
        public GameDifficulty Difficulty { get; set; } = GameDifficulty.Easy;

        public GameState State { get; private set; } = GameState.Waiting;
        public int TimeLimit { get; set; } // in seconds
        public int TimeBetweenStops { get; set; } // in seconds

        private readonly WebSocketService _webSocketService;

        public GameManager(WebSocketService webSocketService)
        {
            _webSocketService = webSocketService;
        }

        #region Player Management

        public void AddPlayer(string id)
        {
            if (IsGameStarted())
            {
                Console.WriteLine("Cannot join a game that has already started.");

                return;
            }

            if (Players.Count >= MaxPlayers)
            {
                Console.WriteLine("Maximum number of players reached.");

                return;
            }

            Players.Add(new Player(id));
        }

        public void ConvertPlayerToGoal(string id)
        {
            var player = Players.FirstOrDefault(p => p.Id == id);

            if (player is null)
            {
                Console.WriteLine("Goal must be a player in the game.");

                return;
            }

            Players.Remove(player);

            Goal = new Goal(id);

            _webSocketService.SendToDeviceByIdAsync(id, new WebSocketEvent(GameEvent.PlayerIsGoal));
        }

        public async Task MarkPlayerAsReadyAsync(string playerId)
        {
            var player = Players.FirstOrDefault(p => p.Id == playerId);
            
            if (player is null)
            {
                Console.WriteLine("Player not found.");
            
                return;
            }
         
            player.State = PlayerState.Ready;

            if (Players.All(p => p.State == PlayerState.Ready))
            {
                Console.WriteLine("All players are ready. Starting the game...");
             
                await StartGameAsync();
            }
        }

        public void RemovePlayer(string playerId)
        {
            if (!Players.Exists(p => p.Id == playerId))
            {
                Console.WriteLine("Player not found.");

                return;
            }

            Players.RemoveAll(p => p.Id == playerId);
        }

        #endregion

        #region Game Management and Actions

        public async Task StartGameAsync()
        {
            if (Goal is null)
            {
                Console.WriteLine("Cannot start a game without a goal.");

                return;
            }

            if (Players.Count < 2)
            {
                Console.WriteLine("Cannot start a game with less than 2 players.");

                return;
            }

            if (Players.Exists(p => p.State != PlayerState.Ready))
            {
                Console.WriteLine("Cannot start a game with players who are not ready.");

                return;
            }

            if (IsGameStarted())
            {
                Console.WriteLine("Game has already started.");

                return;
            }

            Players.ForEach(p => p.State = PlayerState.Playing);

            State = GameState.GreenLight;

            await _webSocketService.SendToEveryoneAsync(new WebSocketEvent(GameEvent.GameStarted));
        }

        public bool IsGameStarted()
        {
            return State != GameState.Waiting && State != GameState.Finished;
        }

        public async Task EndGameAsync()
        {
            State = GameState.Finished;

            if (Players.Any(p => p.State == PlayerState.Won))
            {
                var winner = Players.First(p => p.State == PlayerState.Won);

                Players.Where(p => p.Id != winner.Id).ToList().ForEach(p => p.State = PlayerState.Lost);

                Console.WriteLine($"Player {winner.Name} has won the game!");

                await _webSocketService.SendToEveryoneAsync(new WebSocketEvent(GameEvent.PlayerWon, winner));

                return;
            }
            else
            {
                Console.WriteLine("No players won the game.");

                await _webSocketService.SendToEveryoneAsync(new WebSocketEvent(GameEvent.GameOver));

                return;
            }
        }

        public async Task PlayerMovedAsync(string playerId) 
        {
            var player = Players.FirstOrDefault(p => p.Id == playerId);

            if (player is null)
            {
                Console.WriteLine("Player not found.");

                return;
            }

            if (!player.State.Equals(PlayerState.Playing))
            {
                Console.WriteLine("Player is not playing.");

                return;
            }

            Console.WriteLine($"Player {player.Name} has moved.");
        }

        public async Task EliminatePlayerAsync(string playerId)
        {
            var player = Players.FirstOrDefault(p => p.Id == playerId);

            if (player is null)
            {
                Console.WriteLine("Player not found.");

                return;
            }

            if (!player.State.Equals(PlayerState.Playing))
            {
                Console.WriteLine("Player is not playing.");
                
                return;
            }

            player.State = PlayerState.Lost;

            Console.WriteLine($"Player {player.Name} has been eliminated from the game.");

            if (Players.All(p => p.State == PlayerState.Lost))
            {
                Console.WriteLine("All players have lost. Ending the game.");

                await EndGameAsync();
                
                return;
            }

            await _webSocketService.SendToDeviceByIdAsync(playerId, new WebSocketEvent(GameEvent.PlayerEliminated));
        }

        public async Task PlayerWon(string playerId)
        {
            var player = Players.FirstOrDefault(p => p.Id == playerId);

            if (player is null)
            {
                Console.WriteLine("Player not found.");

                return;
            }

            if (!player.State.Equals(PlayerState.Playing))
            {
                Console.WriteLine("Player is not playing.");

                return;
            }

            player.State = PlayerState.Won;

            await EndGameAsync();
        }

        #endregion
    }
}
