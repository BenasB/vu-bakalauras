namespace Bomberman.Core.Tiles;

public class ExplosionTile(GridPosition position, TileMap tileMap, TimeSpan destroyAfter)
    : Tile(position),
        IUpdatable,
        IEnterable
{
    private TimeSpan _existingTime = TimeSpan.Zero;

    public bool Destroyed { get; private set; }

    public void Update(TimeSpan deltaTime)
    {
        _existingTime += deltaTime;

        if (_existingTime >= destroyAfter)
        {
            tileMap.RemoveTile(this);
            Destroyed = true;
        }
    }

    public void OnEntered(object entree)
    {
        if (entree is not IDamageable damageable)
            return;

        damageable.TakeDamage();
    }

    public override Tile Clone(TileMap newTileMap) =>
        new ExplosionTile(Position, newTileMap, destroyAfter) { _existingTime = _existingTime };
}
