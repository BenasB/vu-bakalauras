using System.Numerics;
using Bomberman.Core.Utilities;

namespace Bomberman.Core.Agents.Mcts;

public class MctsAgent : IUpdatable
{
    private readonly GameState _state;
    private readonly Player _player;
    private readonly TileMap _tileMap;
    public Vector2 Position => _player.Position;
    public bool Alive => _player.Alive;

    public bool PerformMcts { get; set; }
    private CancellationTokenSource? _cts;

    private static readonly TimeSpan MctsInterval = TimeSpan.FromSeconds(1.0f / 3);
    private TimeSpan _elapsedTime = TimeSpan.Zero;

    public MctsAgent(GridPosition startPosition, TileMap tileMap, GameState state)
    {
        _tileMap = tileMap;
        _state = state;
        _player = new Player(startPosition, tileMap);
    }

    internal MctsAgent(MctsAgent original, TileMap tileMap, GameState state)
    {
        _tileMap = tileMap;
        _state = state;
        _player = new Player(original._player, tileMap);
    }

    public void Update(TimeSpan deltaTime)
    {
        if (!Alive)
            return;

        _player.Update(deltaTime);

        if (!PerformMcts)
            return;

        _elapsedTime += deltaTime;

        if (_elapsedTime < MctsInterval)
            return;

        if (_cts is { IsCancellationRequested: false })
            throw new InvalidOperationException("The last MCTS process was not cancelled");

        _cts = new CancellationTokenSource(MctsInterval - TimeSpan.FromMilliseconds(20));
        FindBestActionAsync(_cts.Token).ContinueWith(x => ApplyAction(x.Result));
        _elapsedTime = TimeSpan.Zero;
    }

    private Task<BombermanAction> FindBestActionAsync(CancellationToken cancellation)
    {
        return Task.Run(
            () =>
            {
                var root = new Node(_state);
                var iterations = 0;

                while (!cancellation.IsCancellationRequested)
                {
                    iterations++;
                    var selectedNode = root.Select();
                    var expandedNode = selectedNode.Expand();
                    float reward;
                    try
                    {
                        reward = expandedNode.Simulate();
                    }
                    catch
                    {
                        Logger.Warning("Something went wrong during simulation");
                        continue;
                    }
                    expandedNode.Backpropagate(reward);
                }

                var bestNode = root.Children.MaxBy(child => child.Visits);
                var bestAction =
                    bestNode?.Action
                    ?? throw new InvalidOperationException("Could not find the best action");

                Logger.Information($"Best action after ({iterations} iterations): {bestAction}");

                return bestAction;
            },
            cancellation
        );
    }

    internal void ApplyAction(BombermanAction action)
    {
        switch (action)
        {
            case BombermanAction.MoveUp:
                _player.SetMovingDirection(Direction.Up);
                break;
            case BombermanAction.MoveDown:
                _player.SetMovingDirection(Direction.Down);
                break;
            case BombermanAction.MoveLeft:
                _player.SetMovingDirection(Direction.Left);
                break;
            case BombermanAction.MoveRight:
                _player.SetMovingDirection(Direction.Right);
                break;
            case BombermanAction.Stand:
                _player.SetMovingDirection(Direction.None);
                break;
            // case BombermanAction.PlaceBomb:
            //     _player.PlaceBomb();
            //     break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

    internal IEnumerable<BombermanAction> GetPossibleActions()
    {
        var result = new List<BombermanAction> { BombermanAction.Stand };

        var gridPosition = Position.ToGridPosition();
        if (_tileMap.GetTile(gridPosition with { Row = gridPosition.Row - 1 }) == null)
            result.Add(BombermanAction.MoveUp);
        if (_tileMap.GetTile(gridPosition with { Row = gridPosition.Row + 1 }) == null)
            result.Add(BombermanAction.MoveDown);
        if (_tileMap.GetTile(gridPosition with { Column = gridPosition.Column - 1 }) == null)
            result.Add(BombermanAction.MoveLeft);
        if (_tileMap.GetTile(gridPosition with { Column = gridPosition.Column + 1 }) == null)
            result.Add(BombermanAction.MoveRight);

        return result;
    }
}
