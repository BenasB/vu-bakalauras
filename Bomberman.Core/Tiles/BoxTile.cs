namespace Bomberman.Core.Tiles;

public class BoxTile(GridPosition position) : Tile(position)
{
    public override Tile Clone(TileMap tileMap) => new BoxTile(Position);
}
