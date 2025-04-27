using Bomberman.Core.Tiles;
using Bomberman.Core.Utilities;

namespace Bomberman.Core.Agents.MCTS;

public class MctsAgent : Agent
{
    internal double MaxDistance { get; }

    private readonly GameState _state;
    private readonly Random _rnd = new();
    private readonly MctsRunner? _mctsRunner;

    public MctsAgent(GameState state, Player player, int agentIndex, MctsAgentOptions options)
        : base(player, agentIndex)
    {
        _mctsRunner = new MctsRunner(state, this, options);
        _state = state;
        MaxDistance = state.TileMap.MaxDistance(Player.Speed);
    }

    private MctsAgent(GameState state, Player player, MctsAgent original)
        : base(player, original.AgentIndex)
    {
        MaxDistance = original.MaxDistance;
        _state = state;
    }

    internal override Agent Clone(GameState state, Player player) =>
        new MctsAgent(state, player, this);

    public override void Update(TimeSpan deltaTime)
    {
        _mctsRunner?.Update(deltaTime);

        base.Update(deltaTime);
    }

    internal void ApplyAction(BombermanAction action)
    {
        switch (action)
        {
            case BombermanAction.MoveUp:
                Player.SetMovingDirection(Direction.Up);
                break;
            case BombermanAction.MoveDown:
                Player.SetMovingDirection(Direction.Down);
                break;
            case BombermanAction.MoveLeft:
                Player.SetMovingDirection(Direction.Left);
                break;
            case BombermanAction.MoveRight:
                Player.SetMovingDirection(Direction.Right);
                break;
            case BombermanAction.Stand:
                Player.SetMovingDirection(Direction.None);
                break;
            case BombermanAction.PlaceBombAndMoveUp:
                Player.PlaceBomb();
                Player.SetMovingDirection(Direction.Up);
                break;
            case BombermanAction.PlaceBombAndMoveDown:
                Player.PlaceBomb();
                Player.SetMovingDirection(Direction.Down);
                break;
            case BombermanAction.PlaceBombAndMoveLeft:
                Player.PlaceBomb();
                Player.SetMovingDirection(Direction.Left);
                break;
            case BombermanAction.PlaceBombAndMoveRight:
                Player.PlaceBomb();
                Player.SetMovingDirection(Direction.Right);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

    internal IEnumerable<BombermanAction> GetPossibleActions()
    {
        var result = new List<BombermanAction> { BombermanAction.Stand };

        var gridPosition = Player.Position.ToGridPosition();

        var canPlaceBomb = Player.CanPlaceBomb && _state.TileMap.GetTile(gridPosition) is null;

        if (
            _state.TileMap.GetTile(gridPosition with { Row = gridPosition.Row - 1 })
            is null
                or IEnterable
        )
        {
            result.Add(BombermanAction.MoveUp);

            if (canPlaceBomb)
                result.Add(BombermanAction.PlaceBombAndMoveUp);
        }

        if (
            _state.TileMap.GetTile(gridPosition with { Row = gridPosition.Row + 1 })
            is null
                or IEnterable
        )
        {
            result.Add(BombermanAction.MoveDown);

            if (canPlaceBomb)
                result.Add(BombermanAction.PlaceBombAndMoveDown);
        }

        if (
            _state.TileMap.GetTile(gridPosition with { Column = gridPosition.Column - 1 })
            is null
                or IEnterable
        )
        {
            result.Add(BombermanAction.MoveLeft);

            if (canPlaceBomb)
                result.Add(BombermanAction.PlaceBombAndMoveLeft);
        }

        if (
            _state.TileMap.GetTile(gridPosition with { Column = gridPosition.Column + 1 })
            is null
                or IEnterable
        )
        {
            result.Add(BombermanAction.MoveRight);

            if (canPlaceBomb)
                result.Add(BombermanAction.PlaceBombAndMoveRight);
        }

        return result;
    }

    internal BombermanAction GetSimulationAction()
    {
        var possibilities = new List<BombermanAction> { BombermanAction.Stand };

        var gridPosition = Player.Position.ToGridPosition();

        var opponent = _state.Agents.First(a => a != this).Player;
        var opponentGridPosition = opponent.Position.ToGridPosition();

        var shouldPlaceBomb =
            _state.TileMap.GetTile(gridPosition) is null
            && IsPromisingBombPosition(gridPosition, opponentGridPosition);

        var actionsPerDirection = new[]
        {
            (BombermanAction.MoveUp, BombermanAction.PlaceBombAndMoveUp),
            (BombermanAction.MoveDown, BombermanAction.PlaceBombAndMoveDown),
            (BombermanAction.MoveLeft, BombermanAction.PlaceBombAndMoveLeft),
            (BombermanAction.MoveRight, BombermanAction.PlaceBombAndMoveRight),
        };
        foreach (var (action, equivalentMovementAction) in actionsPerDirection)
        {
            if (!IsTileSafeToWalk(GetGridPositionAfterAction(gridPosition, action)))
                continue;

            possibilities.Add(action);

            if (Player.CanPlaceBomb && shouldPlaceBomb)
                possibilities.Add(equivalentMovementAction);
        }

        return possibilities[_rnd.Next(0, possibilities.Count)];

        bool IsTileSafeToWalk(GridPosition position) =>
            _state.TileMap.GetTile(position) is null or (IEnterable and not ExplosionTile);
    }

    internal double CalculateSimulationHeuristic()
    {
        var opponentPosition = _state.Agents.First(a => a != this).Player.Position.ToGridPosition();
        var playerPosition = Player.Position.ToGridPosition();

        var distance = _state.TileMap.Distance(playerPosition, opponentPosition, Player.Speed);

        var distanceScore = 1 - Math.Clamp(distance / MaxDistance, 0, 1);

        var wastedBombsPenalty =
            -0.1
            * Player.ActiveBombs.Count(bombTile =>
                !IsPromisingBombPosition(bombTile.Position, opponentPosition)
            );

        return Math.Clamp(distanceScore + wastedBombsPenalty, 0, 1);
    }

    /// <summary>
    /// Does not take into account physics, just calculates the grid position
    /// </summary>
    internal static GridPosition GetGridPositionAfterAction(
        GridPosition position,
        BombermanAction action
    ) =>
        action switch
        {
            BombermanAction.MoveUp or BombermanAction.PlaceBombAndMoveUp => position with
            {
                Row = position.Row - 1,
            },
            BombermanAction.MoveDown or BombermanAction.PlaceBombAndMoveDown => position with
            {
                Row = position.Row + 1,
            },
            BombermanAction.MoveLeft or BombermanAction.PlaceBombAndMoveLeft => position with
            {
                Column = position.Column - 1,
            },
            BombermanAction.MoveRight or BombermanAction.PlaceBombAndMoveRight => position with
            {
                Column = position.Column + 1,
            },
            BombermanAction.Stand => position with { },
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
        };

    // TODO: Take into account the bomb's range
    private bool IsPromisingBombPosition(
        GridPosition bombPosition,
        GridPosition opponentPosition
    ) =>
        bombPosition with { Row = bombPosition.Row - 1 } == opponentPosition
        || bombPosition with { Row = bombPosition.Row + 1 } == opponentPosition
        || bombPosition with { Column = bombPosition.Column - 1 } == opponentPosition
        || bombPosition with { Column = bombPosition.Column + 1 } == opponentPosition
        || _state.TileMap.GetTile(bombPosition with { Row = bombPosition.Row - 1 }) is BoxTile
        || _state.TileMap.GetTile(bombPosition with { Row = bombPosition.Row + 1 }) is BoxTile
        || _state.TileMap.GetTile(bombPosition with { Column = bombPosition.Column - 1 }) is BoxTile
        || _state.TileMap.GetTile(bombPosition with { Column = bombPosition.Column + 1 })
            is BoxTile;
}
