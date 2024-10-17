using System.Collections.Immutable;
using System.Numerics;
using Bomberman.Core.Tiles;

namespace Bomberman.Core;

public class TileMap : IUpdatable
{
    public int Length { get; }
    public int Width { get; }

    private readonly Tile[][] _backgroundTiles;
    private readonly Tile?[][] _foregroundTiles;

    public ImmutableArray<Tile?> Tiles =>
        [.. _backgroundTiles.Concat(_foregroundTiles).SelectMany(row => row)];

    public TileMap(int length, int width, GridPosition start)
    {
        Length = length;
        Width = width;

        _backgroundTiles = Enumerable
            .Range(0, width)
            .Select(row =>
                Enumerable
                    .Range(0, length)
                    .Select(column => new GridPosition(row, column))
                    .Select(gridPosition => new FloorTile(gridPosition))
                    .ToArray()
            )
            .ToArray<Tile[]>();

        _foregroundTiles = Enumerable.Range(0, width).Select(_ => new Tile?[length]).ToArray();

        // Wall on top row
        _foregroundTiles[0] = Enumerable
            .Range(0, length)
            .Select(column => new GridPosition(0, column))
            .Select(gridPosition => new WallTile(gridPosition))
            .ToArray<Tile>();

        // Wall on bottom row
        _foregroundTiles[width - 1] = Enumerable
            .Range(0, length)
            .Select(column => new GridPosition(width - 1, column))
            .Select(gridPosition => new WallTile(gridPosition))
            .ToArray<Tile>();

        // Walls on left and right columns
        for (int row = 0; row < width; row++)
        {
            _foregroundTiles[row][0] = new WallTile(new GridPosition(row, 0));
            _foregroundTiles[row][length - 1] = new WallTile(new GridPosition(row, length - 1));
        }

        // Checker walls
        for (int row = 2; row < Width - 1; row += 2)
        for (int column = 2; column < Length - 1; column += 2)
            _foregroundTiles[row][column] = new WallTile(new GridPosition(row, column));

        // Boxes
        var rnd = new Random(Seed: 42);
        var freeTiles =
            length * width - _foregroundTiles.SelectMany(row => row).Count(x => x != null);
        var boxTiles = 0;
        while (boxTiles < freeTiles / 2)
        {
            GridPosition boxPosition;
            do
            {
                boxPosition = new GridPosition(rnd.Next(0, Width), rnd.Next(0, Length));
            } while (_foregroundTiles[boxPosition.Row][boxPosition.Column] != null);

            _foregroundTiles[boxPosition.Row][boxPosition.Column] = new BoxTile(boxPosition);
            boxTiles++;
        }

        // Clear around starting position to allow player to move
        _foregroundTiles[start.Row][start.Column] = null;
        _foregroundTiles[start.Row - 1][start.Column] = null;
        _foregroundTiles[start.Row + 1][start.Column] = null;
        _foregroundTiles[start.Row][start.Column - 1] = null;
        _foregroundTiles[start.Row][start.Column + 1] = null;
    }

    public void Update(TimeSpan deltaTime)
    {
        foreach (var updatableTile in _foregroundTiles.SelectMany(row => row).OfType<IUpdatable>())
        {
            updatableTile.Update(deltaTime);
        }
    }

    internal Tile? GetTile(GridPosition gridPosition) =>
        _foregroundTiles[gridPosition.Row][gridPosition.Column];

    internal void PlaceTile(GridPosition gridPosition, Tile newTile)
    {
        var existingTile = _foregroundTiles[gridPosition.Row][gridPosition.Column];

        if (existingTile != null)
            throw new InvalidOperationException(
                $"This grid position is already taken ({gridPosition})"
            );

        _foregroundTiles[gridPosition.Row][gridPosition.Column] = newTile;
    }

    internal void RemoveTile(Tile tile)
    {
        if (_foregroundTiles[tile.Position.Row][tile.Position.Column] == null)
            throw new InvalidOperationException($"Tile is not in the tile map ({tile.Position})");

        if (_foregroundTiles[tile.Position.Row][tile.Position.Column] != tile)
            throw new InvalidOperationException(
                $"Tile in the tile map is not the specified tile ({tile.Position})"
            );

        _foregroundTiles[tile.Position.Row][tile.Position.Column] = null;
    }

    // TODO: Move collision logic to a collision component
    /// <returns>Position adjustment vector for staying out of the colliding tile, null if no collision is detected</returns>
    internal Vector2? IsColliding(Vector2 objectPosition, object caller)
    {
        for (int row = 0; row < Width; row++)
        {
            for (int column = 0; column < Length; column++)
            {
                if (_foregroundTiles[row][column] == null)
                    continue;

                var tilePosition = new Vector2(
                    x: column * Constants.TileSize,
                    y: row * Constants.TileSize
                );

                var collidingData = _foregroundTiles[row][column] switch
                {
                    IEnterable => IsColliding(
                        objectPosition.ToGridPosition(),
                        tilePosition,
                        applyOverlapThreshold: false
                    ),
                    _ => IsColliding(objectPosition, tilePosition, applyOverlapThreshold: true),
                };
                IsColliding(
                    objectPosition,
                    tilePosition,
                    applyOverlapThreshold: _foregroundTiles[row][column] is not IEnterable
                );

                if (collidingData == null)
                    continue;

                if (_foregroundTiles[row][column] is IEnterable enterableTile)
                {
                    enterableTile.OnEntered(caller);
                    continue;
                }

                return collidingData;
            }
        }

        return null;
    }

    private static Vector2? IsColliding(
        Vector2 objectPosition,
        Vector2 tilePosition,
        bool applyOverlapThreshold
    )
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

            // Do not consider big overlaps (and let the user walk out of the tile themselves, e.g. bomb tile)
            const float overlapConsiderationThreshold = 0.1f * Constants.TileSize;

            var consideredOverlaps = (
                applyOverlapThreshold
                    ? overlaps.Where(tuple => tuple.Overlap < overlapConsiderationThreshold)
                    : overlaps
            ).ToList();

            if (consideredOverlaps.Count == 0)
                return null;

            return consideredOverlaps.MinBy(tuple => tuple.Overlap).AdjustmentCalculation();
        }

        return null;
    }
}
