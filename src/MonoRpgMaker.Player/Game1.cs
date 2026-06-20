using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Engine.Core;
using MonoRpgMaker.Engine.Sim;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Player;

/// <summary>
/// The shipped game executable's host: boots the engine over the bundled map SET (a <c>game.json</c> manifest +
/// per-map <c>$data</c>) and renders the active map with its embedded LPC tileset. The repo is the game (D-0023)
/// — content is embedded, never loaded from a roamed path; a Warp event switches the active map.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class Game1 : RpgGame
{
    private const int TileSize = 32;

    /// <summary>Boot over the bundled map set (falling back to the built-in two-map set if the resources are unreadable).</summary>
    public Game1()
        : base(LoadSession())
    {
    }

    /// <summary>
    /// Reload the embedded sheet for the active map's tileset (the single-source <see cref="TilesetCatalog"/>
    /// resolves it) — fired by the base host on the first frame and whenever a warp switches to a map with a
    /// different tileset, so the active map always renders with its own art.
    /// </summary>
    protected override void OnTilesetChanged(string tilesetName)
    {
        string resource = TilesetCatalog.ResolveResourceFile(tilesetName);
        using Stream? sheet = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource);
        if (sheet is not null)
        {
            var texture = Texture2D.FromStream(GraphicsDevice, sheet);
            SetTileset(texture, Tileset.FromSheet(texture.Width, texture.Height, TileSize));
        }
    }

    private static GameSession LoadSession()
    {
        GameManifest? manifest = ReadManifest();
        if (manifest is null)
        {
            return StartMap.BuildSession();
        }

        var maps = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string id in manifest.MapIds)
        {
            string? json = ReadResource(id + ".json");
            if (json is not null)
            {
                maps[id] = json;
            }
        }

        GameSessionResult result = GameSession.Create(maps, manifest.StartMap, new GridPoint(StartMap.PlayerStart.X, StartMap.PlayerStart.Y));
        return result.Ok ? result.Session! : StartMap.BuildSession();
    }

    private static GameManifest? ReadManifest()
    {
        string? json = ReadResource("game.json");
        return json is null ? null : GameManifestSerializer.Deserialize(json);
    }

    private static string? ReadResource(string logicalName)
    {
        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(logicalName);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
