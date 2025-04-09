using Bomberman.Core.Agents.MCTS;
using Bomberman.Core.Tiles;

namespace Bomberman.Core.Serialization;

internal static class ToDtoExtensions
{
    private static int ToDto(this Tile? tile) =>
        tile switch
        {
            null => -1,
            BombTile => 0,
            BoxTile => 1,
            ExplosionTile => 2,
            WallTile => 3,
            _ => throw new InvalidOperationException(
                "Could not map tile to a tile type for serialization"
            ),
        };

    private static PlayerDto ToDto(this Player player) =>
        new(new Vector2Dto(player.Position.X, player.Position.Y), player.Alive);

    private static int[][] ToDto(this TileMap tileMap) =>
        tileMap.ForegroundTiles.Select(r => r.Select(t => t.ToDto()).ToArray()).ToArray();

    private static GameStateDto ToDto(this GameState state) =>
        new(
            state.Agents.Select(a => a.Player.ToDto()).ToArray(),
            state.TileMap.ToDto(),
            state.Terminated
        );

    public static NodeDto ToDto(this Node node) =>
        new(
            node.Action.ToString(),
            node.UnexploredActions.Select(a => a.ToString()).ToArray(),
            node.Visits,
            node.TotalReward,
            node.AverageReward,
            node.HeuristicValue,
            node.State.ToDto(),
            node.Children.Select(n => n.ToDto()).ToArray(),
            // DEBUG
            node.SimulationEndState?.ToDto()
        );
}
