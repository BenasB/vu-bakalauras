namespace Bomberman.Core.Agents;

public class StaticAgent : Agent
{
    public StaticAgent(Player player, int agentIndex)
        : base(player, agentIndex) { }

    internal override Agent Clone(GameState state, Player player) =>
        new StaticAgent(player, AgentIndex);
}
