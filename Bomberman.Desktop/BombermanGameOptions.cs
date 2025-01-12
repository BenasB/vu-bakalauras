namespace Bomberman.Desktop;

internal record BombermanGameOptions
{
    public GamePlayer Player { get; set; } = GamePlayer.Agent;
}

internal enum GamePlayer
{
    Agent,
    Keyboard
}
