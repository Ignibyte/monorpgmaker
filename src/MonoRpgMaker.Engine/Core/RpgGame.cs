using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoRpgMaker.Engine.Sim;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Engine.Core;

/// <summary>
/// The MonoGame host for a running project: it renders the active map of a <see cref="GameSession"/> through a
/// player-following <see cref="Camera"/> and drives it from keyboard input. The session owns the multi-map
/// switch (a Warp swaps the active map); the host always renders <see cref="GameSession.Active"/>. The host app
/// supplies the scene to run and (via <see cref="SetTileset"/>) its tileset art.
/// </summary>
/// <remarks>
/// Excluded from coverage: the composition/host root owning the live MonoGame loop
/// (window, GPU, input) with no unit-testable contract — the C# analogue of an
/// excluded <c>main</c>. The simulation it drives (<see cref="GameSession"/>/<see cref="WorldSim"/>) and the view
/// math (<see cref="Camera"/>, <see cref="Tileset"/>) are covered in their own tests.
/// </remarks>
[ExcludeFromCodeCoverage]
public class RpgGame : Game
{
    private const int TileSize = 32;
    private const int ViewportTilesWide = 20;
    private const int ViewportTilesHigh = 15;

    private readonly GraphicsDeviceManager _graphics;
    private readonly GameSession _session;
    private SpriteBatch? _spriteBatch;
    private Texture2D? _pixel;
    private Texture2D? _tileset;
    private Tileset? _tilesetInfo;
    private SpriteFont? _spriteFont;
    private KeyboardState _previous;
    private readonly TilesetTracker _tilesetTracker = new();

