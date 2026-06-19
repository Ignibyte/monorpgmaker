using System;
using System.IO;
using MonoRpgMaker.Editor.Expectations;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

public class OracleCliTests
{
    private static readonly string[] VerbOnly = { "check-expectations" };
    private static readonly string[] MissingDirArgs = { "check-expectations", "/no/such/dir/mrm-oracle-missing" };

    [Fact] // every row reproduces → exit 0 + the OK summary (2 rows = plural, 1 module = singular)
    public void Run_AllReproduce_ExitZero_PluralizedSummary()
    {
        int code = RunOnTempDir(
            dir => Write(dir, "LeverEvent",
                "=> ShowMessage(\"You pull the lever. The door grinds open.\"); SetSwitch(door_open, true)\ndoor_open=true => (none)"),
            out string output);

        Assert.Equal(0, code);
        Assert.Contains("2 rows across 1 module reproduced — OK", output, StringComparison.Ordinal);
    }

    [Fact] // a wrong row → exit 1 + MRM0006 naming module:line and the expected-vs-actual outcomes
    public void Run_Mismatch_ExitOne_NamesModuleLineExpectedActual()
    {
        int code = RunOnTempDir(
            dir => Write(dir, "LeverEvent", "=> (none)"),
            out string output);

        Assert.Equal(1, code);
        Assert.Contains("MRM0006: LeverEvent.expect:1 — expectation mismatch", output, StringComparison.Ordinal);
        Assert.Contains("expected: (none)", output, StringComparison.Ordinal);
        Assert.Contains(
            "actual:   ShowMessage(\"You pull the lever. The door grinds open.\"); SetSwitch(door_open, true)",
            output,
            StringComparison.Ordinal);
    }

    [Fact] // a malformed file → exit 1 + a parse error
    public void Run_ParseError_ExitOne()
    {
        int code = RunOnTempDir(dir => Write(dir, "LeverEvent", "not a valid line"), out string output);

        Assert.Equal(1, code);
        Assert.Contains("parse error", output, StringComparison.Ordinal);
    }

    [Fact] // a .expect for a module with no registered handler → exit 1
    public void Run_UnknownModule_ExitOne()
    {
        int code = RunOnTempDir(dir => Write(dir, "Mystery", "=> (none)"), out string output);

        Assert.Equal(1, code);
        Assert.Contains("no handler registered", output, StringComparison.Ordinal);
    }

    [Fact] // a registered module whose table has no rows (comments only) → exit 1 (inspect fix)
    public void Run_EmptyTable_ExitOne()
    {
        int code = RunOnTempDir(dir => Write(dir, "LeverEvent", "# only a comment\n"), out string output);

        Assert.Equal(1, code);
        Assert.Contains("no expectation rows", output, StringComparison.Ordinal);
    }

    [Fact] // a missing directory → exit 2
    public void Run_MissingDirectory_ExitTwo()
    {
        var writer = new StringWriter();
        Assert.Equal(2, OracleCli.Run(MissingDirArgs, writer));
    }

    [Fact] // no verb → exit 2 + usage
    public void Run_NoVerb_ExitTwo()
    {
        var writer = new StringWriter();
        Assert.Equal(2, OracleCli.Run(Array.Empty<string>(), writer));
        Assert.Contains("usage", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact] // the verb with no directory argument → exit 2
    public void Run_CheckExpectations_NoDir_ExitTwo()
    {
        var writer = new StringWriter();
        Assert.Equal(2, OracleCli.Run(VerbOnly, writer));
    }

    [Fact] // REQ-004 — the REAL authored tables reproduce against the real handlers
    public void RealAuthoredTables_Reproduce()
    {
        string dir = Path.Combine(FindRepoRoot(), "src", "MonoRpgMaker.Engine", "Sim", "Tracer", "Expectations");
        var writer = new StringWriter();

        int code = OracleCli.Run(new[] { "check-expectations", dir }, writer);

        Assert.Equal(0, code);
        Assert.Contains("reproduced — OK", writer.ToString(), StringComparison.Ordinal);
    }

    private static void Write(string dir, string module, string content) =>
        File.WriteAllText(Path.Combine(dir, module + ".expect"), content);

    private static int RunOnTempDir(Action<string> setup, out string output)
    {
        string dir = Directory.CreateTempSubdirectory("mrm-oracle-").FullName;
        try
        {
            setup(dir);
            var writer = new StringWriter();
            int code = OracleCli.Run(new[] { "check-expectations", dir }, writer);
            output = writer.ToString();
            return code;
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "MonoRpgMaker.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
