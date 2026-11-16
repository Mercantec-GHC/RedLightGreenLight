using System;
using System.Collections.Generic;
using System.Text;

namespace Domain
{
    public class Game
    {
        public List<Player> Players { get; set; } = new List<Player>();
        public Host Host { get; set; }

        public Action OnChanged { get; set; } = delegate { };

        public void AddPlayer(string? name)
        {
            Players.Add(new Player(name));
            OnChanged();
        }
        public void Start()
        {
            // Game start logic
            OnChanged();
        }
        public void End()
        {

            // Game end logic

            // Anounce winner
            OnChanged();
        }
    }
}
