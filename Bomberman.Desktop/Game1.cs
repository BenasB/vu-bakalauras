using System.Linq;
using Bomberman.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Bomberman.Desktop;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteFont _spriteFont;

    private readonly TileMap _tileMap = new(17, 9);
    private readonly Player _player;
    private Texture2D _floorTexture;
    private Texture2D _wallTexture;
    private Texture2D _playerTexture;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _player = new Player(
            new System.Numerics.Vector2(x: 3 * Constants.TileSize, y: 3 * Constants.TileSize),
            _tileMap
        );
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _spriteFont = Content.Load<SpriteFont>("MyTestFont");
        _floorTexture = Content.Load<Texture2D>("floor");
        _wallTexture = Content.Load<Texture2D>("wall");
        _playerTexture = Content.Load<Texture2D>("player");
    }

    protected override void Update(GameTime gameTime)
    {
        if (
            GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape)
        )
            Exit();

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

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();

        for (int row = 0; row < _tileMap.Width; row++)
        {
            for (int column = 0; column < _tileMap.Length; column++)
            {
                if (!_tileMap.BackgroundTiles[row][column])
                    continue;

                _spriteBatch.Draw(
                    _floorTexture,
                    new Vector2(column * Constants.TileSize, row * Constants.TileSize),
                    Color.White
                );
            }
        }

        for (int row = 0; row < _tileMap.Width; row++)
        {
            for (int column = 0; column < _tileMap.Length; column++)
            {
                if (!_tileMap.ForegroundTiles[row][column])
                    continue;

                _spriteBatch.Draw(
                    _wallTexture,
                    new Vector2(column * Constants.TileSize, row * Constants.TileSize),
                    Color.White
                );
            }
        }

        _spriteBatch.Draw(_playerTexture, _player.Position, Color.White);

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
