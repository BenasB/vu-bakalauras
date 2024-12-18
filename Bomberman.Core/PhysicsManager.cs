using System.Numerics;
using Bomberman.Core.Tiles;

namespace Bomberman.Core;

internal static class PhysicsManager
{
    public static float Raycast(
        object caller,
        TileMap tileMap,
        Vector2 subjectPosition,
        Vector2 subjectVelocity
    )
    {
        var velocityLength = subjectVelocity.Length();
        float rayLength = 0;
        var depth = 1;
        while (rayLength < velocityLength)
        {
            rayLength += Constants.TileSize;

            var relevantGridPositions = GetRelevantGridPositions(
                subjectPosition,
                subjectVelocity,
                depth
            );

            foreach (var tileGridPosition in relevantGridPositions)
            {
                Tile? tile;
                try
                {
                    tile = tileMap.GetTile(tileGridPosition);
                }
                catch
                {
                    // For some reason we failed to retrieve the tile at that position, tile might be outside tilemap
                    // which is okay, no collision, just move on
                    continue;
                }

                if (tile is null)
                    continue;

                // Skip enterable tiles as they are not solid
                if (tile is IEnterable enterableTile)
                {
                    // If we are still not at the end of the ray make sure it is triggered
                    if (rayLength < velocityLength)
                        enterableTile.OnEntered(caller);

                    continue;
                }

                var tilePosition = (Vector2)tileGridPosition;
                var diff = subjectVelocity switch
                {
                    { X: > 0 } => (subjectPosition.X + Constants.TileSize + rayLength)
                        - tilePosition.X,
                    { X: < 0 } => tilePosition.X
                        - (subjectPosition.X - Constants.TileSize - rayLength),
                    { Y: > 0 } => (subjectPosition.Y + Constants.TileSize + rayLength)
                        - tilePosition.Y,
                    _ => tilePosition.Y - (subjectPosition.Y - Constants.TileSize - rayLength),
                };

                return Math.Min(rayLength - diff, velocityLength);
            }

            depth++;
        }

        return velocityLength;
    }

    private static GridPosition[] GetRelevantGridPositions(
        Vector2 position,
        Vector2 subjectVelocity,
        int depth
    ) =>
        subjectVelocity switch
        {
            { X: > 0 } =>
            [
                new GridPosition(
                    (int)Math.Floor(position.Y / Constants.TileSize),
                    (int)Math.Ceiling(position.X / Constants.TileSize) + depth
                ),
                new GridPosition(
                    (int)Math.Ceiling(position.Y / Constants.TileSize),
                    (int)Math.Ceiling(position.X / Constants.TileSize) + depth
                ),
            ],
            { X: < 0 } =>
            [
                new GridPosition(
                    (int)Math.Floor(position.Y / Constants.TileSize),
                    (int)Math.Floor(position.X / Constants.TileSize) - depth
                ),
                new GridPosition(
                    (int)Math.Ceiling(position.Y / Constants.TileSize),
                    (int)Math.Floor(position.X / Constants.TileSize) - depth
                ),
            ],
            { Y: > 0 } =>
            [
                new GridPosition(
                    (int)Math.Ceiling(position.Y / Constants.TileSize) + depth,
                    (int)Math.Floor(position.X / Constants.TileSize)
                ),
                new GridPosition(
                    (int)Math.Ceiling(position.Y / Constants.TileSize) + depth,
                    (int)Math.Ceiling(position.X / Constants.TileSize)
                ),
            ],
            _ =>
            [
                new GridPosition(
                    (int)Math.Floor(position.Y / Constants.TileSize) - depth,
                    (int)Math.Floor(position.X / Constants.TileSize)
                ),
                new GridPosition(
                    (int)Math.Floor(position.Y / Constants.TileSize) - depth,
                    (int)Math.Ceiling(position.X / Constants.TileSize)
                ),
            ],
        };
}
