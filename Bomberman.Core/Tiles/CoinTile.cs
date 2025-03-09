namespace Bomberman.Core.Tiles;

public class CoinTile(GridPosition position, TileMap tileMap) : Tile(position), IEnterable
{
    public void OnEntered(object entree)
    {
        if (entree is not Player player)
            return;

        tileMap.RemoveTile(this);
    }

    public override Tile Clone(TileMap newTileMap) => new CoinTile(Position, newTileMap);
}
