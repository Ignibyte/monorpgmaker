using System;

namespace MonoRpgMaker.Abstractions;

/// <summary>
/// An integer map coordinate — the pure, MonoGame-free cell position the published seam
/// uses in place of XNA <c>Point</c>, so <c>MonoRpgMaker.Abstractions</c> stays free of the
/// host framework (D-0014). Every operation is integer-only and deterministic (D-0016).
/// Coordinates are map-sized, so the distance helpers use plain <see cref="int"/> arithmetic
/// without an overflow guard.
/// </summary>
/// <param name="X">The column (horizontal cell index).</param>
/// <param name="Y">The row (vertical cell index).</param>
public readonly record struct GridPoint(int X, int Y)
{
    /// <summary>The origin, <c>(0, 0)</c>.</summary>
    public static readonly GridPoint Zero = new(0, 0);

    /// <summary>Adds two points componentwise.</summary>
    public static GridPoint operator +(GridPoint a, GridPoint b) => new(a.X + b.X, a.Y + b.Y);

    /// <summary>Subtracts <paramref name="b"/> from <paramref name="a"/> componentwise.</summary>
    public static GridPoint operator -(GridPoint a, GridPoint b) => new(a.X - b.X, a.Y - b.Y);

    /// <summary>The Manhattan (taxicab) distance to <paramref name="other"/>: <c>|Δx| + |Δy|</c>.</summary>
    public int ManhattanDistanceTo(GridPoint other) => Abs(X - other.X) + Abs(Y - other.Y);

    /// <summary>The Chebyshev (chessboard) distance to <paramref name="other"/>: <c>max(|Δx|, |Δy|)</c>.</summary>
    public int ChebyshevDistanceTo(GridPoint other) => Math.Max(Abs(X - other.X), Abs(Y - other.Y));

    // Integer absolute value (Math.Abs throws on int.MinValue); Abs(int.MinValue) wraps deterministically.
    private static int Abs(int n) => n < 0 ? unchecked(-n) : n;
}
