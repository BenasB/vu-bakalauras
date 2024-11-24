namespace Bomberman.Core.Tiles;

public class ExplosionTile(GridPosition position, TileMap tileMap, TimeSpan destroyAfter)
    : Tile(position),
        IUpdatable,
        IEnterable
{
    private TimeSpan _existingTime = TimeSpan.Zero;

    public void Update(TimeSpan deltaTime)
    {
        _existingTime += deltaTime;

        if (_existingTime >= destroyAfter)
            tileMap.RemoveTile(this);
    }

    public void OnEntered(object entree)
    {
        if (entree is not IDamageable damageable)
            return;

        damageable.TakeDamage();
    }

    public override object Clone() =>
        new ExplosionTile(Position, tileMap, destroyAfter) { _existingTime = _existingTime };
}
