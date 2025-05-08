namespace Bomberman.Core.Tiles;

public class BombTile(GridPosition position, TileMap tileMap, int range)
    : Tile(position),
        IUpdatable
{
    public static readonly TimeSpan DetonateAfter = TimeSpan.FromSeconds(3);
    public static readonly TimeSpan ExplosionDuration = TimeSpan.FromSeconds(0.25);

    private TimeSpan _existingTime = TimeSpan.Zero;

    public bool Detonated { get; internal set; }

    public void Update(TimeSpan deltaTime)
    {
        _existingTime += deltaTime;

        if (_existingTime >= DetonateAfter)
            Detonate(_existingTime - DetonateAfter);
    }

    /// <summary>
    /// Gets explosion paths in 4 directions based on the bomb range
    /// </summary>
    internal IEnumerable<IEnumerable<GridPosition>> ExplosionPaths =>
        new Func<int, GridPosition>[]
        {
            distanceFromCenter => Position with { Row = Position.Row - distanceFromCenter },
            distanceFromCenter => Position with { Row = Position.Row + distanceFromCenter },
            distanceFromCenter => Position with { Column = Position.Column - distanceFromCenter },
            distanceFromCenter => Position with { Column = Position.Column + distanceFromCenter },
        }.Select(explosionPositionCalculationOnSpecificDirection =>
            Enumerable.Range(1, range).Select(explosionPositionCalculationOnSpecificDirection)
        );

    private void Detonate(TimeSpan elapsedExplosionTime)
    {
        Detonated = true;

        tileMap.RemoveTile(this);
        var centerExplosionTile = new ExplosionTile(Position, tileMap, ExplosionDuration);
        tileMap.PlaceTile(centerExplosionTile);
        centerExplosionTile.Update(elapsedExplosionTime);

        foreach (var explosionPath in ExplosionPaths)
        {
            foreach (var explosionPosition in explosionPath)
            {
                var tileToExplode = tileMap.GetTile(explosionPosition);

                if (tileToExplode is BombTile bombTile)
                {
                    // Chain reaction
                    bombTile.Detonate(elapsedExplosionTime);
                    break;
                }

                if (tileToExplode is BoxTile)
                {
                    tileMap.RemoveTile(tileToExplode);
                    var replacementExplosionTile = new ExplosionTile(
                        tileToExplode.Position,
                        tileMap,
                        ExplosionDuration
                    );
                    tileMap.PlaceTile(replacementExplosionTile);
                    replacementExplosionTile.Update(elapsedExplosionTime);
                    break;
                }

                // TODO: What if the explosion path intercepts an explosion path from another bomb?
                // Currently this bomb's explosion path will be blocked

                if (tileToExplode != null)
                    break;

                var newExplosionTile = new ExplosionTile(
                    explosionPosition,
                    tileMap,
                    ExplosionDuration
                );
                tileMap.PlaceTile(newExplosionTile);
                newExplosionTile.Update(elapsedExplosionTime);
            }
        }
    }

    public override Tile Clone(TileMap newTileMap) =>
        new BombTile(Position, newTileMap, range)
        {
            _existingTime = _existingTime,
            Detonated = Detonated,
        };
}
