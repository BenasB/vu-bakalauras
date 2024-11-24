using System;
using System.Numerics;
using Bomberman.Core;
using Microsoft.Xna.Framework.Input;

namespace Bomberman.Desktop;

public class KeyboardPlayer(TileMap tileMap) : IUpdatable
{
    private readonly Player _player = new(new GridPosition(5, 13), tileMap);

    private bool _spacePressed;

    public bool Alive => _player.Alive;

    public Vector2 Position => _player.Position;

    public void Update(TimeSpan deltaTime)
    {
        if (_player.Alive)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.W))
                _player.SetMovingDirection(Direction.Up);
            else if (Keyboard.GetState().IsKeyDown(Keys.S))
                _player.SetMovingDirection(Direction.Down);
            else if (Keyboard.GetState().IsKeyDown(Keys.A))
                _player.SetMovingDirection(Direction.Left);
            else if (Keyboard.GetState().IsKeyDown(Keys.D))
                _player.SetMovingDirection(Direction.Right);
            else
                _player.SetMovingDirection(Direction.None);

            _player.Update(deltaTime);

            if (!_spacePressed && Keyboard.GetState().IsKeyDown(Keys.Space))
                _spacePressed = true;

            if (_spacePressed && Keyboard.GetState().IsKeyUp(Keys.Space))
            {
                try
                {
                    _player.PlaceBomb();
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
