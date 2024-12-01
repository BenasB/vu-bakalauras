namespace Bomberman.Core.Tiles;

public class WallTile(GridPosition position) : Tile(position)
{
    public override Tile Clone(TileMap tileMap) => new WallTile(Position);
}
