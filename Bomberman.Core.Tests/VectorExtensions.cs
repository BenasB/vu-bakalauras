using System.Numerics;
using Bomberman.Core.Utilities;

namespace Bomberman.Core.Tests;

public class VectorExtensions
{
    private const float Epsilon = 0.01f;
    private const float MaximumGridOffset = Constants.TileSize / 2.0f - Epsilon;

    [Theory]
    [InlineData(4, 6)]
    [InlineData(4, 4)]
    [InlineData(0, 0)]
    public void ToGridPosition_ExactlyCentered(int row, int column)
    {
        var gridPosition = new GridPosition(Row: row, Column: column);
        var position = new Vector2(
            gridPosition.Column * Constants.TileSize,
            gridPosition.Row * Constants.TileSize
        );

        var result = position.ToGridPosition();

        Assert.Equal(gridPosition, result);
    }

    [Theory]
    [InlineData(4, 6)]
    [InlineData(4, 4)]
    [InlineData(0, 0)]
    public void ToGridPosition_OffsetOnRight(int row, int column)
    {
        var gridPosition = new GridPosition(Row: row, Column: column);

        var position = new Vector2(
            gridPosition.Column * Constants.TileSize + MaximumGridOffset,
            gridPosition.Row * Constants.TileSize
        );

        var result = position.ToGridPosition();

        Assert.Equal(gridPosition, result);
    }

    [Theory]
    [InlineData(4, 6)]
    [InlineData(4, 4)]
    [InlineData(0, 0)]
    public void ToGridPosition_OffsetOnLeft(int row, int column)
    {
        var gridPosition = new GridPosition(Row: row, Column: column);

        var position = new Vector2(
            gridPosition.Column * Constants.TileSize - MaximumGridOffset,
            gridPosition.Row * Constants.TileSize
        );

        var result = position.ToGridPosition();

        Assert.Equal(gridPosition, result);
    }

    [Theory]
    [InlineData(4, 6)]
    [InlineData(4, 4)]
    [InlineData(0, 0)]
    public void ToGridPosition_OffsetOnTop(int row, int column)
    {
        var gridPosition = new GridPosition(Row: row, Column: column);

        var position = new Vector2(
            gridPosition.Column * Constants.TileSize,
            gridPosition.Row * Constants.TileSize - MaximumGridOffset
        );

        var result = position.ToGridPosition();

        Assert.Equal(gridPosition, result);
    }

    [Theory]
    [InlineData(4, 6)]
    [InlineData(4, 4)]
    [InlineData(0, 0)]
    public void ToGridPosition_OffsetOnBottom(int row, int column)
    {
        var gridPosition = new GridPosition(Row: row, Column: column);

        var position = new Vector2(
            gridPosition.Column * Constants.TileSize,
            gridPosition.Row * Constants.TileSize + MaximumGridOffset
        );

        var result = position.ToGridPosition();

        Assert.Equal(gridPosition, result);
    }
}
