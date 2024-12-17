using System;
using Bomberman.Core;
using Microsoft.Xna.Framework.Input;

namespace Bomberman.Desktop;

public class KeyboardPlayer(Player player) : IUpdatable
{
    private bool _spacePressed;

    public void Update(TimeSpan deltaTime)
    {
        if (player.Alive)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.W))
                player.SetMovingDirection(Direction.Up);
            else if (Keyboard.GetState().IsKeyDown(Keys.S))
                player.SetMovingDirection(Direction.Down);
            else if (Keyboard.GetState().IsKeyDown(Keys.A))
                player.SetMovingDirection(Direction.Left);
            else if (Keyboard.GetState().IsKeyDown(Keys.D))
                player.SetMovingDirection(Direction.Right);
            else
                player.SetMovingDirection(Direction.None);

            if (!_spacePressed && Keyboard.GetState().IsKeyDown(Keys.Space))
                _spacePressed = true;

            if (_spacePressed && Keyboard.GetState().IsKeyUp(Keys.Space))
            {
                try
                {
                    player.PlaceBomb();
                }
                catch (InvalidOperationException)
                {
                    // A player might try to place more bombs than they are allowed
                }
                _spacePressed = false;
            }
        }
    }
}
