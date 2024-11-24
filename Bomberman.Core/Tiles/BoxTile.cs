namespace Bomberman.Core.Tiles;

public class BoxTile(GridPosition position) : Tile(position)
{
    public override object Clone() => new BoxTile(Position);
}
