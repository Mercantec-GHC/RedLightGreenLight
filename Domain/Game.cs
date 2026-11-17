namespace Domain
{
    public class Game
    {
        public List<Player> Players { get; private set; } = new List<Player>();
        public Host? Host { get; set; }
        public int MaxPlayers { get; private set; } = 5;
        public int NextPlayerId = 1; // To assign unique IDs to players

        public GameDifficulty Difficulty { get; set; } = GameDifficulty.Easy;
        public GameState State { get; private set; } = GameState.Waiting;
        public int TimeLimit { get; set; } // in seconds
        public int TimeBetweenStops { get; set; } // in seconds

        public Action OnChanged { get; set; } = delegate { };

        public Game(int maxPlayers = 5, GameDifficulty difficulty = GameDifficulty.Easy, int timeLimit = 300, int timeBetweenStops = 8)
        {
            MaxPlayers = maxPlayers;
            Difficulty = difficulty;
            TimeLimit = timeLimit;
            TimeBetweenStops = timeBetweenStops;
        }
        
        public GameResult AddPlayer(string? name = null)
        {
            if (!State.Equals(GameState.Waiting)) { Console.WriteLine("Cannot join a game that has already started."); return GameResult.GameAlreadyStarted; }
            if (Players.Count >= MaxPlayers) { Console.WriteLine("Maximum number of players reached."); return GameResult.MaxPlayersReached; }
            Players.Add(new Player(name, NextPlayerId++));
            OnChanged?.Invoke();
            return GameResult.Success;
        }

        public GameResult AddHost(int id)
        {
            if (Host != null) { Console.WriteLine("A host is already assigned to this game."); return GameResult.HostAlreadyAssigned; }
            Host = new Host(id);
            OnChanged?.Invoke();
            return GameResult.Success;
        }
        public GameResult RemovePlayer(int id)
        {
            if (!Players.Exists(p => p.Id == id)) { Console.WriteLine("Player not found."); return GameResult.PlayerNotFound; }
            Players.RemoveAll(p => p.Id == id);
            OnChanged?.Invoke();
            return GameResult.Success;
        }

        public async Task<GameResult> Start()
        {
            if (!State.Equals(GameState.Waiting)) { Console.WriteLine("Game has already started."); return GameResult.GameAlreadyStarted; }
            if (Host == null) { Console.WriteLine("Cannot start a game without a host."); return GameResult.NoHost; }
            if (!Players.Any()) { Console.WriteLine("Cannot start a game with no players."); return GameResult.NoPlayers; }
            if (Players.Exists(p => p.State != PlayerState.Ready)) { Console.WriteLine("Cannot start a game with players who are not ready."); return GameResult.PlayersNotReady; }

            Players.ForEach(p => p.State = PlayerState.Playing);

            OnChanged?.Invoke();
            await Loop();
            return End();
        }
        public GameResult End()
        {
            State = GameState.Finished;
            OnChanged();

            if (Players.Any(p => p.State == PlayerState.Won))
            {
                var winner = Players.First(p => p.State == PlayerState.Won);
                Console.WriteLine($"Player {winner.Name} has won the game!");
                return GameResult.PlayerWon;
            }
            else
            {
                Console.WriteLine("No players won the game.");
                return GameResult.AllIsLost;
            }
        }

        public async Task Loop()
        {
            while (State != GameState.Finished)
            {
                // Green Light
                State = GameState.GreenLight;
                OnChanged?.Invoke();

                await Task.Delay(TimeBetweenStops * 1000);

                if (State == GameState.Finished)
                    break;

                // Yellow Light
                State = GameState.YellowLight;
                OnChanged?.Invoke();

                await Task.Delay(2000);

                if (State == GameState.Finished)
                    break;

                // Red Light
                State = GameState.RedLight;
                OnChanged?.Invoke();

                await Task.Delay(TimeBetweenStops * 1000);
            }
        }

        public GameResult PlayerMoved(Player player)
        {
            if (State != GameState.RedLight) return GameResult.Success;
            if (!player.State.Equals(PlayerState.Playing)) { Console.WriteLine("Player is not playing."); return GameResult.PlayerNotPlaying; }

            player.State = PlayerState.Lost;
            Console.WriteLine($"Player {player.Name} has lost the game for moving during Red Light.");
            
            if (Players.All(p => p.State != PlayerState.Playing))
            {
                Console.WriteLine("All players have lost. Ending the game.");
                return End();
            }
            OnChanged?.Invoke();
            return GameResult.PlayerLost;
        }

        public GameResult PlayerWon(Player player)
        {
            if (!player.State.Equals(PlayerState.Playing)) { Console.WriteLine("Player is not playing."); return GameResult.PlayerNotPlaying; }
            player.State = PlayerState.Won;
            Console.WriteLine($"Player {player.Name} has won the game.");
            
            return End();
        }
    }
}
