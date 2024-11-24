namespace Bomberman.Core.Tiles;

public class FloorTile(GridPosition position) : Tile(position)
{
    public override object Clone() => new FloorTile(Position);
}
