using MonoRpgMaker.Abstractions;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

public class GridPointTests
{
    [Fact] // GP1 — REQ-001
    public void Components_Equality_Hash()
    {
        var p = new GridPoint(3, 5);
        Assert.Equal(3, p.X);
        Assert.Equal(5, p.Y);

        Assert.True(p == new GridPoint(3, 5));
        Assert.Equal(p.GetHashCode(), new GridPoint(3, 5).GetHashCode());

        Assert.True(p != new GridPoint(3, 6));
        Assert.NotEqual(p, new GridPoint(4, 5));
    }

    [Fact] // GP2 — REQ-002
    public void Add_Subtract_AreComponentwise()
    {
        Assert.Equal(new GridPoint(4, -2), new GridPoint(1, 2) + new GridPoint(3, -4));
        Assert.Equal(new GridPoint(3, -4), new GridPoint(4, -2) - new GridPoint(1, 2));

        var p = new GridPoint(7, -9);
        Assert.Equal(p, p + GridPoint.Zero);
    }

    [Fact] // GP3 — REQ-003
    public void Zero_IsOrigin()
    {
        Assert.Equal(0, GridPoint.Zero.X);
        Assert.Equal(0, GridPoint.Zero.Y);
        Assert.Equal(new GridPoint(0, 0), GridPoint.Zero);
    }

    [Fact] // GP4 — REQ-004
    public void Manhattan_And_Chebyshev()
    {
        var origin = GridPoint.Zero;

        Assert.Equal(7, origin.ManhattanDistanceTo(new GridPoint(3, -4)));
        Assert.Equal(4, origin.ChebyshevDistanceTo(new GridPoint(3, -4)));

        Assert.Equal(5, origin.ChebyshevDistanceTo(new GridPoint(5, 2)));   // dx > dy
        Assert.Equal(5, origin.ChebyshevDistanceTo(new GridPoint(2, 5)));   // dy > dx
        Assert.Equal(3, origin.ChebyshevDistanceTo(new GridPoint(3, 3)));   // dx == dy

        Assert.Equal(0, origin.ManhattanDistanceTo(origin));                // self
        Assert.Equal(0, new GridPoint(4, 7).ChebyshevDistanceTo(new GridPoint(4, 7)));

        var a = new GridPoint(3, -4);
        var b = new GridPoint(-1, 2);
        Assert.Equal(b.ManhattanDistanceTo(a), a.ManhattanDistanceTo(b)); // symmetric

        // Non-origin pair (both axes non-zero on BOTH endpoints) — kills the
        // `_ - other._` -> `+` subtraction mutants that origin-anchored cases miss.
        Assert.Equal(6, new GridPoint(4, 2).ManhattanDistanceTo(new GridPoint(1, 5)));
        Assert.Equal(3, new GridPoint(4, 2).ChebyshevDistanceTo(new GridPoint(1, 5)));
    }
}
