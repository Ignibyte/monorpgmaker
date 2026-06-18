using System;

namespace MonoRpgMaker.Abstractions;

/// <summary>
/// The default deterministic pseudo-random generator (SplitMix64): a 64-bit additive
/// counter with a strong finalizing mix. The generation path is integer-only — no
/// <see cref="float"/>/<see cref="double"/> — so output is bit-identical across CPU /
/// JIT / NativeAOT / architecture (D-0016). Quality is good for game use, not
/// cryptographic; because callers depend only on <see cref="IRandom"/>, the algorithm
/// can be swapped later without touching them. Not thread-safe (a single sim thread owns it).
/// </summary>
public sealed class SplitMix64Random : IRandom
{
    private const ulong Gamma = 0x9E3779B97F4A7C15UL;
    private const ulong MixA = 0xBF58476D1CE4E5B9UL;
    private const ulong MixB = 0x94D049BB133111EBUL;

    private ulong _state;

    /// <summary>
    /// Create a generator seeded with <paramref name="seed"/>. The seed is the initial
    /// state, so passing a captured <see cref="State"/> reproduces that continuation.
    /// </summary>
    public SplitMix64Random(ulong seed) => _state = seed;

    /// <inheritdoc />
    public ulong State => _state;

    /// <inheritdoc />
    public ulong NextBits()
    {
        unchecked
        {
            _state += Gamma;
            var z = _state;
            z = (z ^ (z >> 30)) * MixA;
            z = (z ^ (z >> 27)) * MixB;
            return z ^ (z >> 31);
        }
    }

    /// <inheritdoc />
    public int NextInt() => unchecked((int)(NextBits() >> 32));

    /// <inheritdoc />
    public int NextInt(int minInclusive, int maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxExclusive, minInclusive);

        var range = (ulong)((long)maxExclusive - minInclusive);
        return (int)((long)minInclusive + (long)(NextBits() % range));
    }

    /// <inheritdoc />
    public bool NextBool() => (NextBits() >> 63) != 0UL;

    /// <inheritdoc />
    public FixedPoint NextFixedPoint() => FixedPoint.FromRaw((int)(NextBits() & 0xFFFFUL));
}
