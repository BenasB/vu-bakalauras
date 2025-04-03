using System.Numerics;

namespace Bomberman.Core;

public record GridPosition(int Row, int Column)
{
    public static implicit operator Vector2(GridPosition d) =>
        new(x: d.Column * Constants.TileSize, y: d.Row * Constants.TileSize);

    internal GridPosition[] Neighbours =>
        [
            this with
            {
                Row = Row + 1,
            },
            this with
            {
                Row = Row - 1,
            },
            this with
            {
                Column = Column - 1,
            },
            this with
            {
                Column = Column + 1,
            },
        ];

    internal int ManhattanDistance(GridPosition pos) =>
        Math.Abs(pos.Row - Row) + Math.Abs(pos.Column - Column);
}
