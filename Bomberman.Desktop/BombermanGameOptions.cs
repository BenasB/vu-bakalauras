using System;
using Bomberman.Core.Agents;
using Bomberman.Core.Agents.MCTS;

namespace Bomberman.Desktop;

internal record BombermanGameOptions
{
    public AgentType PlayerOne { get; set; } = AgentType.Walking;
    public AgentType PlayerTwo { get; set; } = AgentType.Walking;

    public MctsAgentOptions PlayerOneMctsOptions { get; set; } = new();
    public MctsAgentOptions PlayerTwoMctsOptions { get; set; } = new();

    public int Seed { get; set; } = 44;

    public string? JsonReportFilePath { get; set; } = null;

    public TimeSpan? Timeout { get; set; } = null;
}
