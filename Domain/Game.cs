namespace Domain
{
    public class Game
    {
        public List<Player> Players { get; private set; } = new List<Player>();
        public Host? Host { get; set; }
        public int MaxPlayers { get; private set; } = 5;
        public int NextPlayerId = 1; // To assign unique IDs to players

        public Difficulty GameDifficulty { get; set; } = Difficulty.Easy;
        public GameState State { get; private set; } = GameState.Waiting;
        public int TimeLimit { get; set; } // in seconds
        public int TimeBetweenStops { get; set; } // in seconds

        public Action OnChanged { get; set; } = delegate { };

        public Game(int maxPlayers = 5, Difficulty difficulty = Difficulty.Easy, int timeLimit = 300, int timeBetweenStops = 8)
        {
            MaxPlayers = maxPlayers;
            GameDifficulty = difficulty;
            TimeLimit = timeLimit;
            TimeBetweenStops = timeBetweenStops;
        }
        public enum Difficulty
        {
            Easy,
            Medium,
            Hard
        }

        public enum GameState
        {
            GreenLight,
            YellowLight,
            RedLight,
            Waiting,
            Finished
        }
        public void AddPlayer(string? name)
        {
            if (!State.Equals(GameState.Waiting)) { Console.WriteLine("Cannot join a game that has already started."); return; }
            if (Players.Count >= MaxPlayers) { Console.WriteLine("Maximum number of players reached."); return; }
            Players.Add(new Player(name, NextPlayerId++));
            OnChanged();
        }

        public void AddHost(int id)
        {
            if (Host != null) { Console.WriteLine("A host is already assigned to this game."); return; }
            Host = new Host(id);
            OnChanged();
        }
        public void RemovePlayer(int id)
        {
            Players.RemoveAll(p => p.Id == id);
            OnChanged();
        }

        public void Start()
        {
            if (Host == null) { Console.WriteLine("Cannot start a game without a host."); return; }
            if (!Players.Any()) { Console.WriteLine("Cannot start a game with no players."); return; }
            if (Players.Exists(p => p.State != Player.PlayerState.Ready)) { Console.WriteLine("Cannot start a game with players who are not ready."); return; }

            Players.ForEach(p => p.State = Player.PlayerState.Playing);
            State = GameState.GreenLight;


            OnChanged();

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
