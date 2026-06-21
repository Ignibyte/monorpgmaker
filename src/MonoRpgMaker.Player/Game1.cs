using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework.Content;
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

    /// <summary>Load the UI font from the content pipeline + seed the demo economy (so the bundled shop is transactable).</summary>
    protected override void LoadContent()
    {
        base.LoadContent();
        try
        {
            SetFont(Content.Load<SpriteFont>("font"));
        }
        catch (ContentLoadException)
        {
            // No font shipped → the game runs textless rather than crashing.
        }

        Session.Active.State.Add("gold", 100);
        Session.Active.State.Add("item.potion", 1);
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

    /// <summary>Persist the game (state + active map + player position) to the user's save file (F5).</summary>
    protected override void OnSaveRequested()
    {
        try
        {
            string json = SaveSerializer.Serialize(
                Session.Active.State, Session.Active.Player.Cell, Session.Active.Player.Facing, 0UL, Session.ActiveMapId);
            string path = SavePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json);
        }
        catch (IOException)
        {
            // A bad disk must not crash the game; the save is simply skipped.
        }
        catch (UnauthorizedAccessException)
        {
            // A protected/sandboxed save directory must not crash the game; the save is simply skipped.
        }
    }

    /// <summary>Restore the game from the user's save file (F9) — in place on the live session, if the file is present + valid.</summary>
    protected override void OnLoadRequested()
    {
        string path = SavePath();
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            SaveLoadResult result = SaveSerializer.Deserialize(File.ReadAllText(path));
            if (result.Ok)
            {
                Session.TryRestore(result.Save!);
            }
        }
        catch (IOException)
        {
            // A bad read must not crash the game; the load is simply skipped.
        }
        catch (UnauthorizedAccessException)
        {
            // A protected/sandboxed save file must not crash the game; the load is simply skipped.
        }
    }

    private static string SavePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "monorpgmaker", "save.json");

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
