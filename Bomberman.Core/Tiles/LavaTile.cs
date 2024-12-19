namespace Bomberman.Core.Tiles;

public class LavaTile(GridPosition position) : Tile(position), IEnterable
{
    public void OnEntered(object entree)
    {
        if (entree is not IDamageable damageable)
            return;

        damageable.TakeDamage();
    }

    public override Tile Clone(TileMap newTileMap) => new LavaTile(Position);
}
