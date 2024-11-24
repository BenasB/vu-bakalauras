namespace Bomberman.Core.Tiles;

public class WallTile(GridPosition position) : Tile(position)
{
    public override object Clone() => new WallTile(Position);
}
