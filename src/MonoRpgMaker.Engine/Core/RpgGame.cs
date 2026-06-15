using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoRpgMaker.Engine.Entities;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Engine.Core;

/// <summary>
/// The MonoGame host for a running project. The Player app hosts this; the editor
/// previews maps through the same engine types. Game-specific behaviour is layered
/// on by overriding the MonoGame lifecycle hooks.
/// </summary>
public class RpgGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;

    /// <summary>Boot the engine with a placeholder map and a single player actor.</summary>
    public RpgGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Map = new TileMap(20, 15);
        Player = new Actor("Hero", new Point(10, 7), maxHp: 30);
    }

    /// <summary>The map currently being walked.</summary>
    public TileMap Map { get; }

    /// <summary>The party leader the input drives.</summary>
    public Actor Player { get; }

    /// <inheritdoc />
    protected override void LoadContent() => _spriteBatch = new SpriteBatch(GraphicsDevice);

    /// <inheritdoc />
    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        if (keyboard.IsKeyDown(Keys.Escape))
            Exit();

        HandleMovement(keyboard);
        base.Update(gameTime);
    }

    private void HandleMovement(KeyboardState keyboard)
    {
        if (keyboard.IsKeyDown(Keys.Up))
            Player.TryStep(Direction.Up, Map);
        else if (keyboard.IsKeyDown(Keys.Down))
            Player.TryStep(Direction.Down, Map);
        else if (keyboard.IsKeyDown(Keys.Left))
            Player.TryStep(Direction.Left, Map);
        else if (keyboard.IsKeyDown(Keys.Right))
            Player.TryStep(Direction.Right, Map);
    }

    /// <inheritdoc />
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        base.Draw(gameTime);
    }
}
