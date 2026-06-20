using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MonoRpgMaker.Editor;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Studio;

/// <summary>
/// The tileset palette: draws the full sheet and selects the active tile (by sheet index) under the pointer,
/// highlighting it. A thin view over <see cref="MapPaintSession"/>, excluded from coverage. The host swaps the
/// sheet + its <see cref="Tileset"/> geometry when the tileset changes.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TilePalette : Control
{
    private static readonly Pen HighlightPen = new(new SolidColorBrush(Colors.Gold), 2);

    private readonly MapPaintSession _session;
    private Bitmap _sheet;
    private Tileset _geometry;

    /// <summary>Create the palette over <paramref name="session"/> showing <paramref name="sheet"/> (sliced by <paramref name="geometry"/>).</summary>
    public TilePalette(MapPaintSession session, Bitmap sheet, Tileset geometry)
    {
        _session = session;
        _sheet = sheet;
        _geometry = geometry;
        Width = sheet.Size.Width;
        Height = sheet.Size.Height;
    }

    /// <summary>Raised when the active tile changes, so the host can reflect the selection.</summary>
    public event EventHandler? TileSelected;

    /// <summary>Supplies whether newly selected tiles should be flagged as blocking (the toolbar checkbox).</summary>
    public Func<bool>? BlockingProvider { get; set; }

    /// <summary>Swap the displayed sheet + its geometry (after a tileset pick), resize to the new sheet, and repaint.</summary>
    public void SetSheet(Bitmap sheet, Tileset geometry)
    {
        _sheet = sheet;
        _geometry = geometry;
        Width = sheet.Size.Width;
        Height = sheet.Size.Height;
        InvalidateVisual();
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        context.DrawImage(_sheet, new Rect(0, 0, _sheet.Size.Width, _sheet.Size.Height));

        if (_geometry.TryGetSourceRect(_session.Active.TilesetId, out SourceRect sr))
        {
            context.DrawRectangle(null, HighlightPen, new Rect(sr.X, sr.Y, sr.Width, sr.Height));
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var pixel = e.GetPosition(this);
        if (_geometry.TryGetTileIndex((int)pixel.X, (int)pixel.Y, out var index))
        {
            _session.SelectTile(index, BlockingProvider?.Invoke() ?? false);
            InvalidateVisual();
            TileSelected?.Invoke(this, EventArgs.Empty);
        }
    }
}
