using System;
using System.Threading.Tasks;
using Bomberman.Core;
using Bomberman.Core.MCTS;
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

    private readonly KeyboardPlayer? _keyboardPlayer;

    // TODO: Move to texturing component
    private Texture2D _floorTexture;
    private Texture2D _wallTexture;
    private Texture2D _playerTexture;
    private Texture2D _bombTexture;
    private Texture2D _explosionTexture;
    private Texture2D _boxTexture;
    private Texture2D _coinTexture;
    private Texture2D _fireUpTexture;
    private Texture2D _speedUpTexture;
    private Texture2D _bombUpTexture;
    private Texture2D _blankTexture;
    private Texture2D _debugGridMarkerTexture;

    public BombermanGame(BombermanGameOptions options)
    {
        // Disable throttling when the window is inactive
        InactiveSleepTime = TimeSpan.Zero;

        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _gameState = new GameState();

        if (options.Player == GamePlayer.Agent)
            _ = Task.Run(() => Agent.LoopMcts(_gameState, options.Export));
        else if (options.Player == GamePlayer.Keyboard)
            _keyboardPlayer = new KeyboardPlayer(_gameState.Player);
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
        _coinTexture = Content.Load<Texture2D>("coin");
        _fireUpTexture = Content.Load<Texture2D>("fireup");
        _speedUpTexture = Content.Load<Texture2D>("speedup");
        _bombUpTexture = Content.Load<Texture2D>("bombup");

        _blankTexture = new Texture2D(_graphics.GraphicsDevice, 1, 1);
        _blankTexture.SetData([Color.White]);

        _debugGridMarkerTexture = Content.Load<Texture2D>("debug_grid_marker");
    }

    protected override void Update(GameTime gameTime)
    {
        if (_gameState.Terminated)
            return;

        // if (gameTime.IsRunningSlowly)
        //     throw new InvalidOperationException(
        //         "Update takes more time than a frame has allocated to it, results are unexpected"
        //     );

        if (
            GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape)
        )
            Exit();

        _keyboardPlayer?.Update(gameTime.ElapsedGameTime);
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

        if (_gameState.Player.Alive)
        {
            _spriteBatch.Draw(_playerTexture, _gameState.Player.Position, Color.White);
            _spriteBatch.Draw(
                _debugGridMarkerTexture,
                (Vector2)_gameState.Player.Position.ToGridPosition(),
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
            CoinTile => _coinTexture,
            FireUpTile => _fireUpTexture,
            SpeedUpTile => _speedUpTexture,
            BombUpTile => _bombUpTexture,
            _ => throw new InvalidOperationException("Could not find a texture for the tile"),
        };
}
