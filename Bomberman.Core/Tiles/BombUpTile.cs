namespace Bomberman.Core.Tiles;

public class BombUpTile(GridPosition position, TileMap tileMap) : Tile(position), IEnterable
{
    public void OnEntered(object entree)
    {
        if (entree is not Player player)
            return;

        player.MaxPlacedBombs = Math.Min(3, player.MaxPlacedBombs + 1);
        tileMap.RemoveTile(this);
    }

    public override Tile Clone(TileMap newTileMap) => new BombUpTile(Position, newTileMap);
}
