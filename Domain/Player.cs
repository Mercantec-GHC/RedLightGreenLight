namespace Domain
{
    public class Player
    {
        public string Name { get; set; }
        public int Id { get; set; }

        public Player(string? name, int id)
        {
            Name = name ?? "Player " + id;
            Id = id;
        }
        public PlayerState State { get; set; } = PlayerState.Joined;

        public enum PlayerState
        {
            Joined,
            Ready,
            Playing,
            Lost,
            Finished
        }
    }
}
