using System;
using System.Collections.Generic;
using System.Text;

namespace Domain
{
    public class Player
    {
        public string Name { get; set; }
        public int Id { get; set; }

        public Player(string? name)
        {
            Name = name ?? "Player";
        }
        public PlayerState State { get; set; }

        public enum PlayerState
        {
            Waiting,
            Playing,
            Lost,
            Finished
        }
    }
}
