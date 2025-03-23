using System;
using System.Collections.Generic;
using Bomberman.Core;
using Bomberman.Core.Agents;
using Bomberman.Core.Tiles;
using Bomberman.Core.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Vector2 = System.Numerics.Vector2;

namespace Bomberman.Desktop;

internal class BombermanGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteFont _spriteFont;

    private readonly GameState _gameState;
    private readonly List<KeyboardPlayer> _keyboardPlayers = [];

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
        // Disable throttling when the window is inactive
        InactiveSleepTime = TimeSpan.Zero;

        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _gameState = new GameState(
            GetAgentFactory(options.PlayerOne, 1),
            GetAgentFactory(options.PlayerTwo, 2)
        );
        return;

        Func<GameState, Player, Agent> GetAgentFactory(PlayerType type, int playerNumber) =>
            type switch
            {
                PlayerType.Static => (_, player) => new StaticAgent(player),
                PlayerType.Walking => (state, player) => new WalkingAgent(state, player),
                PlayerType.Keyboard => (_, player) =>
                {
                    var keyboardPlayer = new KeyboardPlayer(
                        player,
                        playerNumber switch
                        {
                            1 => KeyboardPlayer.KeyPreset.Wasd,
                            2 => KeyboardPlayer.KeyPreset.Arrows,
                            _ => throw new InvalidOperationException(
                                $"There isn't any key preset assigned to player number {playerNumber}"
                            ),
                        }
                    );

                    _keyboardPlayers.Add(keyboardPlayer);
                    return new StaticAgent(player);
                },
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

        foreach (var keyboardPlayer in _keyboardPlayers)
            keyboardPlayer.Update(gameTime.ElapsedGameTime);
        _gameState.Update(gameTime.ElapsedGameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();

        foreach (var tile in _gameState.TileMap.Tiles)
        {
            _spriteBatch.Draw(GetTileTexture(tile), (Vector2)tile.Position, Color.White);
        }

        if (_gameState.AgentOne.Player.Alive)
        {
            _spriteBatch.Draw(_playerTexture, _gameState.AgentOne.Player.Position, Color.White);
            _spriteBatch.Draw(
                _debugGridMarkerTexture,
                (Vector2)_gameState.AgentOne.Player.Position.ToGridPosition(),
                Color.Navy
            );
        }

        if (_gameState.AgentTwo.Player.Alive)
        {
            _spriteBatch.Draw(_playerTexture, _gameState.AgentTwo.Player.Position, Color.White);
            _spriteBatch.Draw(
                _debugGridMarkerTexture,
                (Vector2)_gameState.AgentTwo.Player.Position.ToGridPosition(),
                Color.Navy
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
