using Bomberman.Core;
using Bomberman.Core.Agents;
using Bomberman.Core.Agents.MCTS;

namespace Bomberman.Desktop;

internal record BombermanGameOptions
{
    public AgentType PlayerOne { get; set; } = AgentType.Walking;
    public AgentType PlayerTwo { get; set; } = AgentType.Walking;

    public MctsAgentOptions PlayerOneMctsOptions { get; set; } = new();
    public MctsAgentOptions PlayerTwoMctsOptions { get; set; } = new();

    public Scenario Scenario { get; } = Scenario.Empty;
}
