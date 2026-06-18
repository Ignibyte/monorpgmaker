using Microsoft.CodeAnalysis;

namespace MonoRpgMaker.Analyzers;

/// <summary>
/// The diagnostics raised by <see cref="SimDeterminismAnalyzer"/>. Each corresponds to a
/// construct that breaks bit-identical replay in simulation code (D-0016). Ids live in the
/// <c>MRM1xxx</c> band (the <c>MRM0xxx</c> band is reserved for the validator).
/// </summary>
internal static class DeterminismDiagnostics
{
    internal const string Category = "Determinism";

    /// <summary>MRM1001 — <c>float</c>/<c>double</c>/<c>MathF</c>/double-returning <c>Math</c>.</summary>
    internal static readonly DiagnosticDescriptor FloatingPoint = new(
        id: "MRM1001",
        title: "Floating-point math is banned in simulation code",
        messageFormat: "Simulation code must not use '{0}' — floating-point is not bit-identical across CPU/JIT/AOT; use FixedPoint (Q16.16) instead (D-0016)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "float/double/MathF and double-returning System.Math members are non-deterministic across platforms and are banned in the simulation namespaces.");

    /// <summary>MRM1002 — float-backed XNA math types (the <c>Vector2</c> family).</summary>
    internal static readonly DiagnosticDescriptor XnaFloatMath = new(
        id: "MRM1002",
        title: "XNA float-backed math types are banned in simulation code",
        messageFormat: "Simulation code must not use '{0}' (float-backed) — use integer GridPoint / FixedPoint instead (D-0016)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Vector2/Vector3/Vector4/Matrix/Quaternion are float-backed and non-deterministic; the integer XNA Point stays allowed.");

    /// <summary>MRM1003 — <c>foreach</c> over an unordered collection (<c>Dictionary</c>/<c>HashSet</c>).</summary>
    internal static readonly DiagnosticDescriptor DictionaryIteration = new(
        id: "MRM1003",
        title: "foreach over an unordered collection is banned in simulation code",
        messageFormat: "Simulation code must not foreach over '{0}' — its iteration order is unspecified; order it explicitly (with OrderBy) before iterating (D-0016)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Dictionary and HashSet (and Dictionary Keys/Values) enumeration order is unspecified; iterating one makes replay non-deterministic. Ordered collections (SortedDictionary, List, arrays) are allowed.");

    /// <summary>MRM1004 — <see cref="System.Random"/> (non-reproducible RNG).</summary>
    internal static readonly DiagnosticDescriptor NonDeterministicRandom = new(
        id: "MRM1004",
        title: "System.Random is banned in simulation code",
        messageFormat: "Simulation code must not use '{0}' — inject IRandom (a seeded, reproducible RNG) instead (D-0016)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "System.Random is not reproducible; simulation draws must come from the injected deterministic IRandom seam.");

    /// <summary>MRM1005 — ambient clocks (<see cref="System.DateTime"/>/<see cref="System.DateTimeOffset"/>).</summary>
    internal static readonly DiagnosticDescriptor AmbientClock = new(
        id: "MRM1005",
        title: "Ambient clock types are banned in simulation code",
        messageFormat: "Simulation code must not use '{0}' — wall-clock time is non-deterministic; drive time from the simulation tick instead (D-0016)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "DateTime/DateTimeOffset read ambient wall-clock state, which breaks replay; use the deterministic simulation tick.");
}
