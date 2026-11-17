using System;
using System.Collections.Generic;
using System.Text;

namespace Domain
{
    public enum GameResult
    {
        Success,
        NoGameAvailable,
        GameAlreadyStarted,
        MaxPlayersReached,
        HostAlreadyAssigned,
        PlayerNotFound,
        NoPlayers,
        NoHost,
        PlayersNotReady
    }
}
