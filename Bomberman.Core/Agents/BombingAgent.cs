using Bomberman.Core.Tiles;
using Bomberman.Core.Utilities;

namespace Bomberman.Core.Agents;

public class BombingAgent : Agent
{
    private readonly GameState _state;
    private Queue<GridPosition> _pathQueue = new();
    private Walker? _attackWalker;
    private Walker? _safetyWalker;
    private BombTile? _placedBombTile;
    private GridPosition? _placedBombPosition;

    private bool _init;

    private Agent? _opponentBacking;

    private Agent Opponent
    {
        get
        {
            return _opponentBacking ??= _state.Agents.First(agent =>
                agent.AgentIndex != AgentIndex
            );
        }
    }

    public BombingAgent(GameState state, Player player, int agentIndex)
        : base(player, agentIndex)
    {
        player.SetMovingDirection(Direction.None);
        _state = state;
    }

    private BombingAgent(GameState state, Player player, BombingAgent original)
        : base(player, original.AgentIndex)
    {
        _state = state;
        _init = original._init;
        _pathQueue = new Queue<GridPosition>(original._pathQueue);

        if (original._placedBombTile != null)
        {
            var tile = state.TileMap.GetTile(original._placedBombTile.Position);
            if (tile is ExplosionTile)
            {
                _placedBombTile = null;
                _placedBombPosition = original._placedBombPosition;
            }
            else if (tile != null)
            {
                _placedBombTile = (BombTile)tile;
                _placedBombPosition = original._placedBombPosition;
            }
            else
            {
                _placedBombTile = null;
                _placedBombPosition = null;
            }
        }

        _attackWalker =
            original._attackWalker == null
                ? null
                : new Walker(player, GetNextTarget, original._attackWalker);
        _safetyWalker =
            original._safetyWalker == null
                ? null
                : new Walker(player, GetNextTarget, original._safetyWalker);
    }

    internal override Agent Clone(GameState state, Player player) =>
        new BombingAgent(state, player, this);

    public override void Update(TimeSpan deltaTime)
    {
        base.Update(deltaTime);

        if (!Opponent.Player.Alive)
            return;

        if (_attackWalker == null && _safetyWalker == null)
        {
            if (!_init)
            {
                _init = true;
                MoveToAttack();
            }
            else
                return;
        }

        if (_attackWalker is { IsStuck: true } or { IsFinished: true })
        {
            MoveToSafety();
        }

        if (_safetyWalker is { IsFinished: true })
        {
            // Wait for the bomb to fully explode
            if (
                _placedBombTile is { Detonated: false }
                || (
                    _placedBombPosition != null
                    && _state.TileMap.GetTile(_placedBombPosition) != null
                )
            )
            {
                return;
            }

            _placedBombTile = null;
            _placedBombPosition = null;
            MoveToAttack();
        }

        _attackWalker?.Update(deltaTime);
        _safetyWalker?.Update(deltaTime);
    }

    private GridPosition? GetNextTarget()
    {
        return _pathQueue.TryDequeue(out var target) ? target : null;
    }

    private void MoveToAttack()
    {
        _safetyWalker = null;

        List<GridPosition> path;
        try
        {
            path =
                _state.TileMap.ShortestPath(
                    Player.Position.ToGridPosition(),
                    Opponent.Player.Position.ToGridPosition(),
                    Player.Speed
                ) ?? throw new InvalidOperationException("Could not find a path to the opponent");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        path.RemoveAt(path.Count - 1);
        _pathQueue = new Queue<GridPosition>(path);
        _attackWalker = new Walker(Player, GetNextTarget);
    }

    private void MoveToSafety()
    {
        _attackWalker = null;

        try
        {
            _placedBombTile = Player.PlaceBomb();
            _placedBombPosition = _placedBombTile.Position;
        }
        catch
        {
            return;
        }

        var pathToSafety = GetSafetyPath(_placedBombTile);
        if (pathToSafety == null)
            return;

        _pathQueue = new Queue<GridPosition>(pathToSafety);
        _safetyWalker = new Walker(Player, GetNextTarget);
    }

    private List<GridPosition>? GetSafetyPath(BombTile bombTile)
    {
        var dangerousPositions = bombTile
            .ExplosionPaths.SelectMany(x => x)
            .Concat([bombTile.Position])
            .ToList();

        var queue = new Queue<GridPosition>();
        queue.Enqueue(bombTile.Position);
        var parents = new GridPosition?[_state.TileMap.Height, _state.TileMap.Width];

        GridPosition? current;
        while (queue.TryDequeue(out current))
        {
            if (!dangerousPositions.Contains(current))
                break;

            foreach (var neighbour in current.Neighbours)
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
