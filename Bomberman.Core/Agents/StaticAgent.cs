namespace Bomberman.Core.Agents;

public class StaticAgent : Agent
{
    public StaticAgent(Player player)
        : base(player) { }

    internal override Agent Clone(GameState state, Player player) => new StaticAgent(player);
}
