using System.Numerics;

namespace Bomberman.Core.Agents.Mcts;

public class MctsAgent : IUpdatable
{
    private readonly Player _player;
    public Vector2 Position => _player.Position;
    public bool Alive => _player.Alive;

    public MctsAgent(GridPosition startPosition, TileMap tileMap)
    {
        _player = new Player(startPosition, tileMap);
    }

    internal MctsAgent(MctsAgent original, TileMap tileMap)
    {
        _player = new Player(original._player, tileMap);
    }

    public void Update(TimeSpan deltaTime)
    {
        _player.Update(deltaTime);
    }

    public void ApplyAction(BombermanAction action)
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
            case BombermanAction.PlaceBomb:
                _player.PlaceBomb();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }
}
