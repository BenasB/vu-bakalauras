using Bomberman.Core.Tiles;
using Bomberman.Core.Utilities;

namespace Bomberman.Core.Tests;

public class Walker
{
    [Fact]
    public void ExplosionInPath_WaitsForExplosionToEnd()
    {
        var tileMap = new TileMap(5, 1);
        var player = new Player(new GridPosition(0, 0), tileMap);
        tileMap.PlaceTile(
            new ExplosionTile(new GridPosition(0, 2), tileMap, TimeSpan.FromSeconds(5))
        );
        var walker = new Bomberman.Core.Agents.Walker(
            player,
            tileMap,
            () =>
            {
                var pos = player.Position.ToGridPosition();
                return pos == new GridPosition(0, 4) ? null : pos with { Column = pos.Column + 1 };
            }
        );

        const int fps = 60;
        const double secondsPerFrame = 1.0 / fps;
        var deltaTime = TimeSpan.FromSeconds(secondsPerFrame);
        var frameCount = (int)Math.Ceiling(20 / secondsPerFrame);
        for (int i = 0; i < frameCount; i++)
        {
            player.Update(deltaTime);
            tileMap.Update(deltaTime);
            walker.Update(deltaTime);
        }

        Assert.Multiple(
            () => Assert.True(player.Alive, "player.Alive"),
            () => Assert.Equal(new GridPosition(0, 4), player.Position.ToGridPosition()),
            () => Assert.True(walker.IsFinished, "walker.IsFinished")
        );
    }
}
