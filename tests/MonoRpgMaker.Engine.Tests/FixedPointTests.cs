using System;
using MonoRpgMaker.Abstractions;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

/// <summary>
/// Q16.16 raw reference values used across the cases below (value × 65536):
/// 0.25=16384, 0.5=32768, 1.5=98304, 2.0=131072, 2.4≈157286, 2.25=147456,
/// 2.5=163840, 3.75=245760.
/// </summary>
public class FixedPointConstructionTests
{
    [Theory] // FP1 — REQ-001
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(7)]
    [InlineData(-7)]
    [InlineData(32767)]
    [InlineData(-32768)]
    public void FromInt_RoundTrips_AndScalesByOneRaw(int n)
    {
        var fp = FixedPoint.FromInt(n);

        Assert.Equal(n, fp.ToInt());
        Assert.Equal(n * 65536, fp.Raw);   // pins the << 16 scale
    }

    [Fact]
    public void FromRaw_ExposesRaw()
    {
        Assert.Equal(12345, FixedPoint.FromRaw(12345).Raw);
        Assert.Equal(-12345, FixedPoint.FromRaw(-12345).Raw);
    }

    [Fact]
    public void Constants_AreExact()
    {
        Assert.Equal(0, FixedPoint.Zero.Raw);
        Assert.Equal(65536, FixedPoint.One.Raw);
        Assert.Equal(16, FixedPoint.FractionalBits);
    }
}

public class FixedPointArithmeticTests
{
    [Fact] // FP2 — REQ-002
    public void Add_Sub_Negate_AreExact()
    {
        Assert.Equal(245760, (FixedPoint.FromRaw(98304) + FixedPoint.FromRaw(147456)).Raw);   // 1.5 + 2.25 = 3.75
        Assert.Equal(147456, (FixedPoint.FromRaw(245760) - FixedPoint.FromRaw(98304)).Raw);   // 3.75 - 1.5 = 2.25
        Assert.Equal(-131072, (FixedPoint.FromInt(1) - FixedPoint.FromInt(3)).Raw);            // 1 - 3 = -2
        Assert.Equal(-163840, (-FixedPoint.FromRaw(163840)).Raw);                              // -(2.5) = -2.5
        Assert.Equal(163840, (-FixedPoint.FromRaw(-163840)).Raw);                              // -(-2.5) = 2.5
    }

    [Fact] // FP3 — REQ-002 (Int64-widened multiply)
    public void Multiply_IsExact()
    {
        Assert.Equal(16384, (FixedPoint.FromRaw(32768) * FixedPoint.FromRaw(32768)).Raw);   // 0.5 × 0.5 = 0.25
        Assert.Equal(655360, (FixedPoint.FromRaw(163840) * FixedPoint.FromInt(4)).Raw);     // 2.5 × 4 = 10
        Assert.Equal(-393216, (FixedPoint.FromInt(-2) * FixedPoint.FromInt(3)).Raw);        // -2 × 3 = -6
        Assert.Equal(147456, (FixedPoint.FromRaw(98304) * FixedPoint.FromRaw(98304)).Raw);  // 1.5 × 1.5 = 2.25
    }

    [Fact] // FP4 — REQ-002 (divide truncates toward zero)
    public void Divide_IsExact_TruncatingTowardZero()
    {
        Assert.Equal(163840, (FixedPoint.FromInt(10) / FixedPoint.FromInt(4)).Raw);   // 10 / 4 = 2.5
        Assert.Equal(32768, (FixedPoint.FromInt(1) / FixedPoint.FromInt(2)).Raw);     // 1 / 2 = 0.5
        Assert.Equal(-131072, (FixedPoint.FromInt(-6) / FixedPoint.FromInt(3)).Raw);  // -6 / 3 = -2
        Assert.Equal(218453, (FixedPoint.FromInt(10) / FixedPoint.FromInt(3)).Raw);   // 10 / 3 = 3.3333… (truncated)
    }

    [Fact] // from inspect finding #2
    public void Divide_ByZero_Throws()
    {
        Assert.Throws<DivideByZeroException>(() => FixedPoint.FromInt(1) / FixedPoint.Zero);
    }
}

