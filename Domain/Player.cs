namespace Domain
{
    public class Player
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public PlayerState State { get; set; } = PlayerState.Waiting;
        public Player(string? name, string id)
        {
            Name = name ?? "Player " + id;
            Id = id;
        }
    }
}
