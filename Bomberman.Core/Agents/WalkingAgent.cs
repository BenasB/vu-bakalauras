using Bomberman.Core.Utilities;

namespace Bomberman.Core.Agents;

public class WalkingAgent : Agent
{
    private readonly GameState _state;
    private readonly StatefulRandom _rnd;

    private readonly Walker _walker;

    public WalkingAgent(GameState state, Player player, int agentIndex)
        : base(player, agentIndex)
    {
        player.SetMovingDirection(Direction.None);
        _state = state;
        _rnd = new StatefulRandom();
        _walker = new Walker(player, GetRandomTarget);
    }

    private WalkingAgent(GameState state, Player player, WalkingAgent original)
        : base(player, original.AgentIndex)
    {
        _state = state;
        _rnd = new StatefulRandom(original._rnd);
        _walker = new Walker(player, GetRandomTarget, original._walker);
    }

    internal override Agent Clone(GameState state, Player player) =>
        new WalkingAgent(state, player, this);

    public override void Update(TimeSpan deltaTime)
    {
        base.Update(deltaTime);

        if (_walker.IsFinished || _walker.IsStuck)
            return;

        _walker.Update(deltaTime);
    }

    private GridPosition? GetRandomTarget()
    {
        var playerPosition = Player.Position.ToGridPosition();

        var clearTiles = playerPosition
            .Neighbours.Where(tile => _state.TileMap.GetTile(tile) is null)
            .ToList();

        if (clearTiles.Count == 0)
        {
            return null;
        }

        var randomIndex = (int)(_rnd.NextDouble() * clearTiles.Count);
        return clearTiles[randomIndex];
    }
}
