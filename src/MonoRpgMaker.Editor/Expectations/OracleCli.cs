using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MonoRpgMaker.Abstractions;

namespace MonoRpgMaker.Editor.Expectations;

/// <summary>
/// The <c>.expect</c> oracle command (gate #13). Runs every authored <c>&lt;module&gt;.expect</c>
/// table against the real handler and reports an <c>MRM0006</c> for any row the handler fails to
/// reproduce — the executable correctness contract (D-0017 / §7). Writes to an injected
/// <see cref="TextWriter"/> and returns a process exit code (0 = all reproduced) so it is testable
/// without capturing the console.
/// </summary>
public static class OracleCli
{
    /// <summary>Dispatch a maker-tool verb. Returns a process exit code (0 = ok, non-zero = failure).</summary>
    public static int Run(IReadOnlyList<string> args, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);

        if (args.Count >= 2 && args[0] == "check-expectations")
        {
            return CheckExpectations(args[1], output);
        }

        output.WriteLine("usage: check-expectations <dir>");
        return 2;
    }

    private static int CheckExpectations(string dir, TextWriter output)
    {
        if (!Directory.Exists(dir))
        {
            output.WriteLine("MRM0006: expectations directory not found: " + dir);
            return 2;
        }

        string[] files = Directory.GetFiles(dir, "*.expect");
        Array.Sort(files, StringComparer.Ordinal);

        int failures = 0;
        int rowsChecked = 0;
        foreach (string file in files)
        {
            string module = Path.GetFileNameWithoutExtension(file);
            if (!HandlerRegistry.TryGet(module, out Func<IMapEvent>? factory))
            {
                output.WriteLine("MRM0006: " + module + ".expect — no handler registered for module '" + module + "'");
                failures++;
                continue;
            }

            ParseResult parsed = ExpectationParser.Parse(File.ReadAllText(file));
            if (!parsed.Ok)
            {
                output.WriteLine(Prefix(module, parsed.Error!.Line) + " — parse error: " + parsed.Error.Message);
                failures++;
                continue;
            }

            if (parsed.Rows!.Count == 0)
            {
                output.WriteLine("MRM0006: " + module + ".expect — no expectation rows (a registered module must be pinned by at least one row)");
                failures++;
                continue;
            }

            rowsChecked += parsed.Rows!.Count;
            foreach (Mismatch mismatch in ExpectationRunner.Check(module, parsed.Rows!, factory))
            {
                output.WriteLine(Prefix(module, mismatch.Row.Line) + " — expectation mismatch");
                output.WriteLine("  given:    " + OutcomeFormat.FormatInput(mismatch.Row));
                output.WriteLine("  expected: " + OutcomeFormat.Format(mismatch.Row.Expected));
                output.WriteLine("  actual:   " + OutcomeFormat.Format(mismatch.Actual));
                failures++;
            }
        }

        if (failures == 0)
        {
            output.WriteLine(
                ".expect oracle: " + Count(rowsChecked, "row") + " across " + Count(files.Length, "module") + " reproduced — OK");
        }

        return failures == 0 ? 0 : 1;
    }

    private static string Prefix(string module, int line) =>
        "MRM0006: " + module + ".expect:" + line.ToString(CultureInfo.InvariantCulture);

    private static string Count(int n, string noun) =>
        n.ToString(CultureInfo.InvariantCulture) + " " + noun + (n == 1 ? string.Empty : "s");
}
