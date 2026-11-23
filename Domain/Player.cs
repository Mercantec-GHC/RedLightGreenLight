namespace Domain
{
    public class Player
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public PlayerState State { get; set; } = PlayerState.Waiting;

        public Player(string id)
        {
            Name = NameGenerator.Generate();
            Id = id;
        }
    }
}
