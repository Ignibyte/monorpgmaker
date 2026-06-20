using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using MonoRpgMaker.Engine.Core;
using MonoRpgMaker.Engine.Sim;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Player;

/// <summary>
/// The shipped game executable's host: boots the engine over the bundled start map and renders it with the
/// embedded LPC tileset. The repo is the game (D-0023) — content is embedded, never loaded from a roamed path.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class Game1 : RpgGame
{
    private const int TileSize = 32;

    /// <summary>Boot over the bundled start map (falling back to a built-in map if the resource is unreadable).</summary>
    public Game1()
        : base(LoadStartWorld())
    {
    }

    /// <inheritdoc />
    protected override void LoadContent()
    {
        base.LoadContent();

        using Stream? sheet = Assembly.GetExecutingAssembly().GetManifestResourceStream("lpc-mountains.png");
        if (sheet is not null)
        {
            var texture = Texture2D.FromStream(GraphicsDevice, sheet);
            SetTileset(texture, Tileset.FromSheet(texture.Width, texture.Height, TileSize));
        }
    }

    private static WorldSim LoadStartWorld()
    {
        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("start.json");
        if (stream is null)
        {
            return StartMap.BuildWorld();
        }

        using var reader = new StreamReader(stream);
        WorldSimResult result = StartMap.LoadWorld(reader.ReadToEnd());
        return result.Ok ? result.Sim! : StartMap.BuildWorld();
    }
}
