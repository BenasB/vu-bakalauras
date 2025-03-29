namespace Bomberman.Core.Serialization;

internal record NodeDto(
    string? Action,
    string[] UnexploredActions,
    int Visits,
    double TotalReward,
    double AverageReward,
    GameStateDto State,
    NodeDto[] Children
);

internal record GameStateDto(PlayerDto[] Players, TileMapDto TileMap, bool Terminated);

internal record PlayerDto(
    Vector2Dto Position,
    float Speed,
    int BombRange,
    int MaxPlacedBombs,
    bool Alive
);

internal record Vector2Dto(float X, float Y);

internal record TileMapDto(int Width, int Height, TileDto[] Tiles);

internal record TileDto(GridPosition Position, string Type);
