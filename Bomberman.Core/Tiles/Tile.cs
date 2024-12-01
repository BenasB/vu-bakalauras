namespace Bomberman.Core.Tiles;

public abstract class Tile
{
    public GridPosition Position { get; }

    internal Tile(GridPosition position)
    {
        Position = position;
    }

    public abstract Tile Clone(TileMap tileMap);
}
