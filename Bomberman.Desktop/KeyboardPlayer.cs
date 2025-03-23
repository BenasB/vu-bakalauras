using System;
using System.Collections.Generic;
using Bomberman.Core;
using Microsoft.Xna.Framework.Input;

namespace Bomberman.Desktop;

public class KeyboardPlayer(Player player, KeyboardPlayer.KeyPreset preset) : IUpdatable
{
    public enum KeyPreset
    {
        Wasd,
        Arrows,
    }

    private enum Action
    {
        Up,
        Down,
        Left,
        Right,
        PlaceBomb,
    }

    private static readonly Dictionary<Action, Keys> WasdPreset = new()
    {
        { Action.Up, Keys.W },
        { Action.Down, Keys.S },
        { Action.Left, Keys.A },
        { Action.Right, Keys.D },
        { Action.PlaceBomb, Keys.Space },
    };

    private static readonly Dictionary<Action, Keys> ArrowsPreset = new()
    {
        { Action.Up, Keys.Up },
        { Action.Down, Keys.Down },
        { Action.Left, Keys.Left },
        { Action.Right, Keys.Right },
        { Action.PlaceBomb, Keys.Enter },
    };

    private static readonly Dictionary<KeyPreset, Dictionary<Action, Keys>> Presets = new()
    {
        { KeyPreset.Wasd, WasdPreset },
        { KeyPreset.Arrows, ArrowsPreset },
    };

    private readonly Dictionary<Action, Keys> _activePreset = Presets[preset];
    private bool _bombPlacementKeyPressed;

    public void Update(TimeSpan deltaTime)
    {
        if (player.Alive)
        {
            if (Keyboard.GetState().IsKeyDown(_activePreset[Action.Up]))
                player.SetMovingDirection(Direction.Up);
            else if (Keyboard.GetState().IsKeyDown(_activePreset[Action.Down]))
                player.SetMovingDirection(Direction.Down);
            else if (Keyboard.GetState().IsKeyDown(_activePreset[Action.Left]))
                player.SetMovingDirection(Direction.Left);
            else if (Keyboard.GetState().IsKeyDown(_activePreset[Action.Right]))
                player.SetMovingDirection(Direction.Right);
            else
                player.SetMovingDirection(Direction.None);

            if (
                !_bombPlacementKeyPressed
                && Keyboard.GetState().IsKeyDown(_activePreset[Action.PlaceBomb])
            )
                _bombPlacementKeyPressed = true;

            if (
                _bombPlacementKeyPressed
                && Keyboard.GetState().IsKeyUp(_activePreset[Action.PlaceBomb])
            )
            {
                try
                {
                    player.PlaceBomb();
                }
                catch (InvalidOperationException)
                {
                    // A player might try to place more bombs than they are allowed
                }
                _bombPlacementKeyPressed = false;
            }
        }
    }
}
