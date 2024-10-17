namespace Bomberman.Core.Tiles;

public class BombTile(GridPosition position, TileMap tileMap, int range)
    : Tile(position),
        IUpdatable
{
    private static readonly TimeSpan DetonateAfter = TimeSpan.FromSeconds(3);

    private TimeSpan _existingTime = TimeSpan.Zero;

    public void Update(TimeSpan deltaTime)
    {
        _existingTime += deltaTime;

        if (_existingTime >= DetonateAfter)
            Explode();
    }

    /// <summary>
    /// Gets explosion paths in 4 directions based on the bomb range
    /// </summary>
    private IEnumerable<IEnumerable<GridPosition>> ExplosionPaths =>
        new Func<int, GridPosition>[]
        {
            distanceFromCenter => Position with { Row = Position.Row - distanceFromCenter },
            distanceFromCenter => Position with { Row = Position.Row + distanceFromCenter },
            distanceFromCenter => Position with { Column = Position.Column - distanceFromCenter },
            distanceFromCenter => Position with { Column = Position.Column + distanceFromCenter },
        }.Select(explosionPositionCalculationOnSpecificDirection =>
            Enumerable.Range(1, range).Select(explosionPositionCalculationOnSpecificDirection)
        );

    private void Explode()
    {
        tileMap.RemoveTile(this);
        tileMap.PlaceTile(Position, new ExplosionTile(Position, tileMap));

        foreach (var explosionPath in ExplosionPaths)
        {
            foreach (var explosionPosition in explosionPath)
            {
                var tileToExplode = tileMap.GetTile(explosionPosition);

                if (tileToExplode is BombTile bombTile)
                {
                    // Chain reaction
                    bombTile.Explode();
                    break;
                }

                if (tileToExplode is BoxTile boxTile)
                {
                    tileMap.RemoveTile(boxTile);
                    tileMap.PlaceTile(
                        boxTile.Position,
                        new ExplosionTile(boxTile.Position, tileMap)
                    );
                    break;
                }

                if (tileToExplode != null)
                    break;

                tileMap.PlaceTile(explosionPosition, new ExplosionTile(explosionPosition, tileMap));
            }
        }
    }
}
