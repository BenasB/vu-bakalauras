using System.Collections.Immutable;
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
            .Select(gridPosition => new WallTile(gridPosition))
            .ToArray<Tile>();

        // Wall on bottom row
        _foregroundTiles[Width - 1] = Enumerable
            .Range(0, Length)
            .Select(column => new GridPosition(Width - 1, column))
            .Select(gridPosition => new WallTile(gridPosition))
            .ToArray<Tile>();

        // Walls on left and right columns
        for (int row = 0; row < Width; row++)
        {
            _foregroundTiles[row][0] = new WallTile(new GridPosition(row, 0));
            _foregroundTiles[row][Length - 1] = new WallTile(new GridPosition(row, Length - 1));
        }

        // Checker walls
        for (int row = 2; row < Width - 1; row += 2)
        for (int column = 2; column < Length - 1; column += 2)
            _foregroundTiles[row][column] = new WallTile(new GridPosition(row, column));

        // Boxes
        var rnd = new Random(Seed: 42);
        var freeTiles =
            Length * Width - _foregroundTiles.SelectMany(row => row).Count(x => x != null);
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
}
