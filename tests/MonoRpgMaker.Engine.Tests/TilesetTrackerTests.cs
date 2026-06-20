using System;
using MonoRpgMaker.Engine.Core;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

/// <summary>
/// REQ-001 / REQ-003 — the gated change-detect seam the host polls each frame to reload the tileset sheet only on
/// an actual change (e.g. a warp to a map with a different tileset). Isolated exact-value asserts.
/// </summary>
public class TilesetTrackerTests
{
    [Fact] // REQ-001 — the first advance fires (nothing applied yet → the initial load)
    public void TryAdvance_Initial_True()
    {
        var tracker = new TilesetTracker();

        Assert.True(tracker.TryAdvance("lpc-mountains"));
    }

    [Fact] // REQ-003 — the same name twice does not re-fire (no per-frame churn)
    public void TryAdvance_SameName_FalseSecondTime()
    {
        var tracker = new TilesetTracker();
        tracker.TryAdvance("lpc-mountains");

        Assert.False(tracker.TryAdvance("lpc-mountains"));
    }

    [Fact] // REQ-001 — a real change fires (re-skin on a warp to a different sheet)
    public void TryAdvance_Changed_True()
    {
        var tracker = new TilesetTracker();
        tracker.TryAdvance("lpc-mountains");

        Assert.True(tracker.TryAdvance("lpc-grass"));
    }

    [Fact] // REQ-001 — changing BACK to a prior name fires too (kills a "remember every seen name" mutant)
    public void TryAdvance_ChangedBack_True()
    {
        var tracker = new TilesetTracker();
        tracker.TryAdvance("lpc-mountains");
        tracker.TryAdvance("lpc-grass");

        Assert.True(tracker.TryAdvance("lpc-mountains"));
    }

    [Fact] // a null name is a guarded programmer error (a map always names a tileset)
    public void TryAdvance_Null_Throws()
    {
        var tracker = new TilesetTracker();

        Assert.Throws<ArgumentNullException>(() => tracker.TryAdvance(null!));
    }
}
