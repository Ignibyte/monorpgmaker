using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using MonoRpgMaker.Analyzers;
using MonoRpgMaker.Editor.Expectations;
using MonoRpgMaker.Editor.Scaffolding;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

public class EventScaffoldTests
{
    [Fact] // T1 — pure + deterministic: same inputs → byte-identical output
    public void Render_IsDeterministic()
    {
        ScaffoldFiles a = EventScaffold.Render("SampleEvent", "Demo.Ns");
        ScaffoldFiles b = EventScaffold.Render("SampleEvent", "Demo.Ns");

        Assert.Equal(a.CsText, b.CsText);
        Assert.Equal(a.ExpectText, b.ExpectText);
    }

    [Fact] // T2/T6 — the rendered skeleton's structure (kills the {{var}} substitutions + the trailing newline)
    public void Render_Skeleton_HasExpectedShape()
    {
        ScaffoldFiles files = EventScaffold.Render("SampleEvent", "Demo.Ns");

        Assert.Equal("SampleEvent.cs", files.CsFileName);
        Assert.Equal("SampleEvent.expect", files.ExpectFileName);

        string cs = files.CsText;
        Assert.StartsWith(
            "using System.Collections.Generic;\nusing MonoRpgMaker.Abstractions;\n\nnamespace Demo.Ns;\n",
            cs,
            StringComparison.Ordinal);
        Assert.Contains("public sealed class SampleEvent : IMapEvent", cs, StringComparison.Ordinal);
        Assert.Contains("public IReadOnlyList<Outcome> Run(IEventContext context)", cs, StringComparison.Ordinal);
        Assert.Contains("// fill:", cs, StringComparison.Ordinal);
        Assert.Contains("return [];", cs, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", cs, StringComparison.Ordinal);   // every placeholder substituted
        Assert.EndsWith("}\n", cs, StringComparison.Ordinal);        // exactly one trailing newline

        Assert.StartsWith("# SampleEvent.expect", files.ExpectText, StringComparison.Ordinal);
        Assert.EndsWith("=> (none)\n", files.ExpectText, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", files.ExpectText, StringComparison.Ordinal);
    }

    [Fact] // T3 — identifier validation (char rules + reserved-keyword reject)
    public void IsValidIdentifier_AcceptsIdentifiers_RejectsBadAndKeywords()
    {
        Assert.True(EventScaffold.IsValidIdentifier("PascalCase"));
        Assert.True(EventScaffold.IsValidIdentifier("_Underscore"));
        Assert.True(EventScaffold.IsValidIdentifier("A1"));

        Assert.False(EventScaffold.IsValidIdentifier(""));
        Assert.False(EventScaffold.IsValidIdentifier("1Bad"));
        Assert.False(EventScaffold.IsValidIdentifier("has space"));
        Assert.False(EventScaffold.IsValidIdentifier("with-dash"));

        Assert.False(EventScaffold.IsValidIdentifier("class"));
        Assert.False(EventScaffold.IsValidIdentifier("return"));
        Assert.False(EventScaffold.IsValidIdentifier("int"));
        Assert.False(EventScaffold.IsValidIdentifier("namespace"));
    }

    [Fact] // T3 — namespace validation (each dot-segment a valid identifier)
    public void IsValidNamespace_AcceptsDotted_RejectsMalformed()
    {
        Assert.True(EventScaffold.IsValidNamespace("Single"));
        Assert.True(EventScaffold.IsValidNamespace("A.B.C"));

        Assert.False(EventScaffold.IsValidNamespace(""));
        Assert.False(EventScaffold.IsValidNamespace("a..b"));
        Assert.False(EventScaffold.IsValidNamespace("1.x"));
        Assert.False(EventScaffold.IsValidNamespace("not valid"));
        Assert.False(EventScaffold.IsValidNamespace("a.class"));   // a keyword segment
    }

    [Fact] // T4 — the by-construction guarantee: the rendered .cs COMPILES + is MRM-clean in a sim namespace
    public async Task RenderedSkeleton_Compiles_AndIsAnalyzerClean()
    {
        string cs = EventScaffold.Render("SampleEvent", "MonoRpgMaker.Engine.Sim.Tracer").CsText;

        CSharpCompilation compilation = CSharpCompilation.Create(
            "ScaffoldVerify",
            new[] { CSharpSyntaxTree.ParseText(cs) },
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Zero CS errors FIRST — else "analyzer-clean" would be vacuous (PR-claude-analyzer-verifier-resolve-types-001).
        string[] csErrors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString())
            .ToArray();
        Assert.True(csErrors.Length == 0, "rendered .cs must compile; errors: " + string.Join("; ", csErrors));

        CompilationWithAnalyzers withAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new SimDeterminismAnalyzer(), new OutcomeReturnPurityAnalyzer()));
        ImmutableArray<Diagnostic> diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync();

        string[] mrm = diagnostics.Where(d => d.Id.StartsWith("MRM", StringComparison.Ordinal)).Select(d => d.Id).ToArray();
        Assert.Empty(mrm);
    }

    [Fact] // T5 — the rendered .expect parses cleanly with at least one row
    public void RenderedExpect_Parses_WithAtLeastOneRow()
    {
        string expect = EventScaffold.Render("SampleEvent", "Demo.Ns").ExpectText;

        ParseResult result = ExpectationParser.Parse(expect);

        Assert.True(result.Ok);
        Assert.NotEmpty(result.Rows!);
    }

    [Fact] // every reserved keyword is rejected (kills each keyword-set string mutant)
    public void IsValidIdentifier_RejectsEveryReservedKeyword()
    {
        foreach (string keyword in ReservedKeywords)
        {
            Assert.False(EventScaffold.IsValidIdentifier(keyword), keyword + " is a keyword and must be rejected");
        }
    }

    [Fact] // Render guards an invalid name (kills the exception-message mutant)
    public void Render_InvalidName_ThrowsWithMessage()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => EventScaffold.Render("1bad", "Demo.Ns"));
        Assert.Contains("identifier", ex.Message, StringComparison.Ordinal);
    }

    [Fact] // Render guards an invalid namespace (kills the exception-message mutant)
    public void Render_InvalidNamespace_ThrowsWithMessage()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => EventScaffold.Render("Foo", "not valid"));
        Assert.Contains("namespace", ex.Message, StringComparison.Ordinal);
    }

    // The full C# reserved-keyword list — mirrors EventScaffold's set so a dropped/altered entry is caught.
    private static readonly string[] ReservedKeywords =
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class",
        "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event",
        "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if",
        "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null",
        "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly",
        "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct",
        "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
        "using", "virtual", "void", "volatile", "while",
    };

    private static readonly ImmutableArray<MetadataReference> References =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => path.Length > 0 && path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToImmutableArray();
}
