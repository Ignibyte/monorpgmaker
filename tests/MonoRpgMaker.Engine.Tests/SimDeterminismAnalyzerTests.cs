using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using MonoRpgMaker.Analyzers;

namespace MonoRpgMaker.Engine.Tests;

/// <summary>
/// Drives <see cref="SimDeterminismAnalyzer"/> over in-memory C# snippets. The compilation references
/// the exact running net10 runtime (the trusted-platform-assemblies list) rather than the
/// CodeAnalysis.Testing package's pinned reference sets, which predate net10 and would version-mismatch
/// the net10 <c>MonoRpgMaker.Abstractions</c> assembly. MonoGame + Abstractions are on the test's
/// output path, so they appear in that list too — snippets can name <c>Vector2</c>/<c>Point</c>/
/// <c>FixedPoint</c>/<c>DeterminismExempt</c>. Each construct is tested in isolation with an exact
/// diagnostic assertion so the mutation gate has a specific mutant to kill per banned symbol + path.
/// </summary>
public sealed class SimDeterminismAnalyzerTests
{
    private static readonly ImmutableArray<MetadataReference> References =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => path.Length > 0 && path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToImmutableArray();

    [Theory]
    // --- floating point (MRM1001): every introduction form, in isolation ---
    [InlineData("public float Field;", "MRM1001")]
    [InlineData("public double Field;", "MRM1001")]
    [InlineData("public double Method() => 0;", "MRM1001")]                       // method return type
    [InlineData("public void Method(double d) { }", "MRM1001")]                   // parameter
    [InlineData("public void Method() { double d = 0; _ = d; }", "MRM1001")]      // local (var-safe path)
    [InlineData("public float[] Field;", "MRM1001")]                             // array element
    [InlineData("public float[][] Field;", "MRM1001")]                           // jagged array element
    [InlineData("public double? Field;", "MRM1001")]                            // Nullable<T> underlying
    [InlineData("public int M(int n) => (int)System.Math.Sqrt(n);", "MRM1001")]   // double-returning Math
    [InlineData("public int M(int n) => (int)System.MathF.Sin(n);", "MRM1001")]   // any MathF member
    [InlineData("public int M() => (int)System.Math.PI;", "MRM1001")]            // double static field on Math
    [InlineData("public int M() => (int)System.MathF.PI;", "MRM1001")]           // static member on MathF
    [InlineData("public int M(int n) => (int)(n * 1.5);", "MRM1001")]            // inline float arithmetic
    // --- float-backed XNA math (MRM1002): each type in the family ---
    [InlineData("public Vector2 V;", "MRM1002")]
    [InlineData("public Vector3 V;", "MRM1002")]
    [InlineData("public Vector4 V;", "MRM1002")]
    [InlineData("public Matrix M;", "MRM1002")]
    [InlineData("public Quaternion Q;", "MRM1002")]
    [InlineData("public object M() => new Vector2(1, 2);", "MRM1002")]            // object creation path
    // --- unordered enumeration (MRM1003): each unordered source + Keys/Values ---
    [InlineData("public void M(Dictionary<int, int> d) { foreach (var kv in d) { _ = kv.Value; } }", "MRM1003")]
    [InlineData("public void M(Dictionary<int, int> d) { foreach (var k in d.Keys) { _ = k; } }", "MRM1003")]
    [InlineData("public void M(Dictionary<int, int> d) { foreach (var v in d.Values) { _ = v; } }", "MRM1003")]
    [InlineData("public void M(HashSet<int> s) { foreach (var x in s) { _ = x; } }", "MRM1003")]
    // --- non-deterministic RNG (MRM1004) ---
    [InlineData("public Random Rng;", "MRM1004")]
    [InlineData("public object M() => new Random();", "MRM1004")]
    // --- ambient clocks (MRM1005) ---
    [InlineData("public DateTime D;", "MRM1005")]
    [InlineData("public DateTimeOffset D;", "MRM1005")]
    [InlineData("public int M() => DateTime.UtcNow.Hour;", "MRM1005")]            // static ambient read
    [InlineData("public int M() => Environment.TickCount;", "MRM1005")]
    [InlineData("public long M() => Environment.TickCount64;", "MRM1005")]
    public Task Sim_construct_reports_exactly(string body, string id) => AssertExactlyAsync(Sim(body), id);

    [Theory]
    // --- deterministic constructs that must NOT be flagged in sim ---
    [InlineData("public Point P;")]                                              // integer XNA
    [InlineData("public Point M(Point p) => new Point(p.X + 1, p.Y);")]
    [InlineData("public int[] Ints;")]                                          // integer array
    [InlineData("public int M(int n) => System.Math.Max(0, System.Math.Min(10, n));")]  // integer Math overloads
    [InlineData("public TimeSpan T;")]                                          // a pure duration, not a clock
    [InlineData("public TimeSpan M(TimeSpan a, TimeSpan b) => a + b;")]
    [InlineData("public object M() => new Dictionary<int, int>();")]             // Dictionary as a TYPE is allowed
    [InlineData("public void M(List<int> l) { foreach (var x in l) { _ = x; } }")]
    [InlineData("public void M(SortedDictionary<int, int> d) { foreach (var kv in d) { _ = kv.Value; } }")]
    [InlineData("public void M(Dictionary<int, int> d) { foreach (var kv in d.OrderBy(e => e.Key)) { _ = kv.Value; } }")]
    [InlineData("[DeterminismExempt] public double M() => 1.5;")]                // sanctioned boundary
    [InlineData("[DeterminismExempt] public void M(Dictionary<int, int> d) { foreach (var kv in d) { _ = kv.Value; } }")]
    [InlineData("/// <summary><see cref=\"System.Random\"/> <see cref=\"double\"/> <see cref=\"Vector2\"/></summary>\npublic int Ok() => 1;")]
    public Task Sim_construct_is_allowed(string body) => AssertNoneAsync(Sim(body));

