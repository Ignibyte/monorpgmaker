using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Analyzers;
using MonoRpgMaker.Engine.Sim;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

/// <summary>
/// Drives <see cref="OutcomeReturnPurityAnalyzer"/> (MRM1006, D-0017) over in-memory C# snippets, mirroring
/// the <see cref="SimDeterminismAnalyzer"/> harness (the running net10 trusted-platform-assemblies as
/// references, so snippets can name the real <c>MonoRpgMaker.Engine.Sim.GameState</c> + <c>Outcome</c>).
/// Each snippet is asserted in isolation with an exact diagnostic count, and is first checked to COMPILE
/// (zero CS errors) — an unresolved type yields no bound invocation and would silently pass a negative for
/// the wrong reason (PR-claude-analyzer-verifier-resolve-types-001). Every negative shares its body with the
/// positive <see cref="Handler_CallingMutator_Directly_FlagsMrm1006"/> control, varying one condition.
/// </summary>
public sealed class OutcomeReturnPurityAnalyzerTests
{
    // A handler that calls the real GameState.Set — fully qualified so it resolves in any snippet namespace.
    private const string MutateCall = "new MonoRpgMaker.Engine.Sim.GameState().Set(\"k\", true);";

    // ---- positives: a [StateMutator] call inside an outcome-returning handler → MRM1006 (REQ-001) ----

    [Fact] // P1 — direct call in the handler body
    public Task Handler_CallingMutator_Directly_FlagsMrm1006() => AssertExactlyAsync(
        Sim("public IReadOnlyList<Outcome> Run() { " + MutateCall + " return new List<Outcome>(); }"),
        "MRM1006");

    [Fact] // P2 — the call hides in a nested local function of the handler
    public Task Handler_MutatingViaLocalFunction_FlagsMrm1006() => AssertExactlyAsync(
        Sim("public IReadOnlyList<Outcome> Run() { void Inner() { " + MutateCall + " } Inner(); return new List<Outcome>(); }"),
        "MRM1006");

    [Fact] // P3 — the call hides in a lambda of the handler (the riskiest scope case)
    public Task Handler_MutatingViaLambda_FlagsMrm1006() => AssertExactlyAsync(
        Sim("public IReadOnlyList<Outcome> Run() { Action a = () => { " + MutateCall + " }; a(); return new List<Outcome>(); }"),
        "MRM1006");

    // ---- negatives: each shares P1's body, varying ONE condition → no MRM1006 ----

    [Fact] // N1 — a void method (the OutcomeApplier shape) may mutate freely (REQ-002)
    public Task VoidMethod_CallingMutator_IsNotFlagged() => AssertNoneAsync(
        Sim("public void Apply() { " + MutateCall + " }"));

    [Fact] // N2 — a handler that returns outcomes without mutating is clean (REQ-004)
    public Task Handler_ReturningOutcomesWithoutMutating_IsNotFlagged() => AssertNoneAsync(
        Sim("public IReadOnlyList<Outcome> Run() { return new List<Outcome> { new SetSwitch(\"k\", true) }; }"));

    [Fact] // N3 — the same handler-mutates body, but in a host namespace → out of scope (REQ-007)
    public Task Handler_CallingMutator_InHostNamespace_IsNotFlagged() => AssertNoneAsync(
        Host("public IReadOnlyList<Outcome> Run() { " + MutateCall + " return new List<Outcome>(); }"));

    [Fact] // N6 — IReadOnlyList<string> is not the handler shape (TypeArguments[0] must be Outcome)
    public Task Method_ReturningReadOnlyListOfNonOutcome_IsNotFlagged() => AssertNoneAsync(
        Sim("public IReadOnlyList<string> Run() { " + MutateCall + " return new List<string>(); }"));

    [Fact] // N7 — IReadOnlyList<SetSwitch> is an exact-match miss (a subtype, not Outcome itself)
    public Task Method_ReturningReadOnlyListOfOutcomeSubtype_IsNotFlagged() => AssertNoneAsync(
        Sim("public IReadOnlyList<SetSwitch> Run() { " + MutateCall + " return new List<SetSwitch>(); }"));

