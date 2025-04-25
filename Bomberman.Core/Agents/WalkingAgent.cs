using Bomberman.Core.Utilities;

namespace Bomberman.Core.Agents;

public class WalkingAgent : Agent
{
    private GridPosition? _target;
    private const float TargetThreshold = 0.1f;

    private readonly GameState _state;
    private readonly StatefulRandom _rnd;
    private bool _active;

    public WalkingAgent(GameState state, Player player, int agentIndex)
        : base(player, agentIndex)
    {
        _state = state;
        _rnd = new StatefulRandom();
        _active = true;
    }

    private WalkingAgent(GameState state, Player player, WalkingAgent original)
        : base(player, original.AgentIndex)
    {
        _state = state;
        _target = original._target;
        _rnd = new StatefulRandom(original._rnd);
        _active = original._active;
    }

    internal override Agent Clone(GameState state, Player player) =>
        new WalkingAgent(state, player, this);

    public override void Update(TimeSpan deltaTime)
    {
        base.Update(deltaTime);

        if (!_active)
            return;

        if (_target != null && _target.NearPosition(Player.Position, TargetThreshold))
            return;

        var playerPosition = Player.Position.ToGridPosition();

        var clearTiles = playerPosition
            .Neighbours.Where(tile => _state.TileMap.GetTile(tile) is null)
            .ToList();

        if (clearTiles.Count == 0)
        {
            // TODO: Retry to find a new target (at intervals) instead of giving up completely
            _active = false;
            return;
        }

        var randomIndex = (int)(_rnd.NextDouble() * clearTiles.Count);
        _target = clearTiles[randomIndex];

        if (playerPosition.Row < _target.Row)
            Player.SetMovingDirection(Direction.Down);
        else if (playerPosition.Row > _target.Row)
            Player.SetMovingDirection(Direction.Up);
        else if (playerPosition.Column < _target.Column)
            Player.SetMovingDirection(Direction.Right);
        else if (playerPosition.Column > _target.Column)
            Player.SetMovingDirection(Direction.Left);
    }
}