    // A property reports once (at the property), not again at its accessors or backing field.
    [Fact]
    public Task Property_of_banned_type_reports_once() => AssertExactlyAsync(
        Sim("public double P { get; set; }"), "MRM1001");

    // A banned type used at BOTH its declaration and its construction reports at each (the intentional
    // double-report) — pins that neither report is dropped.
    [Fact]
    public Task Declaration_and_construction_each_report() => AssertExactlyAsync(
        Sim("public Random R = new Random();"), "MRM1004", "MRM1004");

    // The host/renderer carve-out: none of the bans fire outside the sim namespaces, across every path.
    [Fact]
    public Task Host_namespace_reports_nothing() => AssertNoneAsync(Host(
        "public double Field;\n" +
        "public Vector2 V;\n" +
        "public Random Rng;\n" +
        "public float[] Samples;\n" +
        "public DateTimeOffset Off;\n" +
        "public void Local() { double d = 0; _ = d; }\n" +
        "public object Make() => new Vector2(1, 2);\n" +
        "public int Root(int n) => (int)System.Math.Sqrt(n);\n" +
        "public int Now() => DateTime.UtcNow.Hour;\n" +
        "public int Ticks() => Environment.TickCount;\n" +
        "public float Scale(float x) => x * 1.5f;\n" +
        "public void Iter(Dictionary<int, int> d) { foreach (var kv in d) { _ = kv.Value; } }"));

    // A type in the global namespace is not simulation code.
    [Fact]
    public Task Global_namespace_type_is_not_flagged() =>
        AssertNoneAsync("public class G { public double D; }");

    // The diagnostic points at the offending symbol's location (not Location.None).
    [Fact]
    public async Task Diagnostic_is_reported_at_the_offending_symbol()
    {
        ImmutableArray<Diagnostic> diagnostics = await MrmDiagnosticsAsync(Sim("public double D;"));
        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.NotEqual(Location.None, diagnostic.Location);
        Assert.Equal(6, diagnostic.Location.GetLineSpan().StartLinePosition.Line); // 0-based; the field is source line 7
    }

    // The message names the offending member (the interpolated argument is load-bearing, not cosmetic).
    [Theory]
    [InlineData("public int M(int n) => (int)System.MathF.Sin(n);", "MathF")]
    [InlineData("public int M() => DateTime.UtcNow.Hour;", "DateTime")]
    [InlineData("public void M(Dictionary<int, int> d) { foreach (var kv in d) { _ = kv.Value; } }", "Dictionary")]
    public async Task Diagnostic_message_names_the_offending_symbol(string body, string expectedFragment)
    {
        Diagnostic diagnostic = Assert.Single(await MrmDiagnosticsAsync(Sim(body)));
        Assert.Contains(expectedFragment, diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    // Descriptor shape: Warning severity makes -warnaserror promote each to a build error (REQ-008);
    // the message carries the {0} placeholder + the decision reference an author needs.
    [Fact]
    public void All_descriptors_are_enabled_warnings_with_actionable_text()
    {
        ImmutableArray<DiagnosticDescriptor> descriptors = new SimDeterminismAnalyzer().SupportedDiagnostics;
        Assert.Equal(5, descriptors.Length);
        foreach (DiagnosticDescriptor descriptor in descriptors)
        {
            Assert.Equal(DiagnosticSeverity.Warning, descriptor.DefaultSeverity);
            Assert.True(descriptor.IsEnabledByDefault);
            Assert.Equal("Determinism", descriptor.Category);
            Assert.StartsWith("MRM1", descriptor.Id, StringComparison.Ordinal);
            Assert.NotEmpty(descriptor.Title.ToString(CultureInfo.InvariantCulture));
            Assert.NotEmpty(descriptor.Description.ToString(CultureInfo.InvariantCulture));
            string message = descriptor.MessageFormat.ToString(CultureInfo.InvariantCulture);
            Assert.Contains("{0}", message, StringComparison.Ordinal);
            Assert.Contains("D-0016", message, StringComparison.Ordinal);
        }
    }

    // ---- harness ----
    private static async Task AssertExactlyAsync(string source, params string[] expectedIds)
    {
        string[] actual = await MrmIdsAsync(source);
        Assert.Equal(
            expectedIds.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            actual.OrderBy(id => id, StringComparer.Ordinal).ToArray());
    }

    private static async Task AssertNoneAsync(string source) => Assert.Empty(await MrmIdsAsync(source));

    private static async Task<string[]> MrmIdsAsync(string source)
        => (await MrmDiagnosticsAsync(source)).Select(d => d.Id).ToArray();

    private static async Task<ImmutableArray<Diagnostic>> MrmDiagnosticsAsync(string source)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "SimDeterminismSnippet",
            new[] { tree },
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        CompilationWithAnalyzers withAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new SimDeterminismAnalyzer()));

        ImmutableArray<Diagnostic> diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync();
        return diagnostics.Where(d => d.Id.StartsWith("MRM", StringComparison.Ordinal)).ToImmutableArray();
    }

    private static string Sim(string body) => Wrap("MonoRpgMaker.Engine.Sim", body);

    private static string Host(string body) => Wrap("MonoRpgMaker.Engine.Core", body);

    private static string Wrap(string ns, string body) =>
        "using System;\n" +
        "using System.Collections.Generic;\n" +
        "using System.Linq;\n" +
        "using Microsoft.Xna.Framework;\n" +
        "using MonoRpgMaker.Abstractions;\n" +
        "namespace " + ns + " { public class C {\n" + body + "\n} }";
}
