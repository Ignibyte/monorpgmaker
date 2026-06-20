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
/// The map-editing surface: blits each cell's tileset sprite from the sheet, overlays a grid and a blocking
/// indicator, and paints the active tile under the pointer (click + drag). A thin view over
/// <see cref="MapPaintSession"/>; all behaviour is verified by the session's unit tests, so this is excluded
/// from coverage.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class MapCanvas : Control
{
    private static readonly IBrush Background = new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x24));
    private static readonly IBrush BlockingOverlay = new SolidColorBrush(Colors.Red, 0.30);
    private static readonly Pen GridPen = new(new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)));
    private static readonly IBrush EventMarker = new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00), 0.85);
    private static readonly Pen SelectedPen = new(new SolidColorBrush(Colors.White), 2);

    private readonly Bitmap _sheet;
    private readonly int _cellSize;
    private MapPaintSession _session;
    private bool _painting;

    /// <summary>When true, a pointer press places/selects an event instead of painting a tile.</summary>
    public bool EventMode { get; set; }

    /// <summary>Raised after an event is placed or selected, so the host can refresh its inspector.</summary>
    public event EventHandler? EventChanged;

    /// <summary>Create the canvas over <paramref name="session"/>, blitting from <paramref name="sheet"/> at <paramref name="cellSize"/> pixels per cell.</summary>
    public MapCanvas(MapPaintSession session, Bitmap sheet, int cellSize)
    {
        _session = session;
        _sheet = sheet;
        _cellSize = cellSize;
        SyncSize();
    }

    /// <summary>Swap the session under edit (after New/Load) and repaint.</summary>
    public void SetSession(MapPaintSession session)
    {
        _session = session;
        SyncSize();
        InvalidateVisual();
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        context.FillRectangle(Background, new Rect(0, 0, _session.Width * _cellSize, _session.Height * _cellSize));

        for (var y = 0; y < _session.Height; y++)
        {
            for (var x = 0; x < _session.Width; x++)
            {
                Tile tile = _session.TileAt(x, y);
                var dest = new Rect(x * _cellSize, y * _cellSize, _cellSize, _cellSize);

                if (_session.Tileset.TryGetSourceRect(tile.TilesetId, out SourceRect sr))
                {
                    context.DrawImage(_sheet, new Rect(sr.X, sr.Y, sr.Width, sr.Height), dest);
                }

                if (tile.Blocking)
                {
                    context.FillRectangle(BlockingOverlay, dest);
                }

                context.DrawRectangle(null, GridPen, dest);
            }
        }

        foreach (Cell cell in _session.EventCells)
        {
            var inset = _cellSize / 4.0;
            var marker = new Rect((cell.X * _cellSize) + inset, (cell.Y * _cellSize) + inset, _cellSize - (2 * inset), _cellSize - (2 * inset));
            context.FillRectangle(EventMarker, marker);
        }

        if (_session.SelectedEvent is { } selected)
        {
            context.DrawRectangle(null, SelectedPen, new Rect(selected.Cell.X * _cellSize, selected.Cell.Y * _cellSize, _cellSize, _cellSize));
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Point pos = e.GetPosition(this);
        if (EventMode)
        {
            if (_session.CellAt(pos.X, pos.Y, _cellSize) is { } cell)
            {
                _session.AddEvent(cell);
                InvalidateVisual();
                EventChanged?.Invoke(this, EventArgs.Empty);
            }

            return;
        }

        _painting = true;
        PaintAt(pos);
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_painting)
        {
            PaintAt(e.GetPosition(this));
        }
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _painting = false;
    }

    private void PaintAt(Point pixel)
    {
        if (_session.CellAt(pixel.X, pixel.Y, _cellSize) is { } cell && _session.Paint(cell))
        {
            InvalidateVisual();
        }
    }

    private void SyncSize()
    {
        Width = _session.Width * _cellSize;
        Height = _session.Height * _cellSize;
    }
}
