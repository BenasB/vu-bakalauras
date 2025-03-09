using Bomberman.Core.MCTS;
using Bomberman.Core.Tiles;

namespace Bomberman.Core.Serialization;

internal static class ToDtoExtensions
{
    private static TileDto ToDto(this Tile tile) =>
        new(
            tile.Position,
            tile switch
            {
                BombTile => "bomb",
                BombUpTile => "bombup",
                BoxTile => "box",
                CoinTile => "coin",
                ExplosionTile => "explosion",
                FireUpTile => "fireup",
                SpeedUpTile => "speedup",
                WallTile => "wall",
                FloorTile => "floor",
                _ => throw new InvalidOperationException(
                    "Could not map tile to a tile type for serialization"
                ),
            }
        );

    private static PlayerDto ToDto(this Player player) =>
        new(
            new Vector2Dto(player.Position.X, player.Position.Y),
            player.Score,
            player.Speed,
            player.BombRange,
            player.MaxPlacedBombs,
            player.Alive
        );

    private static TileMapDto ToDto(this TileMap tileMap) =>
        new(tileMap.Width, tileMap.Height, tileMap.Tiles.Select(t => t.ToDto()).ToArray());

    private static GameStateDto ToDto(this GameState state) =>
        new(state.Player.ToDto(), state.TileMap.ToDto(), state.Terminated);

    public static NodeDto ToDto(this Node node) =>
        new(
            node.Action.ToString(),
            node.UnexploredActions.Select(a => a.ToString()).ToArray(),
            node.Visits,
            node.TotalReward,
            node.AverageReward,
            node.State.ToDto(),
            node.Children.Select(n => n.ToDto()).ToArray()
        );
}
