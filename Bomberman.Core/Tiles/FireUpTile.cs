namespace Bomberman.Core.Tiles;

public class FireUpTile(GridPosition position, TileMap tileMap) : Tile(position), IEnterable
{
    public void OnEntered(object entree)
    {
        if (entree is not Player player)
            return;

        player.BombRange = Math.Min(4, player.BombRange + 1);
        tileMap.RemoveTile(this);
    }

    public override Tile Clone(TileMap newTileMap) => new FireUpTile(Position, newTileMap);
}
