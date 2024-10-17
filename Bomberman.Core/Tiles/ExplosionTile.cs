namespace Bomberman.Core.Tiles;

public class ExplosionTile(GridPosition position, TileMap tileMap)
    : Tile(position),
        IUpdatable,
        IEnterable
{
    private static readonly TimeSpan DestroyAfter = TimeSpan.FromSeconds(0.25);
    private TimeSpan _existingTime = TimeSpan.Zero;

    public void Update(TimeSpan deltaTime)
    {
        _existingTime += deltaTime;

        if (_existingTime >= DestroyAfter)
            tileMap.RemoveTile(this);
    }

    public void OnEntered(object entree)
    {
        if (entree is not IDamageable damageable)
            return;

        damageable.TakeDamage();
    }
}
