namespace Bomberman.Desktop;

internal record BombermanGameOptions
{
    public GamePlayer Player { get; set; } = GamePlayer.Agent;

    public bool Export { get; set; } = false;
}

internal enum GamePlayer
{
    Agent,
    Keyboard,
}
