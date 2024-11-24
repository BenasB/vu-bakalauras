using System;
using System.Linq;
using Bomberman.Core;
using Bomberman.Core.Tiles;
using Bomberman.Core.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Vector2 = System.Numerics.Vector2;

namespace Bomberman.Desktop;

public class BombermanGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteFont _spriteFont;

    private readonly KeyboardPlayer _keyboardPlayer;
    private readonly GameState _gameState = new();

    // TODO: Move to texturing component
    private Texture2D _floorTexture;
    private Texture2D _wallTexture;
    private Texture2D _playerTexture;
    private Texture2D _bombTexture;
    private Texture2D _explosionTexture;
    private Texture2D _boxTexture;

    private Texture2D _debugGridMarkerTexture;

    public BombermanGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _keyboardPlayer = new KeyboardPlayer(_gameState.TileMap);
    }

    protected override void Initialize()
    {
        _graphics.IsFullScreen = false;
        _graphics.PreferredBackBufferHeight = _gameState.TileMap.Width * Constants.TileSize;
        _graphics.PreferredBackBufferWidth = _gameState.TileMap.Length * Constants.TileSize;
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

        _debugGridMarkerTexture = Content.Load<Texture2D>("debug_grid_marker");
    }

    protected override void Update(GameTime gameTime)
    {
        if (
            GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape)
        )
            Exit();

        _keyboardPlayer.Update(gameTime.ElapsedGameTime);
        _gameState.Update(gameTime.ElapsedGameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();

        foreach (
            var tile in _gameState.TileMap.Tiles.Where(tile => tile != null).Select(tile => tile!)
        )
        {
            _spriteBatch.Draw(GetTileTexture(tile), (Vector2)tile.Position, Color.White);
        }

        if (_keyboardPlayer.Alive)
        {
            _spriteBatch.Draw(_playerTexture, _keyboardPlayer.Position, Color.White);

#if DEBUG
            _spriteBatch.Draw(
                _debugGridMarkerTexture,
                (Vector2)_keyboardPlayer.Position.ToGridPosition(),
                Color.Red
            );
#endif
        }

        if (_gameState.RandomAgent.Alive)
        {
            _spriteBatch.Draw(_playerTexture, _gameState.RandomAgent.Position, Color.GreenYellow);

#if DEBUG
            if (_gameState.RandomAgent.CurrentPath != null)
            {
                foreach (var pathPosition in _gameState.RandomAgent.CurrentPath)
                {
                    _spriteBatch.Draw(_debugGridMarkerTexture, (Vector2)pathPosition, Color.Green);
                }
            }
#endif
        }

        if (_gameState.MctsAgent.Alive)
        {
            _spriteBatch.Draw(_playerTexture, _gameState.MctsAgent.Position, Color.Magenta);
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
