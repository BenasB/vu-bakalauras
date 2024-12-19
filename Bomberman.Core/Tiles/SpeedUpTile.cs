namespace Bomberman.Core.Tiles;

public class SpeedUpTile(GridPosition position, TileMap tileMap) : Tile(position), IEnterable
{
    public void OnEntered(object entree)
    {
        if (entree is not Player player)
            return;

        player.Speed = Math.Min(4, player.Speed + 0.5f);
        player.Score += 100;
        tileMap.RemoveTile(this);
    }

    public override Tile Clone(TileMap newTileMap) => new SpeedUpTile(Position, newTileMap);
}
