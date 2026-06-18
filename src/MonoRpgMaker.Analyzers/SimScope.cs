using System;
using Microsoft.CodeAnalysis;

namespace MonoRpgMaker.Analyzers;

/// <summary>
/// Pure scope/exemption helpers for <see cref="SimDeterminismAnalyzer"/>. Kept free of Roslyn
/// action plumbing so the decision logic is directly unit-testable (and mutation-testable).
/// </summary>
internal static class SimScope
{
    /// <summary>
    /// The simulation namespaces whose code must stay deterministic (D-0016). Mirrors the boundary
    /// asserted in <c>ArchitectureTests</c>. Host namespaces (<c>MonoRpgMaker.Engine.Core</c>, the
    /// Player, the Editor) are deliberately absent — float/Vector2/clocks are legitimate there.
    /// </summary>
    private static readonly string[] SimNamespacePrefixes =
    {
        "MonoRpgMaker.Engine.World",
        "MonoRpgMaker.Engine.Entities",
        "MonoRpgMaker.Engine.Data",
        "MonoRpgMaker.Engine.Sim",
        "MonoRpgMaker.Abstractions",
    };

    /// <summary>
    /// Whether <paramref name="namespaceName"/> is (or is nested under) a simulation namespace.
    /// Segment-aware: a prefix matches only on a full segment boundary, so
    /// <c>MonoRpgMaker.Engine.Database</c> does not match the <c>...Data</c> prefix and
    /// <c>MonoRpgMaker.Engine.Sim.Tracer</c> does match the <c>...Sim</c> prefix.
    /// </summary>
    internal static bool IsSimNamespace(string? namespaceName)
    {
        if (string.IsNullOrEmpty(namespaceName))
        {
            return false;
        }

        foreach (string prefix in SimNamespacePrefixes)
        {
            if (string.Equals(namespaceName, prefix, StringComparison.Ordinal) ||
                namespaceName!.StartsWith(prefix + ".", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether <paramref name="symbol"/> is declared in a simulation namespace.</summary>
    internal static bool IsInSimNamespace(ISymbol? symbol)
    {
        INamespaceSymbol? containing = symbol?.ContainingNamespace;
        return containing is not null
            && !containing.IsGlobalNamespace
            && IsSimNamespace(containing.ToDisplayString());
    }

    /// <summary>
    /// Whether <paramref name="symbol"/> — or any symbol enclosing it (member, then type) — is
    /// marked with <paramref name="exemptAttribute"/>, the sanctioned-boundary opt-out. Returns
    /// <see langword="false"/> when the attribute type is absent from the compilation.
    /// </summary>
    internal static bool IsExempt(ISymbol? symbol, INamedTypeSymbol? exemptAttribute)
    {
        if (exemptAttribute is null)
        {
            return false;
        }

        for (ISymbol? current = symbol; current is not null; current = current.ContainingSymbol)
        {
            foreach (AttributeData attribute in current.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, exemptAttribute))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