    /// <summary>Boot the host over the supplied <paramref name="session"/> (it owns the map set + transitions).</summary>
    public RpgGame(GameSession session)
    {
        _session = session;
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = ViewportTilesWide * TileSize,
            PreferredBackBufferHeight = ViewportTilesHigh * TileSize,
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    /// <summary>The simulation for the map currently being played — re-evaluated each frame so a warp re-renders.</summary>
    private WorldSim Sim => _session.Active;

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

    /// <summary>Supply the UI font (the host loads it from the content pipeline); text rendering degrades gracefully when absent.</summary>
    protected void SetFont(SpriteFont font) => _spriteFont = font;

    /// <summary>The catalog name of the active map's tileset — the host loads that sheet's art.</summary>
    protected string MapTilesetName => Sim.Map.TilesetName;

    /// <summary>Called when the active map's tileset changes (incl. the first frame) — the host reloads + applies the new sheet (D-0022; the engine never loads embedded resources itself).</summary>
    protected virtual void OnTilesetChanged(string tilesetName)
    {
    }

    /// <summary>The live session (map set + active sim) — the host reads it to save the game and restores it on load.</summary>
    protected GameSession Session => _session;

    /// <summary>Called when the player requests a save — the host persists the game (the engine never touches the filesystem — D-0022).</summary>
    protected virtual void OnSaveRequested()
    {
    }

    /// <summary>Called when the player requests a load — the host reads the save + restores the session.</summary>
    protected virtual void OnLoadRequested()
    {
    }

    /// <inheritdoc />
    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        if (Pressed(keyboard, Keys.Escape) && Sim.ActiveShop is null)
        {
            Exit();
        }

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

        // Re-skin when the active map's tileset changed (e.g. after a warp) — the host reloads the sheet (D-0022).
        if (_tilesetTracker.TryAdvance(MapTilesetName))
        {
            OnTilesetChanged(MapTilesetName);
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

        // Screen-fixed UI (the message banner + the shop modal) — drawn without the camera translation.
        _spriteBatch.Begin();
        DrawMessageBanner(_spriteBatch, _pixel);
        if (Sim.ActiveShop is not null)
        {
            DrawShopScreen(_spriteBatch, _pixel);
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private Point ViewOffset()
    {
        var playerPixel = new Point(
            (Sim.Player.Cell.X * TileSize) + (TileSize / 2),
            (Sim.Player.Cell.Y * TileSize) + (TileSize / 2));
        var viewport = new Point(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        var mapPixels = new Point(Sim.Map.Width * TileSize, Sim.Map.Height * TileSize);
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
        if (Sim.ActiveShop is not null)
        {
            HandleShopInput(keyboard);
            return;
        }

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

        if (Pressed(keyboard, Keys.F5))
            OnSaveRequested();
        else if (Pressed(keyboard, Keys.F9))
            OnLoadRequested();
    }

    // The shop modal owns input while open: arrows move the cursor, Enter/Space buys, S sells, Escape closes.
    private void HandleShopInput(KeyboardState keyboard)
    {
        ShopState? shop = Sim.ActiveShop;
        if (shop is null)
        {
            return;
        }

        if (Pressed(keyboard, Keys.Up))
            Sim.MoveShopCursor(-1);
        else if (Pressed(keyboard, Keys.Down))
            Sim.MoveShopCursor(1);

        if (Pressed(keyboard, Keys.Enter) || Pressed(keyboard, Keys.Space))
            Sim.Buy(shop.Cursor);
        else if (Pressed(keyboard, Keys.S))
            Sim.Sell(shop.Cursor);

        if (Pressed(keyboard, Keys.Escape))
            Sim.CloseShop();
    }

    private bool Pressed(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && _previous.IsKeyUp(key);

    private void Step(Direction direction)
    {
        _session.MovePlayer(direction);
        UpdateTitle();
    }

    private void Act()
    {
        _session.PressAction();
        UpdateTitle();
    }

    private void UpdateTitle()
    {
        if (Sim.CurrentMessage is { } message)
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
        foreach (var cell in Sim.EventCells)
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
        for (var y = 0; y < Sim.Map.Height; y++)
        {
            for (var x = 0; x < Sim.Map.Width; x++)
            {
                Tile tile = Sim.Map.GetTile(new Point(x, y));
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
        var cell = Sim.Player.Cell;
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
        if (Sim.CurrentMessage is null)
            return;

        var bannerHeight = TileSize;
        var width = GraphicsDevice.Viewport.Width;
        var top = GraphicsDevice.Viewport.Height - bannerHeight;
        batch.Draw(pixel, new Rectangle(0, top, width, bannerHeight), new Color(0, 0, 0, 200));
        batch.Draw(pixel, new Rectangle(0, top, width, 2), Color.Goldenrod);
        if (_spriteFont is not null)
        {
            batch.DrawString(_spriteFont, Sim.CurrentMessage, new Vector2(8, top + 6), Color.White);
        }
    }

    // The buy/sell modal: a centred panel listing the offers (a cursor on the selected row) + the player's gold.
    private void DrawShopScreen(SpriteBatch batch, Texture2D pixel)
    {
        ShopState? shop = Sim.ActiveShop;
        if (shop is null)
        {
            return;
        }

        var panel = new Rectangle(TileSize * 3, TileSize * 2, GraphicsDevice.Viewport.Width - (TileSize * 6), GraphicsDevice.Viewport.Height - (TileSize * 5));
        batch.Draw(pixel, panel, new Color(0, 0, 0, 220));
        batch.Draw(pixel, new Rectangle(panel.X, panel.Y, panel.Width, 2), Color.Goldenrod);

        if (_spriteFont is null)
        {
            return;
        }

        var x = panel.X + 12;
        var y = panel.Y + 10;
        batch.DrawString(_spriteFont, "Shop   (Enter buy, S sell, Esc close)", new Vector2(x, y), Color.Goldenrod);
        y += 28;
        for (var i = 0; i < shop.Offers.Count; i++)
        {
            ShopOffer offer = shop.Offers[i];
            string row = (i == shop.Cursor ? "> " : "  ") + offer.ItemId + "   buy " + offer.BuyPrice + "   sell " + offer.SellPrice;
            batch.DrawString(_spriteFont, row, new Vector2(x, y), i == shop.Cursor ? Color.White : Color.LightGray);
            y += 22;
        }

        batch.DrawString(_spriteFont, "Gold: " + Sim.State.GetCount("gold"), new Vector2(x, panel.Bottom - 28), Color.Gold);
    }
}
