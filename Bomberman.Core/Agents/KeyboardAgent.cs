namespace Bomberman.Core.Agents;

public class KeyboardAgent : Agent
{
    public enum Key
    {
        Up,
        Down,
        Left,
        Right,
        PlaceBomb,
    }

    private readonly Predicate<Key> _isKeyPressed;
    private bool _bombPlacementKeyPressed;

    public KeyboardAgent(Player player, int agentIndex, Predicate<Key> isKeyPressed)
        : base(player, agentIndex)
    {
        _isKeyPressed = isKeyPressed;
    }

    internal override Agent Clone(GameState state, Player player) =>
        new KeyboardAgent(player, AgentIndex, _isKeyPressed);

    public override void Update(TimeSpan deltaTime)
    {
        base.Update(deltaTime);

        if (_isKeyPressed.Invoke(Key.Up))
            Player.SetMovingDirection(Direction.Up);
        else if (_isKeyPressed.Invoke(Key.Down))
            Player.SetMovingDirection(Direction.Down);
        else if (_isKeyPressed.Invoke(Key.Left))
            Player.SetMovingDirection(Direction.Left);
        else if (_isKeyPressed.Invoke(Key.Right))
            Player.SetMovingDirection(Direction.Right);
        else
            Player.SetMovingDirection(Direction.None);

        if (!_bombPlacementKeyPressed && _isKeyPressed.Invoke(Key.PlaceBomb))
        {
            try
            {
                Player.PlaceBomb();
            }
            catch (InvalidOperationException)
            {
                // A player might try to place more bombs than they are allowed
            }
            _bombPlacementKeyPressed = true;
        }

        if (_bombPlacementKeyPressed && !_isKeyPressed.Invoke(Key.PlaceBomb))
        {
            _bombPlacementKeyPressed = false;
        }
    }
}
