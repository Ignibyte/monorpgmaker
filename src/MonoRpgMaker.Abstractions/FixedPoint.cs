using System;
using System.Globalization;

namespace MonoRpgMaker.Abstractions;

/// <summary>
/// A deterministic Q16.16 fixed-point number: a 32-bit value with 16 integer and 16
/// fractional bits, backed by a single <see cref="Raw"/> <see cref="int"/>. Every
/// operation is integer-only (multiply and divide widen through <see cref="long"/>)
/// and explicitly <c>unchecked</c>, so a result is bit-identical across CPU / JIT /
/// NativeAOT / architecture — the determinism primitive sim handlers use instead of
/// <see cref="float"/>/<see cref="double"/> (D-0016). The representable whole-number
/// range is [-32768, 32767]; overflow wraps deterministically.
/// </summary>
public readonly struct FixedPoint : IEquatable<FixedPoint>, IComparable<FixedPoint>
{
    /// <summary>The number of fractional bits in the Q16.16 layout.</summary>
    public const int FractionalBits = 16;

    private const int OneRaw = 1 << FractionalBits;   // 65536
    private const int HalfRaw = OneRaw / 2;           // 32768

    private FixedPoint(int raw) => Raw = raw;

    /// <summary>The value 0.</summary>
    public static readonly FixedPoint Zero = new(0);

    /// <summary>The value 1.</summary>
    public static readonly FixedPoint One = new(OneRaw);

    /// <summary>The underlying Q16.16 integer value.</summary>
    public int Raw { get; }

    /// <summary>Create a value from a whole number (range [-32768, 32767]).</summary>
    public static FixedPoint FromInt(int value) => new(unchecked(value << FractionalBits));

    /// <summary>Create a value directly from a Q16.16 <paramref name="raw"/> integer.</summary>
    public static FixedPoint FromRaw(int raw) => new(raw);

    /// <summary>Adds two values; overflow wraps.</summary>
    public static FixedPoint operator +(FixedPoint a, FixedPoint b) => new(unchecked(a.Raw + b.Raw));

    /// <summary>Subtracts <paramref name="b"/> from <paramref name="a"/>; overflow wraps.</summary>
    public static FixedPoint operator -(FixedPoint a, FixedPoint b) => new(unchecked(a.Raw - b.Raw));

    /// <summary>Negates a value; overflow wraps.</summary>
    public static FixedPoint operator -(FixedPoint a) => new(unchecked(-a.Raw));

    /// <summary>Multiplies two values, widening through <see cref="long"/> to hold the scale.</summary>
    public static FixedPoint operator *(FixedPoint a, FixedPoint b) =>
        new(unchecked((int)(((long)a.Raw * b.Raw) >> FractionalBits)));

    /// <summary>Divides <paramref name="a"/> by <paramref name="b"/> (widened through <see cref="long"/>); truncates toward zero.</summary>
    public static FixedPoint operator /(FixedPoint a, FixedPoint b) =>
        new(unchecked((int)(((long)a.Raw << FractionalBits) / b.Raw)));

    /// <summary>Whether two values are equal.</summary>
    public static bool operator ==(FixedPoint a, FixedPoint b) => a.Raw == b.Raw;

    /// <summary>Whether two values are unequal.</summary>
    public static bool operator !=(FixedPoint a, FixedPoint b) => a.Raw != b.Raw;

    /// <summary>Whether <paramref name="a"/> is less than <paramref name="b"/>.</summary>
    public static bool operator <(FixedPoint a, FixedPoint b) => a.Raw < b.Raw;

    /// <summary>Whether <paramref name="a"/> is greater than <paramref name="b"/>.</summary>
    public static bool operator >(FixedPoint a, FixedPoint b) => a.Raw > b.Raw;

    /// <summary>Whether <paramref name="a"/> is less than or equal to <paramref name="b"/>.</summary>
    public static bool operator <=(FixedPoint a, FixedPoint b) => a.Raw <= b.Raw;

    /// <summary>Whether <paramref name="a"/> is greater than or equal to <paramref name="b"/>.</summary>
    public static bool operator >=(FixedPoint a, FixedPoint b) => a.Raw >= b.Raw;

    /// <summary>The absolute value; the minimum representable value wraps.</summary>
    public static FixedPoint Abs(FixedPoint value) => new(value.Raw < 0 ? unchecked(-value.Raw) : value.Raw);

    /// <summary>The smaller of two values.</summary>
    public static FixedPoint Min(FixedPoint a, FixedPoint b) => a.Raw <= b.Raw ? a : b;

    /// <summary>The larger of two values.</summary>
    public static FixedPoint Max(FixedPoint a, FixedPoint b) => a.Raw >= b.Raw ? a : b;

    /// <summary>The largest whole value not greater than this one (toward negative infinity).</summary>
    public FixedPoint Floor() => FromInt(Raw >> FractionalBits);

    /// <summary>The smallest whole value not less than this one (toward positive infinity).</summary>
    public FixedPoint Ceiling() => FromInt(unchecked(Raw + (OneRaw - 1)) >> FractionalBits);

    /// <summary>The nearest whole value, rounding halves away from zero.</summary>
    public FixedPoint Round() =>
        Raw >= 0
            ? FromInt(unchecked(Raw + HalfRaw) >> FractionalBits)
            : FromInt(-(unchecked(-Raw + HalfRaw) >> FractionalBits));

    /// <summary>Truncates toward zero to a whole <see cref="int"/>.</summary>
    public int ToInt() => Raw / OneRaw;

    /// <summary>Converts to <see cref="double"/> — diagnostics and tests only; never use in sim math.</summary>
    public double ToDouble() => (double)Raw / OneRaw;

    /// <inheritdoc />
    public bool Equals(FixedPoint other) => Raw == other.Raw;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is FixedPoint other && Raw == other.Raw;

    /// <inheritdoc />
    public override int GetHashCode() => Raw;

    /// <inheritdoc />
    public int CompareTo(FixedPoint other) => Raw.CompareTo(other.Raw);

    /// <summary>A culture-invariant decimal rendering (diagnostics).</summary>
    public override string ToString() => ToDouble().ToString("0.################", CultureInfo.InvariantCulture);
}
