using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using MonoRpgMaker.Editor;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Studio;

/// <summary>
/// The studio's main window: a toolbar (New / Save / Load / Event mode / Blocking), the tileset palette, the map
/// canvas, and an event inspector. A thin host — it loads the tileset sheet, owns file IO via the storage
/// provider, and drives the pure <see cref="MapPaintSession"/> (it edits placement data; the runtime materialises
/// behaviours); excluded from coverage.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class MainWindow : Window
{
    private const int TileSize = 32;
    private const int CellSize = 32;
    private const int DefaultWidth = 20;
    private const int DefaultHeight = 15;
    private const string DefaultSheet = "lpc-mountains.png";

    private readonly MapPaintSession _session;
    private readonly MapCanvas _canvas;
    private readonly TextBlock _status;
    private readonly ComboBox _triggerBox;
    private readonly ComboBox _kindBox;
    private readonly TextBox _textBox;
    private readonly StackPanel _inspector;
    private bool _refreshing;

    /// <summary>Build the window, its controls, and a default session over the LPC sheet.</summary>
    public MainWindow()
    {
        Title = "MonoRpgMaker Studio — Map Painter";
        Width = 1100;
        Height = 760;

        Bitmap sheet = LoadSheet(DefaultSheet);
        Tileset tileset = Tileset.FromSheet((int)sheet.Size.Width, (int)sheet.Size.Height, TileSize);
        _session = new MapPaintSession(tileset, DefaultWidth, DefaultHeight);

        _canvas = new MapCanvas(_session, sheet, CellSize);
        _status = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0) };

        _triggerBox = new ComboBox { ItemsSource = new[] { "StepOn", "ActionButton" }, HorizontalAlignment = HorizontalAlignment.Stretch };
        _kindBox = new ComboBox { ItemsSource = MapPaintSession.AvailableKinds.Select(k => k.Name).ToArray(), HorizontalAlignment = HorizontalAlignment.Stretch };
        _textBox = new TextBox { AcceptsReturn = true, MinHeight = 80, TextWrapping = TextWrapping.Wrap };
        _inspector = BuildInspector();
        _canvas.EventChanged += (_, _) => RefreshInspector();

        var palette = new TilePalette(_session, sheet);
        var blocking = new CheckBox { Content = "Blocking", VerticalAlignment = VerticalAlignment.Center };
        palette.BlockingProvider = () => blocking.IsChecked == true;
        palette.TileSelected += (_, _) => _status.Text = $"Active tile #{_session.Active.TilesetId}";

        Content = BuildLayout(palette, blocking);
        _status.Text = $"{tileset.TileCount} tiles · {DefaultWidth}×{DefaultHeight} map";
    }

    private DockPanel BuildLayout(TilePalette palette, CheckBox blocking)
    {
        var newButton = new Button { Content = "New" };
        newButton.Click += (_, _) =>
        {
            _session.NewMap(DefaultWidth, DefaultHeight);
            _canvas.SetSession(_session);
            _status.Text = "New map";
        };

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
        toolbar.Children.Add(newButton);
        toolbar.Children.Add(saveButton);
        toolbar.Children.Add(loadButton);
        toolbar.Children.Add(eventMode);
        toolbar.Children.Add(blocking);
        toolbar.Children.Add(_status);

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
        DockPanel.SetDock(paletteScroll, Dock.Left);
        DockPanel.SetDock(inspectorScroll, Dock.Right);
        layout.Children.Add(toolbar);
        layout.Children.Add(paletteScroll);
        layout.Children.Add(inspectorScroll);
        layout.Children.Add(canvasScroll);
        return layout;
    }

    private StackPanel BuildInspector()
    {
        _triggerBox.SelectionChanged += (_, _) => ApplyInspector();
        _kindBox.SelectionChanged += (_, _) => ApplyInspector();
        _textBox.LostFocus += (_, _) => ApplyInspector();

        var delete = new Button { Content = "Delete event", HorizontalAlignment = HorizontalAlignment.Stretch };
        delete.Click += (_, _) =>
        {
            if (_session.RemoveSelected())
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
        panel.Children.Add(new TextBlock { Text = "Text" });
        panel.Children.Add(_textBox);
        panel.Children.Add(delete);
        return panel;
    }

    private void RefreshInspector()
    {
        _refreshing = true;
        try
        {
            if (_session.SelectedEvent is { } selected)
            {
                _inspector.IsVisible = true;
                _triggerBox.SelectedItem = selected.Trigger;
                _kindBox.SelectedItem = selected.Kind;
                _textBox.Text = selected.Params.TryGetValue("text", out string? text) ? text : string.Empty;
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
        if (_refreshing || _session.SelectedEvent is null)
        {
            return;
        }

        string trigger = _triggerBox.SelectedItem as string ?? "ActionButton";
        string kind = _kindBox.SelectedItem as string ?? "ShowText";
        var parameters = new Dictionary<string, string> { ["text"] = _textBox.Text ?? string.Empty };
        _session.UpdateSelected(trigger, kind, parameters);
        _canvas.InvalidateVisual();
    }

    private static Bitmap LoadSheet(string fileName)
    {
        var uri = new Uri($"avares://MonoRpgMaker.Studio/Assets/{fileName}");
        using Stream stream = AssetLoader.Open(uri);
        return new Bitmap(stream);
    }

    private async Task SaveAsync()
    {
        try
        {
            IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save map $data",
                SuggestedFileName = "map.json",
                DefaultExtension = "json",
                FileTypeChoices = new[] { MapFileType },
            });

            if (file is null)
            {
                return;
            }

            await using Stream stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(_session.Save());
            _status.Text = $"Saved {file.Name}";
        }
        catch (IOException ex)
        {
            _status.Text = $"Save failed: {ex.Message}";
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Load map $data",
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

            MapLoadResult result = _session.Load(json);
            if (result.Ok)
            {
                _canvas.SetSession(_session);
                _status.Text = $"Loaded {files[0].Name}";
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

    private static FilePickerFileType MapFileType => new("Map $data") { Patterns = new[] { "*.json" } };
}
