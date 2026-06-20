using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using MonoRpgMaker.Editor;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Studio;

/// <summary>
/// The studio's main window: a toolbar (New map / Save / Load / Event mode / Blocking / tileset picker), a map
/// list (the multi-map set), the tileset palette, the map canvas, and a kind-aware event inspector. A thin host —
/// it loads tileset sheets, owns file IO, and drives the pure <see cref="MapProject"/> (it edits placement data;
/// the runtime materialises behaviours); excluded from coverage.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class MainWindow : Window
{
    private const int TileSize = 32;
    private const int CellSize = 32;
    private const int DefaultWidth = 20;
    private const int DefaultHeight = 15;

    private readonly MapProject _project;
    private readonly MapCanvas _canvas;
    private readonly TilePalette _palette;
    private readonly ListBox _mapList;
    private readonly TextBlock _status;
    private readonly ComboBox _triggerBox;
    private readonly ComboBox _kindBox;
    private readonly ComboBox _tilesetBox;
    private readonly StackPanel _paramPanel;
    private readonly StackPanel _inspector;
    private readonly Button _undoButton;
    private readonly Button _redoButton;
    private readonly Dictionary<string, Control> _paramControls = new(StringComparer.Ordinal);
    private readonly EventHandler _historyHandler;
    private MapPaintSession? _subscribedSession;
    private bool _refreshing;

    /// <summary>Build the window, its controls, and a one-map project over the default LPC sheet.</summary>
    public MainWindow()
    {
        Title = "MonoRpgMaker Studio — Map Painter";
        Width = 1180;
        Height = 760;

        _project = new MapProject("start", DefaultWidth, DefaultHeight);
        (Bitmap sheet, Tileset geometry) = LoadSheetFor(Session.ActiveTileset);

        _canvas = new MapCanvas(Session, sheet, geometry, CellSize);
        _palette = new TilePalette(Session, sheet, geometry);
        _status = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0) };
        _mapList = new ListBox { ItemsSource = _project.MapIds.ToList(), SelectedItem = _project.ActiveId, Width = 150 };

        _triggerBox = new ComboBox { ItemsSource = new[] { "StepOn", "ActionButton" }, HorizontalAlignment = HorizontalAlignment.Stretch };
        _kindBox = new ComboBox { ItemsSource = MapPaintSession.AvailableKinds.Select(k => k.Name).ToArray(), HorizontalAlignment = HorizontalAlignment.Stretch };
        _paramPanel = new StackPanel { Spacing = 4 };
        _inspector = BuildInspector();
        _canvas.EventChanged += (_, _) => RefreshInspector();

        _tilesetBox = new ComboBox
        {
            ItemsSource = MapPaintSession.AvailableTilesets.Select(t => t.Name).ToArray(),
            SelectedItem = Session.ActiveTileset,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _tilesetBox.SelectionChanged += (_, _) => OnTilesetPicked();

        _undoButton = new Button { Content = "Undo", IsEnabled = false };
        _undoButton.Click += (_, _) => Session.Undo();
        _redoButton = new Button { Content = "Redo", IsEnabled = false };
        _redoButton.Click += (_, _) => Session.Redo();
        _historyHandler = (_, _) => RefreshHistory();
        _subscribedSession = Session;
        Session.HistoryChanged += _historyHandler;
        KeyDown += OnKeyDown;

        _mapList.SelectionChanged += (_, _) => OnMapSelected();

        var blocking = new CheckBox { Content = "Blocking", VerticalAlignment = VerticalAlignment.Center };
        _palette.BlockingProvider = () => blocking.IsChecked == true;
        _palette.TileSelected += (_, _) => _status.Text = $"Active tile #{Session.Active.TilesetId}";

        Content = BuildLayout(_palette, blocking);
        _status.Text = $"{_project.MapCount} map(s) · active '{_project.ActiveId}' · {Session.ActiveTileset}";
    }

    private MapPaintSession Session => _project.Active;

    private DockPanel BuildLayout(TilePalette palette, CheckBox blocking)
    {
        var newMap = new Button { Content = "New map" };
        newMap.Click += (_, _) => OnNewMap();

        var saveButton = new Button { Content = "Save" };
        saveButton.Click += async (_, _) => await SaveAsync();

        var loadButton = new Button { Content = "Load" };
        loadButton.Click += async (_, _) => await LoadAsync();

        var eventMode = new ToggleButton { Content = "Event" };
        eventMode.IsCheckedChanged += (_, _) =>
        {
            _canvas.EventMode = eventMode.IsChecked == true;
            if (!_canvas.EventMode)
            {
                _inspector.IsVisible = false;
            }

            _status.Text = _canvas.EventMode ? "Event mode — click a tile to place / select an event" : "Paint mode";
        };

        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(6) };
        toolbar.Children.Add(newMap);
        toolbar.Children.Add(saveButton);
        toolbar.Children.Add(loadButton);
        toolbar.Children.Add(_undoButton);
        toolbar.Children.Add(_redoButton);
        toolbar.Children.Add(eventMode);
        toolbar.Children.Add(blocking);
        toolbar.Children.Add(new TextBlock { Text = "Tileset", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 2, 0) });
        toolbar.Children.Add(_tilesetBox);
        toolbar.Children.Add(_status);

        var mapsPanel = new StackPanel { Width = 150, Margin = new Thickness(6) };
        mapsPanel.Children.Add(new TextBlock { Text = "Maps", FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 4) });
        mapsPanel.Children.Add(_mapList);

        var paletteScroll = new ScrollViewer
        {
            Content = palette,
            Width = 300,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };

        var canvasScroll = new ScrollViewer
        {
            Content = _canvas,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };

        var inspectorScroll = new ScrollViewer
        {
            Content = _inspector,
            Width = 260,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };

        var layout = new DockPanel();
        DockPanel.SetDock(toolbar, Dock.Top);
        DockPanel.SetDock(mapsPanel, Dock.Left);
        DockPanel.SetDock(paletteScroll, Dock.Left);
        DockPanel.SetDock(inspectorScroll, Dock.Right);
        layout.Children.Add(toolbar);
        layout.Children.Add(mapsPanel);
        layout.Children.Add(paletteScroll);
        layout.Children.Add(inspectorScroll);
        layout.Children.Add(canvasScroll);
        return layout;
    }

    private void OnNewMap()
    {
        string id = "map-" + (_project.MapCount + 1).ToString(CultureInfo.InvariantCulture);
        if (!_project.NewMap(id, DefaultWidth, DefaultHeight))
        {
            return;
        }

        RebindActiveSession();
        _mapList.ItemsSource = _project.MapIds.ToList();
        _refreshing = true;
        _mapList.SelectedItem = _project.ActiveId;
        _refreshing = false;
        _status.Text = $"New map '{id}'";
    }

    private void OnMapSelected()
    {
        if (_refreshing || _mapList.SelectedItem is not string id || id == _project.ActiveId)
        {
            return;
        }

        if (_project.SelectMap(id))
        {
            RebindActiveSession();
            _status.Text = $"Editing '{id}'";
        }
    }

    // After a map switch / new map: re-point the canvas + palette at the new active session, reload its sheet,
    // move the undo/redo subscription to it, and sync the tileset picker + buttons + inspector.
    private void RebindActiveSession()
    {
        _canvas.SetSession(Session);
        _palette.SetSession(Session);
        if (_subscribedSession is not null)
        {
            _subscribedSession.HistoryChanged -= _historyHandler;
        }

        Session.HistoryChanged += _historyHandler;
        _subscribedSession = Session;
        SyncTilesetTo(Session.ActiveTileset);
        RefreshHistory();
    }

    private void OnTilesetPicked()
    {
        if (_refreshing || _tilesetBox.SelectedItem is not string name)
        {
            return;
        }

        if (Session.SelectTileset(name))
        {
            ApplySheet(name);
            _status.Text = $"Tileset: {name}";
        }
    }

    private void ApplySheet(string name)
    {
        (Bitmap sheet, Tileset geometry) = LoadSheetFor(name);
        _canvas.SetSheet(sheet, geometry);
        _palette.SetSheet(sheet, geometry);
    }

    private void RefreshHistory()
    {
        _undoButton.IsEnabled = Session.CanUndo;
        _redoButton.IsEnabled = Session.CanRedo;
        _canvas.InvalidateVisual();
        RefreshInspector();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        bool modifier = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
        if (!modifier)
        {
            return;
        }

        bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        if (e.Key == Key.Z && !shift)
        {
            Session.Undo();
            e.Handled = true;
        }
        else if (e.Key == Key.Y || (e.Key == Key.Z && shift))
        {
            Session.Redo();
            e.Handled = true;
        }
    }

    private StackPanel BuildInspector()
    {
        _triggerBox.SelectionChanged += (_, _) => ApplyInspector();
        _kindBox.SelectionChanged += (_, _) => OnKindChanged();

        var delete = new Button { Content = "Delete event", HorizontalAlignment = HorizontalAlignment.Stretch };
        delete.Click += (_, _) =>
        {
            if (Session.RemoveSelected())
            {
                _canvas.InvalidateVisual();
                RefreshInspector();
            }
        };

        var panel = new StackPanel { Spacing = 6, Margin = new Thickness(8), IsVisible = false };
        panel.Children.Add(new TextBlock { Text = "Event", FontWeight = FontWeight.Bold });
        panel.Children.Add(new TextBlock { Text = "Trigger" });
        panel.Children.Add(_triggerBox);
        panel.Children.Add(new TextBlock { Text = "Kind" });
        panel.Children.Add(_kindBox);
        panel.Children.Add(_paramPanel);
        panel.Children.Add(delete);
        return panel;
    }

    // Rebuild the param fields to exactly the selected kind's ParamKeys (the single source — BehaviourRegistry's
    // descriptor — so the editor can never offer a field the runtime won't read), then apply.
    private void OnKindChanged()
    {
        RebuildParamFields();
        ApplyInspector();
    }

    private void RebuildParamFields()
    {
        _paramPanel.Children.Clear();
        _paramControls.Clear();

        string kind = _kindBox.SelectedItem as string ?? "ShowText";
        IReadOnlyList<string> paramKeys = MapPaintSession.AvailableKinds.FirstOrDefault(k => k.Name == kind)?.ParamKeys ?? [];
        foreach (string key in paramKeys)
        {
            _paramPanel.Children.Add(new TextBlock { Text = ParamLabel(key) });
            Control control = key == "map"
                ? new ComboBox { ItemsSource = _project.MapIds.ToList(), HorizontalAlignment = HorizontalAlignment.Stretch }
                : new TextBox { AcceptsReturn = key == "text", MinHeight = key == "text" ? 80 : 0, TextWrapping = TextWrapping.Wrap };
            if (control is ComboBox combo)
            {
                combo.SelectionChanged += (_, _) => ApplyInspector();
            }
            else if (control is TextBox box)
            {
                box.LostFocus += (_, _) => ApplyInspector();
            }

            _paramControls[key] = control;
            _paramPanel.Children.Add(control);
        }
    }

    private static string ParamLabel(string key) => key switch
    {
        "text" => "Text",
        "map" => "Target map",
        "x" => "Target X",
        "y" => "Target Y",
        _ => key,
    };

    private void RefreshInspector()
    {
        _refreshing = true;
        try
        {
            if (Session.SelectedEvent is { } selected)
            {
                _inspector.IsVisible = true;
                _triggerBox.SelectedItem = selected.Trigger;
                _kindBox.SelectedItem = selected.Kind;
                RebuildParamFields();
                foreach (KeyValuePair<string, Control> entry in _paramControls)
                {
                    string value = selected.Params.TryGetValue(entry.Key, out string? v) ? v : string.Empty;
                    SetControlValue(entry.Value, value);
                }

                _status.Text = $"Event {selected.Id} at ({selected.Cell.X}, {selected.Cell.Y})";
            }
            else
            {
                _inspector.IsVisible = false;
            }
        }
        finally
        {
            _refreshing = false;
        }
    }

    private void ApplyInspector()
    {
        if (_refreshing || Session.SelectedEvent is null)
        {
            return;
        }

        string trigger = _triggerBox.SelectedItem as string ?? "ActionButton";
        string kind = _kindBox.SelectedItem as string ?? "ShowText";
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, Control> entry in _paramControls)
        {
            parameters[entry.Key] = ControlValue(entry.Value);
        }

        Session.UpdateSelected(trigger, kind, parameters);
        _canvas.InvalidateVisual();
    }

    private static void SetControlValue(Control control, string value)
    {
        switch (control)
        {
            case ComboBox combo:
                combo.SelectedItem = value;
                break;
            case TextBox box:
                box.Text = value;
                break;
        }
    }

    private static string ControlValue(Control control) => control switch
    {
        ComboBox combo => combo.SelectedItem as string ?? string.Empty,
        TextBox box => box.Text ?? string.Empty,
        _ => string.Empty,
    };

    private static (Bitmap Sheet, Tileset Geometry) LoadSheetFor(string name)
    {
        Bitmap sheet = LoadSheet(TilesetCatalog.ResolveResourceFile(name));
        Tileset geometry = Tileset.FromSheet((int)sheet.Size.Width, (int)sheet.Size.Height, TileSize);
        return (sheet, geometry);
    }

    private static Bitmap LoadSheet(string fileName)
    {
        var uri = new Uri($"avares://MonoRpgMaker.Studio/Assets/{fileName}");
        using Stream stream = AssetLoader.Open(uri);
        return new Bitmap(stream);
    }

    // Save the whole map SET to a folder: game.json (the manifest) + one <id>.json per map — the layout the
    // runtime embeds + reads.
    private async Task SaveAsync()
    {
        try
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Save the map set to a folder" });
            if (folders.Count == 0 || folders[0].TryGetLocalPath() is not { } dir)
            {
                return;
            }

            SavedProject saved = _project.Save();
            await File.WriteAllTextAsync(Path.Combine(dir, "game.json"), saved.Manifest);
            foreach (KeyValuePair<string, string> map in saved.Maps)
            {
                await File.WriteAllTextAsync(Path.Combine(dir, map.Key + ".json"), map.Value);
            }

            _status.Text = $"Saved {saved.Maps.Count} map(s) + game.json to {dir}";
        }
        catch (IOException ex)
        {
            _status.Text = $"Save failed: {ex.Message}";
        }
    }

    // Load a single map's $data into the active map (a convenience over the existing session loader).
    private async Task LoadAsync()
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Load a map $data into the active map",
                AllowMultiple = false,
                FileTypeFilter = new[] { MapFileType },
            });

            if (files.Count == 0)
            {
                return;
            }

            string json;
            await using (Stream stream = await files[0].OpenReadAsync())
            using (var reader = new StreamReader(stream))
            {
                json = await reader.ReadToEndAsync();
            }

            MapLoadResult result = Session.Load(json);
            if (result.Ok)
            {
                _canvas.SetSession(Session);
                SyncTilesetTo(Session.ActiveTileset);
                RefreshHistory();
                _status.Text = $"Loaded {files[0].Name} into '{_project.ActiveId}'";
            }
            else
            {
                _status.Text = $"Load failed: {result.Error}";
            }
        }
        catch (IOException ex)
        {
            _status.Text = $"Load failed: {ex.Message}";
        }
    }

    private void SyncTilesetTo(string name)
    {
        _refreshing = true;
        _tilesetBox.SelectedItem = name;
        _refreshing = false;
        ApplySheet(name);
    }

    private static FilePickerFileType MapFileType => new("Map $data") { Patterns = new[] { "*.json" } };
}
