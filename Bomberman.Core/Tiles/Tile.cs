namespace Bomberman.Core.Tiles;

public abstract class Tile : ICloneable
{
    public GridPosition Position { get; }

    internal Tile(GridPosition position)
    {
        Position = position;
    }

    public abstract object Clone();
}
