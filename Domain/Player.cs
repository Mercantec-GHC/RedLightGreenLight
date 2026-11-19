namespace Domain
{
    public class Player
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public PlayerState State { get; set; } = PlayerState.Waiting;

        public Player(string id)
        {
            Name = "Player " + id;
            Id = id;
        }
    }
}
