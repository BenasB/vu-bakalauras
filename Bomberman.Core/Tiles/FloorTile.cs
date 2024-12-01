namespace Bomberman.Core.Tiles;

public class FloorTile(GridPosition position) : Tile(position)
{
    public override Tile Clone(TileMap tileMap) => new FloorTile(Position);
}
