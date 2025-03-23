namespace Bomberman.Core.Agents;

public abstract class Agent : IUpdatable
{
    protected readonly int AgentIndex;

    public readonly Player Player;

    protected Agent(Player player, int agentIndex)
    {
        Player = player;
        AgentIndex = agentIndex;
    }

    public virtual void Update(TimeSpan deltaTime)
    {
        Player.Update(deltaTime);
    }

    internal abstract Agent Clone(GameState state, Player player);
}
