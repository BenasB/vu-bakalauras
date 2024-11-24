namespace Bomberman.Core.Agents.Mcts;

public class Node
{
    public required BombermanAction Action { get; init; }

    public required Node Parent { get; init; }
}