public class FixedPointComparisonTests
{
    [Fact] // FP5 — REQ-003
    public void Equality_Operators_Object_And_Hash()
    {
        var a = FixedPoint.FromRaw(100);
        var b = FixedPoint.FromRaw(100);
        var c = FixedPoint.FromRaw(200);

        Assert.True(a == b);
        Assert.False(a == c);
        Assert.True(a != c);
        Assert.False(a != b);

        Assert.True(a.Equals(b));
        Assert.False(a.Equals(c));
        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals((object)c));
        Assert.False(a.Equals("not a fixed point"));
        Assert.False(a.Equals(null));

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a.GetHashCode(), c.GetHashCode());
    }

    [Fact] // FP5 — REQ-003 (ordering operators)
    public void Ordering_Operators()
    {
        var lo = FixedPoint.FromRaw(100);
        var hi = FixedPoint.FromRaw(200);
        var eq = FixedPoint.FromRaw(100);   // equal to lo; a distinct var avoids the CS1718 self-compare warning while still pinning the reflexive `<` vs `<=` boundary

        Assert.True(lo < hi);
        Assert.False(hi < lo);
        Assert.False(lo < eq);

        Assert.True(hi > lo);
        Assert.False(lo > hi);
        Assert.False(lo > eq);

        Assert.True(lo <= hi);
        Assert.True(lo <= eq);
        Assert.False(hi <= lo);

        Assert.True(hi >= lo);
        Assert.True(lo >= eq);
        Assert.False(lo >= hi);
    }

    [Fact] // FP6 — REQ-003
    public void CompareTo_ReturnsSign()
    {
        var lo = FixedPoint.FromRaw(100);
        var hi = FixedPoint.FromRaw(200);

        Assert.True(lo.CompareTo(hi) < 0);
        Assert.True(hi.CompareTo(lo) > 0);
        Assert.Equal(0, lo.CompareTo(FixedPoint.FromRaw(100)));
    }
}

public class FixedPointHelperTests
{
    [Fact] // FP7 — REQ-004
    public void Abs_Min_Max()
    {
        Assert.Equal(163840, FixedPoint.Abs(FixedPoint.FromRaw(-163840)).Raw);  // |-2.5| = 2.5
        Assert.Equal(163840, FixedPoint.Abs(FixedPoint.FromRaw(163840)).Raw);   // |2.5| = 2.5
        Assert.Equal(0, FixedPoint.Abs(FixedPoint.Zero).Raw);

        var a = FixedPoint.FromRaw(100);
        var b = FixedPoint.FromRaw(200);
        Assert.Equal(100, FixedPoint.Min(a, b).Raw);
        Assert.Equal(100, FixedPoint.Min(b, a).Raw);
        Assert.Equal(200, FixedPoint.Max(a, b).Raw);
        Assert.Equal(200, FixedPoint.Max(b, a).Raw);
        Assert.Equal(100, FixedPoint.Min(a, a).Raw);   // tie
        Assert.Equal(100, FixedPoint.Max(a, a).Raw);   // tie
    }

    [Fact] // FP8 — REQ-004 (toward-zero truncation, distinct from Floor)
    public void ToInt_TruncatesTowardZero()
    {
        Assert.Equal(2, FixedPoint.FromRaw(163840).ToInt());    // 2.5 → 2
        Assert.Equal(-2, FixedPoint.FromRaw(-163840).ToInt());  // -2.5 → -2 (NOT -3)
        Assert.Equal(0, FixedPoint.FromRaw(-32768).ToInt());    // -0.5 → 0
    }

    [Fact] // FP8 — REQ-004
    public void Floor_Ceiling_Round_PosNegExactHalf()
    {
        Assert.Equal(2, FixedPoint.FromRaw(163840).Floor().ToInt());    // floor 2.5 = 2
        Assert.Equal(-3, FixedPoint.FromRaw(-163840).Floor().ToInt());  // floor -2.5 = -3 (toward -inf)
        Assert.Equal(2, FixedPoint.FromRaw(131072).Floor().ToInt());    // floor 2.0 = 2

        Assert.Equal(3, FixedPoint.FromRaw(163840).Ceiling().ToInt());   // ceil 2.5 = 3
        Assert.Equal(2, FixedPoint.FromRaw(131072).Ceiling().ToInt());   // ceil 2.0 = 2 (exact, no round-up)
        Assert.Equal(-2, FixedPoint.FromRaw(-163840).Ceiling().ToInt()); // ceil -2.5 = -2

        Assert.Equal(3, FixedPoint.FromRaw(163840).Round().ToInt());     // round 2.5 = 3 (half away)
        Assert.Equal(2, FixedPoint.FromRaw(157286).Round().ToInt());     // round 2.4 = 2
        Assert.Equal(-3, FixedPoint.FromRaw(-163840).Round().ToInt());   // round -2.5 = -3 (half away)
        Assert.Equal(-2, FixedPoint.FromRaw(-157286).Round().ToInt());   // round -2.4 = -2
    }
}

public class FixedPointFormattingTests
{
    [Fact] // FP9 — REQ-004
    public void ToDouble_And_ToString_AreExactAndInvariant()
    {
        Assert.Equal(2.5, FixedPoint.FromRaw(163840).ToDouble());
        Assert.Equal(-0.25, FixedPoint.FromRaw(-16384).ToDouble());

        Assert.Equal("2.5", FixedPoint.FromRaw(163840).ToString());
        Assert.Equal("-0.25", FixedPoint.FromRaw(-16384).ToString());
        Assert.Equal("0", FixedPoint.Zero.ToString());
        Assert.Equal("1", FixedPoint.One.ToString());
    }

    [Fact] // FP10 — REQ-002 (determinism: documented unchecked wrap)
    public void Addition_OverflowWraps_Deterministically()
    {
        var max = FixedPoint.FromRaw(int.MaxValue);
        Assert.Equal(int.MinValue, (max + FixedPoint.FromRaw(1)).Raw);
    }
}
