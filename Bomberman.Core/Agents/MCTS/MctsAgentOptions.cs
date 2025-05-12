namespace Bomberman.Core.Agents.MCTS;

public record MctsAgentOptions
{
    public AgentType? OpponentType { get; set; } = null;

    public double SelectionHeuristicWeightCoefficient { get; set; } = 1.0 / 4;

    public bool Export { get; set; } = false;
}
