namespace Bomberman.Core;

public class GameState : IUpdatable
{
    private static readonly TimeSpan ShiftInterval = TimeSpan.FromSeconds(3);
    public Player Player { get; }

    public TileMap TileMap { get; }

    public bool Terminated => !Player.Alive;

    private TimeSpan _shiftElapsed = TimeSpan.Zero;

    public GameState()
    {
        var start = new GridPosition(Row: 5, Column: 7);
        TileMap = new TileMap(17, 9).WithDefaultTileLayout(start);
        Player = new Player(start, TileMap);
    }

    public GameState(GameState original)
    {
        TileMap = new TileMap(original.TileMap);
        Player = new Player(original.Player, TileMap);
        _shiftElapsed = original._shiftElapsed;
    }

    public void Update(TimeSpan deltaTime)
    {
        Player.Update(deltaTime);
        TileMap.Update(deltaTime);

        _shiftElapsed += deltaTime;

        if (_shiftElapsed < ShiftInterval)
            return;

        _shiftElapsed = TimeSpan.Zero;
        TileMap.Shift();
        Player.Position = Player.Position with { X = Player.Position.X - 1 * Constants.TileSize };
    }
}
