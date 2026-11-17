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
            OnChanged();
            return GameResult.Success;
        }

        public GameResult AddHost(int id)
        {
            if (Host != null) { Console.WriteLine("A host is already assigned to this game."); return GameResult.HostAlreadyAssigned; }
            Host = new Host(id);
            OnChanged();
            return GameResult.Success;
        }
        public GameResult RemovePlayer(int id)
        {
            if (!Players.Exists(p => p.Id == id)) { Console.WriteLine("Player not found."); return GameResult.PlayerNotFound; }
            Players.RemoveAll(p => p.Id == id);
            OnChanged();
            return GameResult.Success;
        }

        public GameResult Start()
        {
            if (Host == null) { Console.WriteLine("Cannot start a game without a host."); return GameResult.NoHost; }
            if (!Players.Any()) { Console.WriteLine("Cannot start a game with no players."); return GameResult.NoPlayers; }
            if (Players.Exists(p => p.State != Player.PlayerState.Ready)) { Console.WriteLine("Cannot start a game with players who are not ready."); return GameResult.PlayersNotReady; }

            Players.ForEach(p => p.State = Player.PlayerState.Playing);
            State = GameState.GreenLight;


            OnChanged();
            return GameResult.Success;

            // Start the game loop in a separate thread or task
        }
        public void End()
        {
            

            State = GameState.Finished;

            // Anounce winner
            OnChanged();
        }

        public void PlayerMoved(Player player)
        {
            if (State != GameState.RedLight) return;
            if (!player.State.Equals(Player.PlayerState.Playing)) { Console.WriteLine("Player is not playing."); return; }

            player.State = Player.PlayerState.Lost;
            Console.WriteLine($"Player {player.Name} has lost the game for moving during Red Light.");
            OnChanged();
        }
    }
}
