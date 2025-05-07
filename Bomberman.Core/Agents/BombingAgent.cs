using Bomberman.Core.Utilities;

namespace Bomberman.Core.Agents;

public class BombingAgent : Agent
{
    private readonly GameState _state;
    private readonly Queue<GridPosition> _path;
    private readonly Walker _walker;

    public BombingAgent(GameState state, Player player, int agentIndex)
        : base(player, agentIndex)
    {
        _state = state;
        var opponentAgent = state.Agents.First(agent => agent.AgentIndex != agentIndex);
        _path = new Queue<GridPosition>(
            state.TileMap.ShortestPath(
                player.Position.ToGridPosition(),
                opponentAgent.Player.Position.ToGridPosition(),
                player.Speed
            ) ?? throw new InvalidOperationException("Could not find a path to the opponent")
        );
        _walker = new Walker(player, GetNextTarget);
    }

    private BombingAgent(GameState state, Player player, BombingAgent original)
        : base(player, original.AgentIndex)
    {
        _state = state;
        _path = new Queue<GridPosition>(original._path);
        _walker = new Walker(player, GetNextTarget, original._walker);
    }

    internal override Agent Clone(GameState state, Player player) =>
        new StaticAgent(player, AgentIndex);

    public override void Update(TimeSpan deltaTime)
    {
        base.Update(deltaTime);

        // TODO: More behaviour
        if (_walker.IsFinished)
            return;

        _walker.Update(deltaTime);
    }

    private GridPosition? GetNextTarget()
    {
        return _path.TryDequeue(out var target) ? target : null;
    }
}
