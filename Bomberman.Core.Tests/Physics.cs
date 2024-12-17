using System.Numerics;
using Bomberman.Core.Tiles;

namespace Bomberman.Core.Tests;

public class Physics
{
    [Theory]
    [InlineData(16)]
    [InlineData(16 * 2)]
    [InlineData(16 * 3)]
    [InlineData(16 * 4)]
    [InlineData(16 * 6)]
    [InlineData(16 * 10)]
    [InlineData(16 * 100)]
    public void PlayerMovement_BigVelocityNextToWall_DoesNotWalkThroughSolidWall(int deltaTimeMs)
    {
        var deltaTime = TimeSpan.FromMilliseconds(deltaTimeMs);

        // Player is right next to a wall and moving with any velocity towards it should not change the players position
        var startingPlayerPosition = new GridPosition(0, 0);
        var wallPosition = new GridPosition(0, 1);

        var tileMap = new TileMap(3, 1);
        tileMap.PlaceTile(new WallTile(wallPosition));
        var player = new Player(startingPlayerPosition, tileMap);

        player.SetMovingDirection(Direction.Right);
        player.Update(deltaTime);

        Assert.Equal((Vector2)startingPlayerPosition, player.Position);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void PlayerMovement_BigVelocityFacingWall_PlacesPlayerNextToWall(int numberOfTilesToWall)
    {
        var deltaTime = TimeSpan.FromMilliseconds(16 * 1000);

        var startingPlayerPosition = new GridPosition(0, 0);
        var wallPosition = new GridPosition(0, numberOfTilesToWall + 1);

        var tileMap = new TileMap(numberOfTilesToWall + 2, 1);
        tileMap.PlaceTile(new WallTile(wallPosition));
        var player = new Player(startingPlayerPosition, tileMap);

        player.SetMovingDirection(Direction.Right);
        player.Update(deltaTime);

        Assert.Equal(
            (Vector2)(wallPosition with { Column = wallPosition.Column - 1 }),
            player.Position
        );
    }
}
