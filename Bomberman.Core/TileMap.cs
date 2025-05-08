using System.Collections.Immutable;
using Bomberman.Core.Tiles;
using Bomberman.Core.Utilities;

namespace Bomberman.Core;

public class TileMap : IUpdatable
{
    public int Width { get; }
    public int Height { get; }

    private readonly Tile[][] _backgroundTiles;
    private readonly Tile?[][] _foregroundTiles;
    private readonly StatefulRandom _rnd = new(7321);

    public ImmutableArray<ImmutableArray<Tile>> BackgroundTiles =>
        [.. _backgroundTiles.Select(row => row.ToImmutableArray())];

    public ImmutableArray<ImmutableArray<Tile?>> ForegroundTiles =>
        [.. _foregroundTiles.Select(row => row.ToImmutableArray())];

    public TileMap(int width, int height)
    {
        Width = width;
        Height = height;

        _backgroundTiles = Enumerable
            .Range(0, height)
            .Select(row =>
                Enumerable
                    .Range(0, width)
                    .Select(column => new GridPosition(row, column))
                    .Select(gridPosition => new FloorTile(gridPosition))
                    .ToArray()
            )
            .ToArray<Tile[]>();

        _foregroundTiles = Enumerable.Range(0, height).Select(_ => new Tile?[width]).ToArray();
    }

    internal TileMap(TileMap original)
    {
        Width = original.Width;
        Height = original.Height;
        _rnd = new StatefulRandom(original._rnd);

        _backgroundTiles = original
            .BackgroundTiles.Select(row =>
                row.Select(originalTile => originalTile.Clone(this)).ToArray()
            )
            .ToArray();

        _foregroundTiles = original
            ._foregroundTiles.Select(row =>
                row.Select(originalTile => originalTile?.Clone(this)).ToArray()
            )
            .ToArray();
    }

    internal TileMap WithRandomTileFill()
    {
        for (int row = 0; row < Height; row++)
        for (int column = 0; column < Width; column++)
            _foregroundTiles[row][column] = RandomTile(new GridPosition(row, column));

        return this;
    }

    internal TileMap WithCheckerPattern()
    {
        for (int row = 2; row < Height - 1; row += 2)
        for (int column = 2; column < Width - 1; column += 2)
            _foregroundTiles[row][column] = new WallTile(new GridPosition(row, column));

        return this;
    }

    // Clear around starting positions to allow players to move
    internal TileMap WithSpaceAround(params GridPosition[] startingTiles)
    {
        foreach (var start in startingTiles)
        foreach (var position in new[] { start }.Concat(start.Neighbours))
        {
            var tile = _foregroundTiles[position.Row][position.Column];
            if (tile is WallTile)
                continue;

            _foregroundTiles[position.Row][position.Column] = null;
        }

        return this;
    }

    internal TileMap WithBorder()
    {
        // Wall on top row
        _foregroundTiles[0] = Enumerable
            .Range(0, Width)
            .Select(column => new GridPosition(0, column))
            .Select(gridPosition => new WallTile(gridPosition))
            .ToArray<Tile>();

        // Wall on bottom row
        _foregroundTiles[Height - 1] = Enumerable
            .Range(0, Width)
            .Select(column => new GridPosition(Height - 1, column))
            .Select(gridPosition => new WallTile(gridPosition))
            .ToArray<Tile>();

        // Walls on left and right columns
        for (int row = 0; row < Height; row++)
        {
            _foregroundTiles[row][0] = new WallTile(new GridPosition(row, 0));
            _foregroundTiles[row][Width - 1] = new WallTile(new GridPosition(row, Width - 1));
        }

        return this;
    }

    public void Update(TimeSpan deltaTime)
    {
        foreach (var updatableTile in _foregroundTiles.SelectMany(row => row).OfType<IUpdatable>())
        {
            updatableTile.Update(deltaTime);
        }
    }

    internal Tile? GetTile(GridPosition gridPosition)
    {
        if (gridPosition.Row < 0 || gridPosition.Row >= Height)
            return null;

        if (gridPosition.Column < 0 || gridPosition.Column >= Width)
            return null;

        return _foregroundTiles[gridPosition.Row][gridPosition.Column];
    }

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

    private Tile? RandomTile(GridPosition position) =>
        _rnd.NextDouble() switch
        {
            < 0.4 => new BoxTile(position),
            _ => null,
        };
}
