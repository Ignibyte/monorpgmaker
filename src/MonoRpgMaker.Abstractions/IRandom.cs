namespace MonoRpgMaker.Abstractions;

/// <summary>
/// The injected, deterministic randomness seam — the ONLY randomness source sim code
/// may draw from (<see cref="System.Random"/> is non-reproducible and banned, D-0016).
/// An implementation is seeded and integer-only, and exposes its <see cref="State"/> so
/// that a captured value reproduces an identical continuation of the sequence (the basis
/// for save persistence and the replay-to-same-hash gate). Every <c>Next*</c> call
/// advances the state exactly once.
/// </summary>
public interface IRandom
{
    /// <summary>The raw 64-bit generator output. Advances the state once.</summary>
    ulong NextBits();

    /// <summary>A full-range <see cref="int"/> (may be negative). Advances the state once.</summary>
    int NextInt();

    /// <summary>A value in <c>[minInclusive, maxExclusive)</c>. Advances the state once.</summary>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <paramref name="maxExclusive"/> is not greater than <paramref name="minInclusive"/>.
    /// </exception>
    int NextInt(int minInclusive, int maxExclusive);

    /// <summary>A deterministic coin flip. Advances the state once.</summary>
    bool NextBool();

    /// <summary>A <see cref="FixedPoint"/> in <c>[0,1)</c>. Advances the state once.</summary>
    FixedPoint NextFixedPoint();

    /// <summary>
    /// The current generator state — capture it to reproduce this generator's continuation
    /// later (the basis for save/replay reproducibility).
    /// </summary>
    ulong State { get; }
}
