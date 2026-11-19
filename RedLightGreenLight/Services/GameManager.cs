using Domain;

namespace RedLightGreenLight.Services
{
    public class GameManager
    {
        public List<Player> Players { get; private set; } = new List<Player>();
        public Goal? Host { get; set; }
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

        public void AddPlayer(string id,string? name = null)
        {
            if (!State.Equals(GameState.Waiting)) { Console.WriteLine("Cannot join a game that has already started."); return; }
            if (Players.Count >= MaxPlayers) { Console.WriteLine("Maximum number of players reached."); return; }
            Players.Add(new Player(name, id));
        }

        public void AddHost(string id)
        {
            if (Host is not null) { Console.WriteLine("A host is already assigned to this game."); return; }
            Host = new Goal(id);
        }

        public void RemovePlayer(string playerId)
        {
            if (!Players.Exists(p => p.Id == playerId)) { Console.WriteLine("Player not found."); return; }
            Players.RemoveAll(p => p.Id == playerId);
        }

        public async Task Start()
        {
            if (!State.Equals(GameState.Waiting)) { Console.WriteLine("Game has already started."); return; }
            if (Host is null) { Console.WriteLine("Cannot start a game without a host."); return; }
            if (!Players.Any()) { Console.WriteLine("Cannot start a game with no players."); return; }
            if (Players.Exists(p => p.State != PlayerState.Ready)) { Console.WriteLine("Cannot start a game with players who are not ready."); return; }

            Players.ForEach(p => p.State = PlayerState.Playing);

            // Start the game loop asynchronously
        }
        public async Task End()
        {
            State = GameState.Finished;

            if (Players.Any(p => p.State == PlayerState.Won))
            {
                var winner = Players.First(p => p.State == PlayerState.Won);
                Console.WriteLine($"Player {winner.Name} has won the game!");
                // Additional logic for handling the winner can be added here
                await _webSocketService.SendToAllAsync(new WebSocketEvent(GameEvent.PlayerWon, winner.Id));
                return;
            }
            else
            {
                Console.WriteLine("No players won the game.");
                await _webSocketService.SendToAllAsync(new WebSocketEvent(GameEvent.GameOver));
                return;
            }
        }

        public async Task PlayerMoved(string playerId) // Needs timestamp
        {
            var player = Players.FirstOrDefault(p => p.Id == playerId);
            if (player is null) { Console.WriteLine("Player not found."); return; }

            if (State != GameState.RedLight) return;
            if (!player.State.Equals(PlayerState.Playing)) { Console.WriteLine("Player is not playing."); return; }

            player.State = PlayerState.Lost;
            Console.WriteLine($"Player {player.Name} has lost the game for moving during Red Light.");

            if (Players.All(p => p.State != PlayerState.Playing))
            {
                Console.WriteLine("All players have lost. Ending the game.");
                await End();
                return; 
            }
            await _webSocketService.SendMosquittoAsync(new WebSocketEvent(GameEvent.PlayerEliminated, playerId));
        }

        public async Task PlayerWon(string playerId)
        {
            var player = Players.FirstOrDefault(p => p.Id == playerId);
            if (player is null) { Console.WriteLine("Player not found."); return; }
            if (!player.State.Equals(PlayerState.Playing)) { Console.WriteLine("Player is not playing."); return; }
            player.State = PlayerState.Won;
            Console.WriteLine($"Player {player.Name} has won the game.");

            await End();
        }
    }
}
