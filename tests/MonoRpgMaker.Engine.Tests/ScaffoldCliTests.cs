using System;
using System.IO;
using MonoRpgMaker.Editor;
using MonoRpgMaker.Editor.Scaffolding;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

public class ScaffoldCliTests
{
    private static readonly string[] MissingArgs = { "scaffold", "event", "Foo" };
    private static readonly string[] CheckMissingDir = { "check-expectations", "/no/such/dir/mrm-scaffold-x" };
    private static readonly string[] UnknownVerb = { "frobnicate" };

    [Fact] // T7 — scaffold writes the .cs + .expect pair (rendered content), exit 0 + a confirming message
    public void Run_Scaffold_WritesPair()
    {
        RunInTempDir(dir =>
        {
            var writer = new StringWriter();
            int code = ScaffoldCli.Run(new[] { "scaffold", "event", "Foo", dir }, writer);

            Assert.Equal(0, code);
            Assert.Contains("scaffolded", writer.ToString(), StringComparison.Ordinal);
            string cs = File.ReadAllText(Path.Combine(dir, "Foo.cs"));
            Assert.True(File.Exists(Path.Combine(dir, "Foo.expect")));
            Assert.Contains("public sealed class Foo : IMapEvent", cs, StringComparison.Ordinal);
            Assert.Contains("namespace MonoRpgMaker.Engine.Sim.Tracer;", cs, StringComparison.Ordinal);
        });
    }

    [Fact] // T8 — usage / invalid input → exit 2, with a message naming the problem (no file for the keyword case)
    public void Run_InvalidInput_ExitTwo()
    {
        RunInTempDir(dir =>
        {
            Assert.Equal(2, RunWrite(MissingArgs, out string usage));
            Assert.Contains("usage", usage, StringComparison.Ordinal);

            Assert.Equal(2, RunWrite(new[] { "scaffold", "event", "1x", dir }, out string badId));
            Assert.Contains("identifier", badId, StringComparison.Ordinal);

            Assert.Equal(2, RunWrite(new[] { "scaffold", "event", "class", dir }, out _));   // C# keyword
            Assert.False(File.Exists(Path.Combine(dir, "class.cs")));

            Assert.Equal(2, RunWrite(new[] { "scaffold", "event", "Foo", dir, "--namespace", "not valid" }, out string badNs));
            Assert.Contains("namespace", badNs, StringComparison.Ordinal);
        });
    }

    [Fact] // T8 — refuse to overwrite when EITHER target exists (kills the || mutation), no clobber
    public void Run_RefusesToOverwrite_EitherFile()
    {
        RunInTempDir(dir =>
        {
            Assert.Equal(0, RunWrite(new[] { "scaffold", "event", "Foo", dir }, out _));
            string csBefore = File.ReadAllText(Path.Combine(dir, "Foo.cs"));

            Assert.Equal(1, RunWrite(new[] { "scaffold", "event", "Foo", dir }, out string refused));
            Assert.Contains("refusing", refused, StringComparison.Ordinal);
            Assert.Equal(csBefore, File.ReadAllText(Path.Combine(dir, "Foo.cs")));

            File.WriteAllText(Path.Combine(dir, "Bar.cs"), "x");          // only .cs exists
            Assert.Equal(1, RunWrite(new[] { "scaffold", "event", "Bar", dir }, out _));

            File.WriteAllText(Path.Combine(dir, "Baz.expect"), "x");      // only .expect exists
            Assert.Equal(1, RunWrite(new[] { "scaffold", "event", "Baz", dir }, out _));
        });
    }

    [Fact] // T8 — --namespace is honored; a dangling --namespace (no value) falls back to the default, no crash
    public void Run_NamespaceFlag()
    {
        RunInTempDir(dir =>
        {
            Assert.Equal(0, RunWrite(new[] { "scaffold", "event", "Foo", dir, "--namespace", "My.Ns" }, out _));
            Assert.Contains("namespace My.Ns;", File.ReadAllText(Path.Combine(dir, "Foo.cs")), StringComparison.Ordinal);

            Assert.Equal(0, RunWrite(new[] { "scaffold", "event", "Baz", dir, "--namespace" }, out _));
            Assert.Contains("namespace MonoRpgMaker.Engine.Sim.Tracer;", File.ReadAllText(Path.Combine(dir, "Baz.cs")), StringComparison.Ordinal);
        });
    }

    [Fact] // T9 — the router dispatches scaffold + check-expectations, and rejects the unknown/empty with usage
    public void MonorpgCli_RoutesVerbs()
    {
        RunInTempDir(dir =>
            Assert.Equal(0, MonorpgCli.Run(new[] { "scaffold", "event", "Routed", dir }, new StringWriter())));

        Assert.Equal(2, MonorpgCli.Run(CheckMissingDir, new StringWriter()));   // routed to OracleCli (missing dir → 2)

        var unknownWriter = new StringWriter();
        Assert.Equal(2, MonorpgCli.Run(UnknownVerb, unknownWriter));
        Assert.Contains("usage", unknownWriter.ToString(), StringComparison.Ordinal);

        Assert.Equal(2, MonorpgCli.Run(Array.Empty<string>(), new StringWriter()));
    }

    private static int RunWrite(string[] args, out string output)
    {
        var writer = new StringWriter();
        int code = ScaffoldCli.Run(args, writer);
        output = writer.ToString();
        return code;
    }

    private static void RunInTempDir(Action<string> body)
    {
        string dir = Directory.CreateTempSubdirectory("mrm-scaffold-").FullName;
        try
        {
            body(dir);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
