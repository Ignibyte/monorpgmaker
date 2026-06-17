using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoRpgMaker.Engine.Sim;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Engine.Core;

/// <summary>
/// The MonoGame host for a running project: it renders a <see cref="WorldSim"/> and
/// drives it from keyboard input. The host app supplies the scene to run.
/// </summary>
/// <remarks>
/// Excluded from coverage: the composition/host root owning the live MonoGame loop
/// (window, GPU, input) with no unit-testable contract — the C# analogue of an
/// excluded <c>main</c>. The simulation it drives (<see cref="WorldSim"/> and its
/// neighbours) is covered in its own tests.
/// </remarks>
[ExcludeFromCodeCoverage]
public class RpgGame : Game
{
    private const int TileSize = 32;

    private readonly GraphicsDeviceManager _graphics;
    private readonly WorldSim _sim;
    private SpriteBatch? _spriteBatch;
    private Texture2D? _pixel;
    private KeyboardState _previous;

    /// <summary>Boot the host over the supplied simulation <paramref name="sim"/>.</summary>
    public RpgGame(WorldSim sim)
    {
        _sim = sim;
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = sim.Map.Width * TileSize,
            PreferredBackBufferHeight = sim.Map.Height * TileSize,
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    /// <inheritdoc />
    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    /// <inheritdoc />
    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        if (keyboard.IsKeyDown(Keys.Escape))
            Exit();

        HandleMovement(keyboard);
        _previous = keyboard;
        base.Update(gameTime);
    }

    /// <inheritdoc />
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(20, 22, 28));
        if (_spriteBatch is null || _pixel is null)
        {
            base.Draw(gameTime);
            return;
        }

        _spriteBatch.Begin();
        DrawMap(_spriteBatch, _pixel);
        DrawPlayer(_spriteBatch, _pixel);
        DrawMessageBanner(_spriteBatch, _pixel);
        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private static Color ColorFor(Tile tile) => tile.TilesetId switch
    {
        0 => new Color(40, 44, 52),   // floor
        1 => new Color(70, 60, 50),   // wall
        2 => Color.Goldenrod,         // lever
        3 => new Color(150, 40, 40),  // door closed
        4 => new Color(60, 140, 70),  // door open
        5 => new Color(150, 110, 40), // chest closed
        6 => new Color(90, 80, 55),   // chest open (emptied)
        _ => Color.Black,
    };

    private void HandleMovement(KeyboardState keyboard)
    {
        if (Pressed(keyboard, Keys.Up))
            Step(Direction.Up);
        else if (Pressed(keyboard, Keys.Down))
            Step(Direction.Down);
        else if (Pressed(keyboard, Keys.Left))
            Step(Direction.Left);
        else if (Pressed(keyboard, Keys.Right))
            Step(Direction.Right);
    }

    private bool Pressed(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && _previous.IsKeyUp(key);

    private void Step(Direction direction)
    {
        _sim.MovePlayer(direction);
        if (_sim.CurrentMessage is { } message)
        {
            Window.Title = $"monorpgmaker — {message}";
            Debug.WriteLine(message);
        }
        else
        {
            Window.Title = "monorpgmaker";
        }
    }

    private void DrawMap(SpriteBatch batch, Texture2D pixel)
    {
        for (var y = 0; y < _sim.Map.Height; y++)
        {
            for (var x = 0; x < _sim.Map.Width; x++)
            {
                var color = ColorFor(_sim.Map.GetTile(new Point(x, y)));
                batch.Draw(pixel, new Rectangle(x * TileSize, y * TileSize, TileSize - 1, TileSize - 1), color);
            }
        }
    }

    private void DrawPlayer(SpriteBatch batch, Texture2D pixel)
    {
        var cell = _sim.Player.Cell;
        var inset = TileSize / 6;
        var rect = new Rectangle(
            (cell.X * TileSize) + inset,
            (cell.Y * TileSize) + inset,
            TileSize - (2 * inset),
            TileSize - (2 * inset));
        batch.Draw(pixel, rect, Color.CornflowerBlue);
    }

    private void DrawMessageBanner(SpriteBatch batch, Texture2D pixel)
    {
        if (_sim.CurrentMessage is null)
            return;

        var bannerHeight = TileSize;
        var width = _sim.Map.Width * TileSize;
        var top = (_sim.Map.Height * TileSize) - bannerHeight;
        batch.Draw(pixel, new Rectangle(0, top, width, bannerHeight), new Color(0, 0, 0, 200));
        batch.Draw(pixel, new Rectangle(0, top, width, 2), Color.Goldenrod);
    }
}
