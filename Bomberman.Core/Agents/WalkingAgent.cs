using Bomberman.Core.Utilities;

namespace Bomberman.Core.Agents;

public class WalkingAgent : Agent
{
    private GridPosition _target;
    private const float TargetThreshold = 0.1f;

    private readonly GameState _state;
    private readonly StatefulRandom _rnd;

    public WalkingAgent(GameState state, Player player)
        : base(player)
    {
        _state = state;
        _target = player.Position.ToGridPosition();
        _rnd = new StatefulRandom();
    }

    private WalkingAgent(GameState state, Player player, WalkingAgent original)
        : base(player)
    {
        _state = state;
        _target = original._target;
        _rnd = new StatefulRandom(original._rnd);
    }

    internal override Agent Clone(GameState state, Player player) =>
        new WalkingAgent(state, player, this);

    public override void Update(TimeSpan deltaTime)
    {
        base.Update(deltaTime);

        while (!IsPlayerOnTarget())
            return;

        var playerPosition = Player.Position.ToGridPosition();

        var clearTiles = playerPosition
            .Neighbours.Where(tile => _state.TileMap.GetTile(tile) is null)
            .ToList();

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

    private bool IsPlayerOnTarget() =>
        Player.Position.X > _target.Column * Constants.TileSize - TargetThreshold
        && Player.Position.X < _target.Column * Constants.TileSize + TargetThreshold
        && Player.Position.Y > _target.Row * Constants.TileSize - TargetThreshold
        && Player.Position.Y < _target.Row * Constants.TileSize + TargetThreshold;
}
