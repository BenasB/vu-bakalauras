using System;
using System.Linq;
using Bomberman.Core;
using Bomberman.Core.Tiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Vector2 = System.Numerics.Vector2;

namespace Bomberman.Desktop;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteFont _spriteFont;

    private static readonly GridPosition StartPosition = new(Row: 3, Column: 3);
    private readonly TileMap _tileMap = new(17, 9, StartPosition);
    private readonly Player _player;

    // TODO: Move to input component
    private bool _spacePressed = false;

    // TODO: Move to texturing component
    private Texture2D _floorTexture;
    private Texture2D _wallTexture;
    private Texture2D _playerTexture;
    private Texture2D _bombTexture;
    private Texture2D _explosionTexture;
    private Texture2D _boxTexture;

    private Texture2D _debugGridMarkerTexture;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _player = new Player(StartPosition, _tileMap);
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

            _player.Update(gameTime.ElapsedGameTime);

            if (!_spacePressed && Keyboard.GetState().IsKeyDown(Keys.Space))
                _spacePressed = true;

            if (_spacePressed && Keyboard.GetState().IsKeyUp(Keys.Space))
            {
                _player.PlaceBomb();
                _spacePressed = false;
            }
        }

        _tileMap.Update(gameTime.ElapsedGameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();

        foreach (var tile in _tileMap.Tiles.Where(tile => tile != null).Select(tile => tile!))
        {
            _spriteBatch.Draw(GetTileTexture(tile), (Vector2)tile.Position, Color.White);
        }

        if (_player.Alive)
        {
            _spriteBatch.Draw(_playerTexture, _player.Position, Color.White);

#if DEBUG
            _spriteBatch.Draw(
                _debugGridMarkerTexture,
                (Vector2)_player.Position.ToGridPosition(),
                Color.White
            );
#endif
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
