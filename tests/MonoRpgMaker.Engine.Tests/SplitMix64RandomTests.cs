using System;
using MonoRpgMaker.Abstractions;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

/// <summary>
/// Golden values are the canonical SplitMix64(seed=0) outputs, cross-checked against an
/// independent reference implementation: NextBits = e220a8397b1dcdaf, 6e789e6aa1b965f4,
/// 06c45d188009454f; NextInt() = -501176263; NextBool = true,false,false;
/// NextFixedPoint Raw = 52655 (= 0xE220…F &amp; 0xFFFF).
/// </summary>
public class SplitMix64RandomTests
{
    [Fact] // RT1 — REQ-001
    public void SameSeed_ProducesIdenticalSequence()
    {
        var a = new SplitMix64Random(0x123456789ABCDEFUL);
        var b = new SplitMix64Random(0x123456789ABCDEFUL);

        for (var i = 0; i < 8; i++)
            Assert.Equal(a.NextBits(), b.NextBits());
    }

    [Fact] // RT2 — REQ-002 (canonical golden vector — the generation-path mutation-killer)
    public void KnownSeed_ProducesGoldenSequence()
    {
        var rng = new SplitMix64Random(0UL);

        Assert.Equal(0xE220A8397B1DCDAFUL, rng.NextBits());
        Assert.Equal(0x6E789E6AA1B965F4UL, rng.NextBits());
        Assert.Equal(0x06C45D188009454FUL, rng.NextBits());
    }

    [Fact] // RT3 — REQ-003
    public void NextInt_Bounded_StaysInRange()
    {
        var rng = new SplitMix64Random(42UL);

        for (var i = 0; i < 1000; i++)
            Assert.InRange(rng.NextInt(0, 6), 0, 5);

        for (var i = 0; i < 1000; i++)
            Assert.InRange(rng.NextInt(-3, 3), -3, 2);

        for (var i = 0; i < 1000; i++)
            Assert.True(rng.NextInt(int.MinValue, int.MaxValue) < int.MaxValue);   // maxExclusive honoured across the maximal span
    }

    [Fact] // RT4 — REQ-003 (range of one)
    public void NextInt_RangeOfOne_AlwaysReturnsMin()
    {
        var rng = new SplitMix64Random(7UL);
        for (var i = 0; i < 100; i++)
            Assert.Equal(5, rng.NextInt(5, 6));
    }

    [Fact] // RT5 — REQ-003 (precondition guard)
    public void NextInt_InvalidRange_Throws()
    {
        var rng = new SplitMix64Random(1UL);
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(5, 5));   // empty range
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(5, 4));   // inverted
    }

    [Fact] // RT9 — REQ-001/004 (NextInt() full-range golden + determinism; kills the >>32 extraction)
    public void NextInt_FullRange_GoldenAndDeterministic()
    {
        Assert.Equal(-501176263, new SplitMix64Random(0UL).NextInt());

        var a = new SplitMix64Random(555UL);
        var b = new SplitMix64Random(555UL);
        for (var i = 0; i < 8; i++)
            Assert.Equal(a.NextInt(), b.NextInt());
    }

    [Fact] // RT6 — REQ-004 (NextBool golden prefix kills the >>63 extraction; both values occur)
    public void NextBool_GoldenPrefix_AndBothValuesOccur()
    {
        var rng = new SplitMix64Random(0UL);
        Assert.True(rng.NextBool());    // bit 63 of 0xE220…F == 1
        Assert.False(rng.NextBool());   // bit 63 of 0x6E78…F4 == 0
        Assert.False(rng.NextBool());   // bit 63 of 0x06C4…4F == 0

        var sawTrue = false;
        var sawFalse = false;
        var run = new SplitMix64Random(99UL);
        for (var i = 0; i < 100; i++)
        {
            if (run.NextBool())
                sawTrue = true;
            else
                sawFalse = true;
        }

        Assert.True(sawTrue);
        Assert.True(sawFalse);
    }

    [Fact] // RT7 — REQ-004 (NextFixedPoint golden Raw kills the low-16 mask; unit interval)
    public void NextFixedPoint_GoldenRaw_AndInUnitInterval()
    {
        Assert.Equal(52655, new SplitMix64Random(0UL).NextFixedPoint().Raw);   // 0xE220…F & 0xFFFF

        var rng = new SplitMix64Random(2024UL);
        for (var i = 0; i < 1000; i++)
        {
            var f = rng.NextFixedPoint();
            Assert.InRange(f.Raw, 0, 65535);       // [0, 65536) == [0, 1)
            Assert.True(f >= FixedPoint.Zero);
            Assert.True(f < FixedPoint.One);
        }
    }

    [Fact] // RT8 — REQ-005
    public void State_Capture_Restore_ReproducesContinuation()
    {
        var rng = new SplitMix64Random(0xDEADBEEFUL);
        for (var i = 0; i < 5; i++)
            rng.NextBits();   // advance past the seed

        var saved = rng.State;
        var original = new ulong[4];
        for (var i = 0; i < 4; i++)
            original[i] = rng.NextBits();

        var restored = new SplitMix64Random(saved);
        for (var i = 0; i < 4; i++)
            Assert.Equal(original[i], restored.NextBits());
    }

    [Fact] // RT10 — REQ-003 (golden bounded sequence; pins the range math so in-range-altering mutants die)
    public void NextInt_Bounded_GoldenSequence()
    {
        var rng = new SplitMix64Random(0UL);
        int[] expected06 = { 1, 0, 1, 4, 1, 0 };
        foreach (var e in expected06)
            Assert.Equal(e, rng.NextInt(0, 6));

        var rng2 = new SplitMix64Random(0UL);
        int[] expectedNeg = { -2, -3, -2, 1, -2, -3 };
        foreach (var e in expectedNeg)
            Assert.Equal(e, rng2.NextInt(-3, 3));
    }
}
