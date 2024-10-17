using System.Numerics;

namespace Bomberman.Core;

public record GridPosition(int Row, int Column)
{
    public static implicit operator Vector2(GridPosition d) =>
        new(x: d.Column * Constants.TileSize, y: d.Row * Constants.TileSize);
}
