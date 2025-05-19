using Bomberman.Core.Tiles;
using Bomberman.Core.Utilities;

namespace Bomberman.Core.Agents;

public class BombingAgent2 : Agent
{
    private readonly GameState _state;
    private Queue<GridPosition> _safetyPathQueue = new();
    private Walker? _safetyWalker;
    private BombTile? _placedBombTile;
    private GridPosition? _threatPosition;

    private readonly StatefulRandom _rnd = new();

    private bool _init;

    private Agent? _opponentBacking;

    private Agent Opponent =>
        _opponentBacking ??= _state.Agents.First(agent => agent.AgentIndex != AgentIndex);

    public BombingAgent2(GameState state, Player player, int agentIndex)
        : base(player, agentIndex)
    {
        player.SetMovingDirection(Direction.None);
        _state = state;
    }

    private BombingAgent2(GameState state, Player player, BombingAgent2 original)
        : base(player, original.AgentIndex)
    {
        _state = state;
        _init = original._init;
        _rnd = new StatefulRandom(original._rnd);
        _safetyPathQueue = new Queue<GridPosition>(original._safetyPathQueue);

        if (original._placedBombTile != null)
        {
            var tile = state.TileMap.GetTile(original._placedBombTile.Position);
            if (tile is ExplosionTile)
            {
                _placedBombTile = null;
                _threatPosition = original._threatPosition;
            }
            else if (tile != null)
            {
                _placedBombTile = (BombTile)tile;
                _threatPosition = original._threatPosition;
            }
            else
            {
                _placedBombTile = null;
                _threatPosition = null;
            }
        }

        _safetyWalker =
            original._safetyWalker == null
                ? null
                : new Walker(player, state.TileMap, GetNextTarget, original._safetyWalker);
    }

    internal override Agent Clone(GameState state, Player player) =>
        new BombingAgent2(state, player, this);

    public override void Update(TimeSpan deltaTime)
    {
        base.Update(deltaTime);

        if (!Opponent.Player.Alive)
            return;

        if (_safetyWalker == null)
        {
            var pos = Player.Position.ToGridPosition();
            var oppPos = Opponent.Player.Position.ToGridPosition();
            if (pos.Neighbours.Contains(oppPos))
            {
                PlaceBombAndMoveToSafety();
            }

            return;
        }

        if (_safetyWalker is { IsFinished: true })
        {
            // Wait for the bomb to fully explode
            if (
                _placedBombTile is { Detonated: false }
                || (_threatPosition != null && _state.TileMap.GetTile(_threatPosition) != null)
            )
            {
                return;
            }

            _placedBombTile = null;
            _threatPosition = null;
            _safetyWalker = null;
        }

        _safetyWalker?.Update(deltaTime);
    }

    private GridPosition? GetNextTarget()
    {
        return _safetyPathQueue.TryDequeue(out var target) ? target : null;
    }

    private void PlaceBombAndMoveToSafety()
    {
        _threatPosition = Player.Position.ToGridPosition();
        if (_state.TileMap.GetTile(_threatPosition) == null && Player.CanPlaceBomb)
        {
            _placedBombTile = Player.PlaceBomb();
        }

        var pathToSafety = GetSafetyPath(_threatPosition, _placedBombTile);
        if (pathToSafety == null)
            return;

        _safetyPathQueue = new Queue<GridPosition>(pathToSafety);
        _safetyWalker = new Walker(Player, _state.TileMap, GetNextTarget);
    }

    private List<GridPosition>? GetSafetyPath(GridPosition threatPosition, BombTile? bombTile)
    {
        var dangerousPositions = new List<GridPosition> { threatPosition };
        if (bombTile != null)
            dangerousPositions = dangerousPositions
                .Concat(bombTile.ExplosionPaths.SelectMany(x => x))
                .ToList();

        var queue = new Queue<GridPosition>();
        queue.Enqueue(threatPosition);
        var parents = new GridPosition?[_state.TileMap.Height, _state.TileMap.Width];

        GridPosition? current;
        while (queue.TryDequeue(out current))
        {
            if (!dangerousPositions.Contains(current))
                break;

            // Introduce uncertainty
            var neighbours = current.Neighbours.ToArray();
            _rnd.Shuffle(neighbours);

            foreach (var neighbour in neighbours)
            {
                if (
                    neighbour.Row < 0
                    || neighbour.Row >= _state.TileMap.Height
                    || neighbour.Column < 0
                    || neighbour.Column >= _state.TileMap.Width
                )
                    continue;

                if (_state.TileMap.GetTile(neighbour) != null)
                    continue;

                parents[neighbour.Row, neighbour.Column] = current;
                queue.Enqueue(neighbour);
            }
        }

        var finish = current;
        if (finish == null)
            return null;

        var parent = parents[finish.Row, finish.Column];
        if (parent == null)
            return null;

        var path = new List<GridPosition> { finish };
        while (parent != null)
        {
            path.Add(parent);
            parent = parents[parent.Row, parent.Column];
        }

        path.Reverse();
        return path;
    }
}
