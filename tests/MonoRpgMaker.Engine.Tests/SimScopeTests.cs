using MonoRpgMaker.Analyzers;

namespace MonoRpgMaker.Engine.Tests;

/// <summary>
/// Unit tests for the analyzer's pure scope helper. These exercise the highest-logic-density part of
/// <c>SimDeterminismAnalyzer</c> directly (the segment-aware namespace match), so the mutation gate
/// has plain assertions to kill — distinct from the snippet-driven verifier tests.
/// </summary>
public sealed class SimScopeTests
{
    [Theory]
    [InlineData("MonoRpgMaker.Engine.World")]
    [InlineData("MonoRpgMaker.Engine.Entities")]
    [InlineData("MonoRpgMaker.Engine.Data")]
    [InlineData("MonoRpgMaker.Engine.Sim")]
    [InlineData("MonoRpgMaker.Abstractions")]
    [InlineData("MonoRpgMaker.Engine.Sim.Tracer")]   // nested under a sim prefix
    [InlineData("MonoRpgMaker.Engine.World.Detail")]  // nested under a sim prefix
    public void IsSimNamespace_True_ForSimNamespaces(string ns)
        => Assert.True(SimScope.IsSimNamespace(ns));

    [Theory]
    [InlineData("MonoRpgMaker.Engine.Core")]          // the MonoGame host
    [InlineData("MonoRpgMaker.Player")]
    [InlineData("MonoRpgMaker.Editor")]
    [InlineData("MonoRpgMaker.Engine.Database")]      // segment-boundary: must NOT match "...Data"
    [InlineData("MonoRpgMaker.Engine.Simulation")]    // segment-boundary: must NOT match "...Sim"
    [InlineData("MonoRpgMaker.Engine")]               // a parent of the sim namespaces, not itself sim
    [InlineData("System")]
    [InlineData("")]
    [InlineData(null)]
    public void IsSimNamespace_False_ForHostAndPrefixCollisions(string? ns)
        => Assert.False(SimScope.IsSimNamespace(ns));
}
