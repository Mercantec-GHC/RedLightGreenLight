namespace Domain
{
    public enum MethodResult
    {
        Success,
        NoGameAvailable,
        GameAlreadyStarted,
        MaxPlayersReached,
        HostAlreadyAssigned,
        PlayerNotFound,
        NoPlayers,
        NoHost,
        PlayersNotReady,
        PlayerNotPlaying,
        PlayerWon,
        PlayerLost,
        GameOver
    }
}
