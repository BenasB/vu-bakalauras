namespace Bomberman.Core.Agents;

public abstract class Agent : IUpdatable
{
    // TODO: Add player number

    public readonly Player Player;

    protected Agent(Player player)
    {
        Player = player;
    }

    public virtual void Update(TimeSpan deltaTime)
    {
        Player.Update(deltaTime);
    }

    internal abstract Agent Clone(GameState state, Player player);
}
