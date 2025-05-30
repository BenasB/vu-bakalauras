namespace Bomberman.Core.Serialization;

internal record NodeDto(
    string? Action,
    string[] UnexploredActions,
    int Visits,
    double TotalReward,
    double AverageReward,
    double HeuristicValue,
    GameStateDto State,
    NodeDto[] Children,
    // DEBUG
    GameStateDto? SimulationEndState
);

internal record GameStateDto(PlayerDto[] Players, int[][] TileMap, bool Terminated);

internal record PlayerDto(Vector2Dto Position, bool Alive);

internal record Vector2Dto(float X, float Y);
