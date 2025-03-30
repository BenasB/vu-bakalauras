using System;
using System.Linq;
using Bomberman.Core;
using Bomberman.Core.Agents;
using Bomberman.Core.Agents.MCTS;
using Bomberman.Core.Tiles;
using Bomberman.Core.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Vector2 = System.Numerics.Vector2;

namespace Bomberman.Desktop;

internal class BombermanGame : Game
{
    private readonly BombermanGameOptions _options;
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteFont _spriteFont;

    private readonly GameState _gameState;

    // TODO: Move to texturing component
    private Texture2D _floorTexture;
    private Texture2D _wallTexture;
    private Texture2D _playerTexture;
    private Texture2D _bombTexture;
    private Texture2D _explosionTexture;
    private Texture2D _boxTexture;
    private Texture2D _blankTexture;
    private Texture2D _debugGridMarkerTexture;

    private static readonly TimeSpan RunningSlowThreshold = TimeSpan.FromMilliseconds(500);
    private TimeSpan _runningSlowAccumulator = TimeSpan.Zero;

    public BombermanGame(BombermanGameOptions options)
    {
        _options = options;

        // Disable throttling when the window is inactive
        InactiveSleepTime = TimeSpan.Zero;

        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _gameState = new GameState(CreateAgent);
    }

    private Agent CreateAgent(GameState state, Player player, int agentIndex)
    {
        var playerType = agentIndex switch
        {
            0 => _options.PlayerOne,
            1 => _options.PlayerTwo,
            _ => throw new InvalidOperationException("Only 2 players are supported."),
        };

        return playerType switch
        {
            PlayerType.Static => new StaticAgent(player, agentIndex),
            PlayerType.Walking => new WalkingAgent(state, player, agentIndex),
            PlayerType.Keyboard => new KeyboardAgent(
                player,
                agentIndex,
                agentIndex switch
                {
                    0 => KeyboardPresets.WasdPreset,
                    1 => KeyboardPresets.ArrowsPreset,
                    _ => throw new InvalidOperationException(
                        "Only 2 keyboard presets are supported."
                    ),
                }
            ),
            PlayerType.Mcts => new MctsAgent(state, player, agentIndex, _options.Export),
            _ => throw new NotSupportedException("This player type is not supported yet"),
        };
    }

    protected override void Initialize()
    {
        _graphics.IsFullScreen = false;
        _graphics.PreferredBackBufferHeight = _gameState.TileMap.Height * Constants.TileSize;
        _graphics.PreferredBackBufferWidth = _gameState.TileMap.Width * Constants.TileSize;
        _graphics.ApplyChanges();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _spriteFont = Content.Load<SpriteFont>("MyTestFont");
        _floorTexture = Content.Load<Texture2D>("floor");
        _wallTexture = Content.Load<Texture2D>("wall");
        _playerTexture = Content.Load<Texture2D>("player");
        _bombTexture = Content.Load<Texture2D>("bomb");
        _explosionTexture = Content.Load<Texture2D>("explosion");
        _boxTexture = Content.Load<Texture2D>("box");

        _blankTexture = new Texture2D(_graphics.GraphicsDevice, 1, 1);
        _blankTexture.SetData([Color.White]);

        _debugGridMarkerTexture = Content.Load<Texture2D>("debug_grid_marker");
    }

    protected override void Update(GameTime gameTime)
    {
        if (_gameState.Terminated)
            return;

        if (gameTime.IsRunningSlowly)
        {
            _runningSlowAccumulator += gameTime.ElapsedGameTime;

            if (_runningSlowAccumulator >= RunningSlowThreshold)
            {
                throw new InvalidOperationException(
                    "Update takes more time than a frame has allocated to it, results are unexpected"
                );
            }
        }
        else
        {
            _runningSlowAccumulator = TimeSpan.Zero;
        }

        if (
            GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape)
        )
            Exit();

        _gameState.Update(gameTime.ElapsedGameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();

        foreach (var tile in _gameState.TileMap.BackgroundTiles.SelectMany(r => r))
        {
            _spriteBatch.Draw(GetTileTexture(tile), (Vector2)tile.Position, Color.White);
        }

        foreach (var tile in _gameState.TileMap.ForegroundTiles.SelectMany(r => r).OfType<Tile>())
        {
            _spriteBatch.Draw(GetTileTexture(tile), (Vector2)tile.Position, Color.White);
        }

        for (int i = 0; i < _gameState.Agents.Length; i++)
        {
            if (!_gameState.Agents[i].Player.Alive)
                continue;

            var tint = i switch
            {
                0 => Color.Green,
                1 => Color.Red,
                _ => Color.White,
            };

            _spriteBatch.Draw(_playerTexture, _gameState.Agents[i].Player.Position, tint);
            _spriteBatch.Draw(
                _debugGridMarkerTexture,
                (Vector2)_gameState.Agents[i].Player.Position.ToGridPosition(),
                tint
            );
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private Texture2D GetTileTexture(Tile tile) =>
        tile switch
        {
            FloorTile => _floorTexture,
            WallTile => _wallTexture,
            BombTile => _bombTexture,
            ExplosionTile => _explosionTexture,
            BoxTile => _boxTexture,
            _ => throw new InvalidOperationException("Could not find a texture for the tile"),
        };
}
