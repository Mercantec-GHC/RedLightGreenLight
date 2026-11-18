namespace Domain
{
    public class Player
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public PlayerState State { get; set; } = PlayerState.Waiting;
        public Player(string? name, int id)
        {
            Name = name ?? "Player " + id;
            Id = id;
        }
    }
}
