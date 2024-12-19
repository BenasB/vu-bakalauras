using System.Collections.Immutable;
using Bomberman.Core.Tiles;

namespace Bomberman.Core;

public class TileMap : IUpdatable
{
    public int Length { get; }
    public int Width { get; }

    private readonly Tile[][] _backgroundTiles;
    private readonly Tile?[][] _foregroundTiles;
    private static readonly Random Rnd = new();

    public ImmutableArray<Tile?> Tiles =>
        [.. _backgroundTiles.Concat(_foregroundTiles).SelectMany(row => row)];

    public TileMap(int length, int width)
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
    }

    internal TileMap(TileMap original)
    {
        Length = original.Length;
        Width = original.Width;

        _backgroundTiles = original
            ._backgroundTiles.Select(row =>
                row.Select(originalTile => originalTile.Clone(this)).ToArray()
            )
            .ToArray();

        _foregroundTiles = original
            ._foregroundTiles.Select(row =>
                row.Select(originalTile => originalTile?.Clone(this)).ToArray()
            )
            .ToArray();
    }

    internal TileMap WithDefaultTileLayout(GridPosition start)
    {
        // Wall on top row
        _foregroundTiles[0] = Enumerable
            .Range(0, Length)
            .Select(column => new GridPosition(0, column))
            .Select(gridPosition => new LavaTile(gridPosition))
            .ToArray<Tile>();

        // Wall on bottom row
        _foregroundTiles[Width - 1] = Enumerable
            .Range(0, Length)
            .Select(column => new GridPosition(Width - 1, column))
            .Select(gridPosition => new LavaTile(gridPosition))
            .ToArray<Tile>();

        // Walls on left and right columns
        for (int row = 0; row < Width; row++)
        {
            _foregroundTiles[row][0] = new LavaTile(new GridPosition(row, 0));
            _foregroundTiles[row][Length - 1] = new LavaTile(new GridPosition(row, Length - 1));
        }

        for (int row = 1; row < Width - 1; row++)
        {
            for (int column = 1; column < Length - 1; column++)
            {
                _foregroundTiles[row][column] = RandomTile(new GridPosition(row, column));
            }
        }

        // Checker walls
        for (int row = 2; row < Width - 1; row += 2)
        for (int column = 2; column < Length - 1; column += 2)
            _foregroundTiles[row][column] = new WallTile(new GridPosition(row, column));

        // Clear around starting position to allow player to move
        foreach (var position in new[] { start }.Concat(start.Neighbours))
            _foregroundTiles[position.Row][position.Column] = null;

        return this;
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

    internal void PlaceTile(Tile newTile)
    {
        var gridPosition = newTile.Position;
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

    internal void Shift()
    {
        for (int row = 1; row < Width - 1; row++)
        {
            for (int column = 1; column < Length - 2; column++)
            {
                var oldTile = _foregroundTiles[row][column];

                if (oldTile is BombTile oldBombTile)
                {
                    // Force into a detonated state to unblock player bomb placement logic
                    oldBombTile.Detonated = true;
                }

                _foregroundTiles[row][column] = _foregroundTiles[row][column + 1];
                var shiftedTile = _foregroundTiles[row][column];
                _foregroundTiles[row][column + 1] = null;

                if (shiftedTile != null)
                    shiftedTile.Position = new GridPosition(Row: row, Column: column);
            }
        }

        for (int row = 1; row < Width - 1; row++)
        {
            _foregroundTiles[row][Length - 2] = RandomTile(
                new GridPosition(Row: row, Column: Length - 2)
            );
        }

        for (int row = 2; row < Width - 1; row += 2)
        {
            if (GetTile(new GridPosition(row, Length - 2 - 1)) is not WallTile)
                _foregroundTiles[row][Length - 2] = new WallTile(new GridPosition(row, Length - 2));
        }
    }

    private Tile? RandomTile(GridPosition position) =>
        Rnd.NextDouble() switch
        {
            < 0.491 => new BoxTile(position),
            < 0.494 => new FireUpTile(position, this),
            < 0.497 => new SpeedUpTile(position, this),
            < 0.5 => new BombUpTile(position, this),
            < 0.6 => new CoinTile(position, this),
            _ => null,
        };
}
