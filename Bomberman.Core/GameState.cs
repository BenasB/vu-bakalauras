namespace Bomberman.Core;

public class GameState : IUpdatable
{
    public Player Player { get; }

    public TileMap TileMap { get; }

    public bool Terminated => !Player.Alive;

    public GameState()
    {
        var start = new GridPosition(Row: 3, Column: 3);
        TileMap = new TileMap(17, 9).WithDefaultTileLayout(start);
        Player = new Player(start, TileMap);
    }

    public GameState(GameState original)
    {
        TileMap = new TileMap(original.TileMap);
        Player = new Player(original.Player, TileMap);
    }

    public void Update(TimeSpan deltaTime)
    {
        Player.Update(deltaTime);
        TileMap.Update(deltaTime);
    }
}
