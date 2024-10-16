using System.Collections.Immutable;
using System.Numerics;

namespace Bomberman.Core;

public class TileMap
{
    public int Length { get; }
    public int Width { get; }

    private readonly bool[][] _backgroundTiles;
    private readonly bool[][] _foregroundTiles;

    public ImmutableArray<ImmutableArray<bool>> BackgroundTiles =>
        [.. _backgroundTiles.Select(row => row.ToImmutableArray())];

    public ImmutableArray<ImmutableArray<bool>> ForegroundTiles =>
        [.. _foregroundTiles.Select(row => row.ToImmutableArray())];

    public TileMap(int length, int width)
    {
        Length = length;
        Width = width;

        _backgroundTiles = Enumerable
            .Range(0, width)
            .Select(_ => Enumerable.Range(0, length).Select(_ => true).ToArray())
            .ToArray();

        _foregroundTiles = Enumerable.Range(0, width).Select(_ => new bool[length]).ToArray();
        _foregroundTiles[0] = Enumerable.Range(0, length).Select(_ => true).ToArray();
        _foregroundTiles[width - 1] = Enumerable.Range(0, length).Select(_ => true).ToArray();
        foreach (var row in _foregroundTiles)
            row[0] = row[length - 1] = true;

        for (int row = 2; row < Width - 1; row += 2)
        {
            for (int column = 2; column < Length - 1; column += 2)
            {
                _foregroundTiles[row][column] = true;
            }
        }
    }

    public Vector2? IsColliding(Vector2 objectPosition)
    {
        for (int row = 0; row < Width; row++)
        {
            for (int column = 0; column < Length; column++)
            {
                if (!_foregroundTiles[row][column])
                    continue;

                var tilePosition = new Vector2(
                    x: column * Constants.TileSize,
                    y: row * Constants.TileSize
                );

                var collidingData = IsColliding(objectPosition, tilePosition);

                if (collidingData != null)
                    return collidingData;
            }
        }

        return null;
    }

    private static Vector2? IsColliding(Vector2 objectPosition, Vector2 tilePosition)
    {
        var objectRight = objectPosition.X + Constants.TileSize;
        var objectLeft = objectPosition.X;
        var tileRight = tilePosition.X + Constants.TileSize;
        var tileLeft = tilePosition.X;

        var objectBottom = objectPosition.Y + Constants.TileSize;
        var objectTop = objectPosition.Y;
        var tileBottom = tilePosition.Y + Constants.TileSize;
        var tileTop = tilePosition.Y;

        if (
            objectRight > tileLeft
            && objectLeft < tileRight
            && objectTop < tileBottom
            && objectBottom > tileTop
        )
        {
            var overlapLeft = objectRight - tileLeft;
            var overlapRight = tileRight - objectLeft;
            var overlapTop = objectBottom - tileTop;
            var overlapBottom = tileBottom - objectTop;

            (float Overlap, Func<Vector2> AdjustmentCalculation)[] overlaps =
            [
                (overlapLeft, () => Vector2.UnitX * (tileLeft - Constants.TileSize - objectLeft)),
                (overlapRight, () => Vector2.UnitX * (tileRight - objectLeft)),
                (overlapTop, () => Vector2.UnitY * (tileTop - Constants.TileSize - objectTop)),
                (overlapBottom, () => Vector2.UnitY * (tileBottom - objectTop)),
            ];

            return overlaps.MinBy(tuple => tuple.Overlap).AdjustmentCalculation();

            // if (objectBottom > tileTop && objectTop < tileTop) // From T to B
            //     return Vector2.UnitY * (tileTop - Constants.TileSize - objectTop);
            //
            // if (objectTop < tileBottom && objectBottom > tileBottom) // From B to T
            //     return Vector2.UnitY * (tileBottom - objectTop);
            //
            // if (objectRight > tileLeft && objectLeft < tileLeft) // From L to R
            //     return Vector2.UnitX * (tileLeft - Constants.TileSize - objectLeft);
            //
            // if (objectLeft < tileRight && objectRight > tileRight) // From R to L
            //     return Vector2.UnitX * (tileRight - objectLeft);
        }

        return null;
    }
}
