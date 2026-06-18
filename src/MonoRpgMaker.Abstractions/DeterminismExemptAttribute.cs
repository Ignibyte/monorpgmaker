using System;

namespace MonoRpgMaker.Abstractions;

/// <summary>
/// Marks a member (or type) as a sanctioned determinism boundary that the sim-determinism analyzer
/// (MRM1001–MRM1005) must not flag — e.g. a debug/serialization conversion that touches
/// <see cref="double"/> on purpose. Use sparingly: the marked code is excluded from the
/// determinism-by-construction guarantee (D-0016), so it must never feed sim state. The canonical
/// use is <see cref="FixedPoint.ToDouble"/> (a diagnostics-only conversion).
/// </summary>
[AttributeUsage(
    AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct,
    AllowMultiple = false,
    Inherited = false)]
public sealed class DeterminismExemptAttribute : Attribute
{
}