    [Fact] // N8 — a concrete List<Outcome> is not the IReadOnlyList<Outcome> seam (OriginalDefinition miss)
    public Task Method_ReturningConcreteListOfOutcome_IsNotFlagged() => AssertNoneAsync(
        Sim("public List<Outcome> Run() { " + MutateCall + " return new List<Outcome>(); }"));

    // ---- REQ-003: the real mutators carry [StateMutator]; the readers do not ----

    [Fact]
    public void GameStateMutators_BearStateMutatorAttribute_ButReadersDoNot()
    {
        Assert.NotNull(typeof(GameState).GetMethod(nameof(GameState.Set))!.GetCustomAttribute<StateMutatorAttribute>());
        Assert.NotNull(typeof(GameState).GetMethod(nameof(GameState.Add))!.GetCustomAttribute<StateMutatorAttribute>());
        Assert.Null(typeof(GameState).GetMethod(nameof(GameState.Get))!.GetCustomAttribute<StateMutatorAttribute>());
        Assert.Null(typeof(GameState).GetMethod(nameof(GameState.GetCount))!.GetCustomAttribute<StateMutatorAttribute>());
    }

    // ---- REQ-005/006: the descriptor shape ----

    [Fact]
    public void Mrm1006_Descriptor_IsWellFormed()
    {
        DiagnosticDescriptor descriptor = Assert.Single(new OutcomeReturnPurityAnalyzer().SupportedDiagnostics);

        Assert.Equal("MRM1006", descriptor.Id);
        Assert.StartsWith("MRM1", descriptor.Id, StringComparison.Ordinal);
        Assert.Equal("Determinism", descriptor.Category);
        Assert.Equal(DiagnosticSeverity.Warning, descriptor.DefaultSeverity);
        Assert.True(descriptor.IsEnabledByDefault);
        Assert.NotEmpty(descriptor.Title.ToString(CultureInfo.InvariantCulture));
        Assert.NotEmpty(descriptor.Description.ToString(CultureInfo.InvariantCulture));

        string message = descriptor.MessageFormat.ToString(CultureInfo.InvariantCulture);
        Assert.Contains("{0}", message, StringComparison.Ordinal);
        Assert.Contains("D-0017", message, StringComparison.Ordinal);
    }

    // ---- harness ----

    private static readonly ImmutableArray<MetadataReference> References =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => path.Length > 0 && path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToImmutableArray();

    private static async Task AssertExactlyAsync(string source, params string[] expectedIds)
    {
        string[] actual = await MrmIdsAsync(source);
        Assert.Equal(
            expectedIds.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            actual.OrderBy(id => id, StringComparer.Ordinal).ToArray());
    }

    private static async Task AssertNoneAsync(string source) => Assert.Empty(await MrmIdsAsync(source));

    private static async Task<string[]> MrmIdsAsync(string source)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "OutcomeReturnPuritySnippet",
            new[] { tree },
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // A snippet that does not compile yields no bound invocations, so the analyzer would report
        // nothing — a false-negative green (PR-claude-analyzer-verifier-resolve-types-001). Fail loudly instead.
        string[] compileErrors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString())
            .ToArray();
        Assert.True(compileErrors.Length == 0, "snippet must compile; errors: " + string.Join("; ", compileErrors));

        CompilationWithAnalyzers withAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new OutcomeReturnPurityAnalyzer()));

        ImmutableArray<Diagnostic> diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync();
        return diagnostics
            .Where(d => d.Id.StartsWith("MRM", StringComparison.Ordinal))
            .Select(d => d.Id)
            .ToArray();
    }

    private static string Sim(string body) => Wrap("MonoRpgMaker.Engine.Sim", body);

    private static string Host(string body) => Wrap("MonoRpgMaker.Engine.Core", body);

    private static string Wrap(string ns, string body) =>
        "using System;\n" +
        "using System.Collections.Generic;\n" +
        "using MonoRpgMaker.Abstractions;\n" +
        "namespace " + ns + " { public class C {\n" + body + "\n} }";
}
