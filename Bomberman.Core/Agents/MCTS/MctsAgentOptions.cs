namespace Bomberman.Core.Agents.MCTS;

public record MctsAgentOptions
{
    public AgentType? OpponentType { get; set; } = null;

    public bool Export { get; set; } = false;
}
