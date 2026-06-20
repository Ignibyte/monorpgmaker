using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoRpgMaker.Engine.Sim;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Engine.Core;

/// <summary>
/// The MonoGame host for a running project: it renders a <see cref="WorldSim"/> through a player-following
/// <see cref="Camera"/> and drives it from keyboard input. The host app supplies the scene to run and (via
/// <see cref="SetTileset"/>) its tileset art.
/// </summary>
/// <remarks>
/// Excluded from coverage: the composition/host root owning the live MonoGame loop
/// (window, GPU, input) with no unit-testable contract — the C# analogue of an
/// excluded <c>main</c>. The simulation it drives (<see cref="WorldSim"/>) and the view
/// math (<see cref="Camera"/>, <see cref="Tileset"/>) are covered in their own tests.
/// </remarks>
[ExcludeFromCodeCoverage]
public class RpgGame : Game
{
    private const int TileSize = 32;
    private const int ViewportTilesWide = 20;
    private const int ViewportTilesHigh = 15;

    private readonly GraphicsDeviceManager _graphics;
    private readonly WorldSim _sim;
    private SpriteBatch? _spriteBatch;
    private Texture2D? _pixel;
    private Texture2D? _tileset;
    private Tileset? _tilesetInfo;
    private KeyboardState _previous;

    /// <summary>Boot the host over the supplied simulation <paramref name="sim"/>.</summary>
    public RpgGame(WorldSim sim)
    {
        _sim = sim;
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = ViewportTilesWide * TileSize,
            PreferredBackBufferHeight = ViewportTilesHigh * TileSize,
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

    /// <summary>Supply the tileset sheet + its geometry; the map then renders as sprites (the host owns its art).</summary>
    protected void SetTileset(Texture2D texture, Tileset info)
    {
        _tileset = texture;
        _tilesetInfo = info;
    }

    /// <summary>The catalog name of the running map's tileset — the host loads that sheet's art (the chosen sheet, not a hardcoded one).</summary>
    protected string MapTilesetName => _sim.Map.TilesetName;

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

        Point offset = ViewOffset();

        // The world (map + player) under the camera translation, point-sampled for crisp pixel art.
        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: Matrix.CreateTranslation(-offset.X, -offset.Y, 0));
        DrawMap(_spriteBatch, _pixel);
        DrawEventMarkers(_spriteBatch, _pixel);
        DrawPlayer(_spriteBatch, _pixel);
        _spriteBatch.End();

        // Screen-fixed UI (the message banner) — drawn without the camera translation.
        _spriteBatch.Begin();
        DrawMessageBanner(_spriteBatch, _pixel);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private Point ViewOffset()
    {
        var playerPixel = new Point(
            (_sim.Player.Cell.X * TileSize) + (TileSize / 2),
            (_sim.Player.Cell.Y * TileSize) + (TileSize / 2));
        var viewport = new Point(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        var mapPixels = new Point(_sim.Map.Width * TileSize, _sim.Map.Height * TileSize);
        return Camera.ViewOffset(playerPixel, viewport, mapPixels);
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

        if (Pressed(keyboard, Keys.Space) || Pressed(keyboard, Keys.Enter))
            Act();
    }

    private bool Pressed(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && _previous.IsKeyUp(key);

    private void Step(Direction direction)
    {
        _sim.MovePlayer(direction);
        UpdateTitle();
    }

    private void Act()
    {
        _sim.PressAction();
        UpdateTitle();
    }

    private void UpdateTitle()
    {
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

    private void DrawEventMarkers(SpriteBatch batch, Texture2D pixel)
    {
        foreach (var cell in _sim.EventCells)
        {
            var inset = TileSize / 4;
            var rect = new Rectangle(
                (cell.X * TileSize) + inset,
                (cell.Y * TileSize) + inset,
                TileSize - (2 * inset),
                TileSize - (2 * inset));
            batch.Draw(pixel, rect, Color.Gold);
        }
    }

    private void DrawMap(SpriteBatch batch, Texture2D pixel)
    {
        for (var y = 0; y < _sim.Map.Height; y++)
        {
            for (var x = 0; x < _sim.Map.Width; x++)
            {
                Tile tile = _sim.Map.GetTile(new Point(x, y));
                if (_tileset is not null && _tilesetInfo is not null)
                {
                    if (_tilesetInfo.TryGetSourceRect(tile.TilesetId, out SourceRect sr))
                    {
                        batch.Draw(
                            _tileset,
                            new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize),
                            new Rectangle(sr.X, sr.Y, sr.Width, sr.Height),
                            Color.White);
                    }
                }
                else
                {
                    batch.Draw(pixel, new Rectangle(x * TileSize, y * TileSize, TileSize - 1, TileSize - 1), ColorFor(tile));
                }
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
        var width = GraphicsDevice.Viewport.Width;
        var top = GraphicsDevice.Viewport.Height - bannerHeight;
        batch.Draw(pixel, new Rectangle(0, top, width, bannerHeight), new Color(0, 0, 0, 200));
        batch.Draw(pixel, new Rectangle(0, top, width, 2), Color.Goldenrod);
    }
}
