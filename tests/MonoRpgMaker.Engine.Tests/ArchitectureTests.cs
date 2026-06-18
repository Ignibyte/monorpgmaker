using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Engine.World;
using NetArchTest.Rules;

namespace MonoRpgMaker.Engine.Tests;

/// <summary>
/// Architecture guardrails (CONSTITUTION §14 / CLAUDE.md): the engine stays
/// framework-thin and simulation state is kept separate from rendering. These
/// run as ordinary xUnit tests, so a layering violation turns <c>bin/gate.sh</c>
/// red like any other failing test instead of living only as prose.
/// </summary>
public sealed class ArchitectureTests
{
    // Any Engine type anchors the assembly under inspection.
    private static System.Reflection.Assembly EngineAssembly => typeof(Tile).Assembly;

    [Fact]
    public void Engine_must_not_depend_on_Player_or_Editor()
    {
        var result = Types.InAssembly(EngineAssembly)
            .Should()
            .NotHaveDependencyOnAny("MonoRpgMaker.Player", "MonoRpgMaker.Editor")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            FailureMessage("the Engine must not reference the Player or Editor hosts", result));
    }

    [Fact]
    public void Simulation_namespaces_must_not_depend_on_rendering()
    {
        // World / Entities / Data / Sim are pure simulation: they may use the integer XNA
        // Point, but NOT float-backed math like Vector2 (banned in sim by the determinism
        // analyzer, MRM1002), and must never reach into Graphics (Texture2D, SpriteBatch,
        // GraphicsDevice). This test guards the coarser layering rule (simulation stays
        // separate from rendering); the analyzer owns the construct-level determinism bans.
        var result = Types.InAssembly(EngineAssembly)
            .That()
            .ResideInNamespaceStartingWith("MonoRpgMaker.Engine.World")
            .Or().ResideInNamespaceStartingWith("MonoRpgMaker.Engine.Entities")
            .Or().ResideInNamespaceStartingWith("MonoRpgMaker.Engine.Data")
            .Or().ResideInNamespaceStartingWith("MonoRpgMaker.Engine.Sim")
            .Should()
            .NotHaveDependencyOn("Microsoft.Xna.Framework.Graphics")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            FailureMessage("simulation namespaces must not depend on Microsoft.Xna.Framework.Graphics", result));
    }

    [Fact]
    public void Abstractions_must_not_depend_on_MonoGame_Engine_or_hosts()
    {
        // The published seam surface (MonoRpgMaker.Abstractions) is pure: it must not
        // reach down into MonoGame, the Engine, or the hosts — the "Project →
        // Abstractions only" ring (D-0014). The agent-authored project layer programs
        // against this assembly, so it stays dependency-free.
        var result = Types.InAssembly(typeof(FixedPoint).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.Xna.Framework",
                "MonoRpgMaker.Engine",
                "MonoRpgMaker.Player",
                "MonoRpgMaker.Editor")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            FailureMessage("MonoRpgMaker.Abstractions must stay pure (no MonoGame / Engine / host dependency)", result));
    }

    private static string FailureMessage(string rule, TestResult result)
    {
        var offenders = result.FailingTypeNames is null
            ? "(none reported)"
            : string.Join(", ", result.FailingTypeNames);
        return $"Architecture rule violated — {rule}. Offending types: {offenders}.";
    }
}
