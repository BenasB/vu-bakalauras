namespace Bomberman.Desktop;

internal record BombermanGameOptions
{
    public PlayerType PlayerOne { get; set; } = PlayerType.Walking;
    public PlayerType PlayerTwo { get; set; } = PlayerType.Walking;

    public bool Export { get; set; } = false;
}

internal enum PlayerType
{
    Static,
    Walking,
    Mcts,
    Keyboard,
}
