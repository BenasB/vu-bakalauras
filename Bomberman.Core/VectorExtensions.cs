using System.Numerics;

namespace Bomberman.Core;

public static class VectorExtensions
{
    public static GridPosition ToGridPosition(this Vector2 position) =>
        new(
            (int)Math.Round(position.Y / Constants.TileSize),
            (int)Math.Round(position.X / Constants.TileSize)
        );
}
