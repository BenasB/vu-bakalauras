using System;
using System.Collections.Generic;
using Bomberman.Core.Agents;
using Microsoft.Xna.Framework.Input;

namespace Bomberman.Desktop;

public static class KeyboardPresets
{
    public static Predicate<KeyboardAgent.Key> WasdPreset => GetIsKeyDown(WasdMapping);
    public static Predicate<KeyboardAgent.Key> ArrowsPreset => GetIsKeyDown(ArrowsMapping);

    private static readonly Dictionary<KeyboardAgent.Key, Keys> WasdMapping = new()
    {
        { KeyboardAgent.Key.Up, Keys.W },
        { KeyboardAgent.Key.Down, Keys.S },
        { KeyboardAgent.Key.Left, Keys.A },
        { KeyboardAgent.Key.Right, Keys.D },
        { KeyboardAgent.Key.PlaceBomb, Keys.Space },
    };

    private static readonly Dictionary<KeyboardAgent.Key, Keys> ArrowsMapping = new()
    {
        { KeyboardAgent.Key.Up, Keys.Up },
        { KeyboardAgent.Key.Down, Keys.Down },
        { KeyboardAgent.Key.Left, Keys.Left },
        { KeyboardAgent.Key.Right, Keys.Right },
        { KeyboardAgent.Key.PlaceBomb, Keys.Enter },
    };

    private static Predicate<KeyboardAgent.Key> GetIsKeyDown(
        Dictionary<KeyboardAgent.Key, Keys> preset
    )
    {
        return key => Keyboard.GetState().IsKeyDown(preset[key]);
    }
}
