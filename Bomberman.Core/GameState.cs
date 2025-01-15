namespace Bomberman.Core;

public class GameState : IUpdatable
{
    public Player Player { get; }

    public TileMap TileMap { get; }

    public bool Terminated => !Player.Alive;

    private TimeSpan _shiftElapsed = TimeSpan.Zero;
    private int _shiftsSoFar = 0;
    private TimeSpan _shiftInterval;

    public GameState()
    {
        var start = new GridPosition(Row: 5, Column: 7);
        TileMap = new TileMap(17, 11).WithDefaultTileLayout(start);
        Player = new Player(start, TileMap);
        _shiftInterval = GetShiftInterval(_shiftsSoFar);
    }

    public GameState(GameState original)
    {
        TileMap = new TileMap(original.TileMap);
        Player = new Player(original.Player, TileMap);
        _shiftElapsed = original._shiftElapsed;
        _shiftsSoFar = original._shiftsSoFar;
        _shiftInterval = original._shiftInterval;
    }

    public void Update(TimeSpan deltaTime)
    {
        if (Terminated)
            return;

        Player.Update(deltaTime);
        TileMap.Update(deltaTime);

        _shiftElapsed += deltaTime;

        if (_shiftElapsed < _shiftInterval)
            return;

        _shiftElapsed = _shiftElapsed - _shiftInterval;
        _shiftsSoFar++;
        _shiftInterval = GetShiftInterval(_shiftsSoFar);
        TileMap.Shift();
        Player.Position = Player.Position with { X = Player.Position.X - 1 * Constants.TileSize };

        // Safety precaution to make sure the player dies
        if (Player.Position.X < 0)
            Player.TakeDamage();

        if (Player.Alive)
            Player.Score += 10;
    }

    private static TimeSpan GetShiftInterval(int shiftsSoFar) =>
        TimeSpan.FromSeconds(Math.Max(2, 4 * Math.Pow(0.96, shiftsSoFar)));
}
