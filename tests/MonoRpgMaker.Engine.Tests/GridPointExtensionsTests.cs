using Microsoft.Xna.Framework;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Engine.World;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

public class GridPointExtensionsTests
{
    [Fact] // AD1 — REQ-005 (Point <-> GridPoint round-trip, both directions)
    public void Adapter_ConvertsBothDirections()
    {
        Assert.Equal(new GridPoint(3, 5), new Point(3, 5).ToGridPoint());
        Assert.Equal(new GridPoint(-2, -7), new Point(-2, -7).ToGridPoint());

        Assert.Equal(new Point(3, 5), new GridPoint(3, 5).ToXnaPoint());
        Assert.Equal(new Point(-2, -7), new GridPoint(-2, -7).ToXnaPoint());
    }
}
