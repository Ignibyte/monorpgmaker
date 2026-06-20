using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MonoRpgMaker.Abstractions;

namespace MonoRpgMaker.Editor.Expectations;

/// <summary>
/// Parses a <c>.expect</c> file into <see cref="ExpectationRow"/>s. Pure (no IO) and total — a
/// malformed file yields a typed <see cref="ParseError"/>, never an exception (§14). Grammar, one
/// row per non-blank/non-comment line:
/// <code>&lt;input&gt; =&gt; &lt;outcomes&gt;</code>
/// where <c>input</c> is space-separated <c>key=value</c> (<c>true</c>/<c>false</c> → a switch, an
/// integer → a counter), and <c>outcomes</c> is semicolon-separated
/// <c>SetSwitch(key, true|false)</c> / <c>AddCounter(key, int)</c> / <c>ShowMessage("text")</c>, or
/// the literal <c>(none)</c>. Lines beginning <c>#</c> and blank lines are ignored.
/// </summary>
public static class ExpectationParser
{
    /// <summary>Parse <paramref name="text"/> into rows, or a <see cref="ParseError"/>.</summary>
    public static ParseResult Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var rows = new List<ExpectationRow>();
        string[] lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            int lineNo = i + 1;
            string line = lines[i].Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            int arrow = line.IndexOf("=>", StringComparison.Ordinal);
            if (arrow < 0)
            {
                return ParseResult.Failure(new ParseError(lineNo, "missing '=>' separator"));
            }

            ParseError? error = ParseRow(line, arrow, lineNo, out ExpectationRow? row);
            if (error is not null)
            {
                return ParseResult.Failure(error);
            }

            rows.Add(row!);
        }

        return ParseResult.Success(rows);
    }

    private static ParseError? ParseRow(string line, int arrow, int lineNo, out ExpectationRow? row)
    {
        row = null;
        string inputPart = line[..arrow].Trim();
        string outputPart = line[(arrow + 2)..].Trim();

        var switches = new Dictionary<string, bool>(StringComparer.Ordinal);
        var counters = new Dictionary<string, int>(StringComparer.Ordinal);
        if (inputPart.Length > 0)
        {
            foreach (string token in inputPart.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = token.IndexOf('=', StringComparison.Ordinal);
                if (eq <= 0)
                {
                    return new ParseError(lineNo, "malformed input token '" + token + "'");
                }

                string key = token[..eq];
                string val = token[(eq + 1)..];
                if (val == "true")
                {
                    switches[key] = true;
                }
                else if (val == "false")
                {
                    switches[key] = false;
                }
                else if (int.TryParse(val, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int n))
                {
                    counters[key] = n;
                }
                else
                {
                    return new ParseError(lineNo, "input value must be true/false or an integer: '" + token + "'");
                }
            }
        }

        var outcomes = new List<Outcome>();
        if (outputPart != "(none)")
        {
            foreach (string part in SplitOutcomes(outputPart))
            {
                Outcome? outcome = ParseOutcome(part.Trim());
                if (outcome is null)
                {
                    return new ParseError(lineNo, "malformed outcome '" + part.Trim() + "'");
                }

                outcomes.Add(outcome);
            }
        }

        row = new ExpectationRow(lineNo, switches, counters, outcomes);
        return null;
    }

    // Split on ';' that is not inside a "double-quoted" message.
    private static List<string> SplitOutcomes(string text)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;
        foreach (char c in text)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }

            if (c == ';' && !inQuotes)
            {
                parts.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        parts.Add(current.ToString());
        return parts;
    }

    private static Outcome? ParseOutcome(string text)
    {
        if (text.StartsWith("ShowMessage(\"", StringComparison.Ordinal) && text.EndsWith("\")", StringComparison.Ordinal))
        {
            int start = "ShowMessage(\"".Length;
            int length = text.Length - start - "\")".Length;
            // A lone quote (e.g. ShowMessage(") ) satisfies both the prefix and the suffix and would
            // yield a negative length; a malformed token must fall through to a typed error, never throw (§14).
            return length >= 0 ? new ShowMessage(text.Substring(start, length)) : null;
        }

        if (TryArgs(text, "SetSwitch(", out string[] swArgs) && swArgs.Length == 2)
        {
            if (swArgs[1] == "true")
            {
                return new SetSwitch(swArgs[0], true);
            }

            if (swArgs[1] == "false")
            {
                return new SetSwitch(swArgs[0], false);
            }

            return null;
        }

        if (TryArgs(text, "AddCounter(", out string[] ctrArgs) &&
            ctrArgs.Length == 2 &&
            int.TryParse(ctrArgs[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int amount))
        {
            return new AddCounter(ctrArgs[0], amount);
        }

        // Warp("map", x, y) — a quoted map id + an integer target cell (flat 3-arg; a tuple's inner comma would
        // break the naive comma split).
        if (TryArgs(text, "Warp(", out string[] warpArgs) &&
            warpArgs.Length == 3 &&
            warpArgs[0].Length >= 2 && warpArgs[0][0] == '"' && warpArgs[0][^1] == '"' &&
            int.TryParse(warpArgs[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int wx) &&
            int.TryParse(warpArgs[2], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int wy))
        {
            return new Warp(warpArgs[0][1..^1], new GridPoint(wx, wy));
        }

        return null;
    }

    private static bool TryArgs(string text, string prefix, out string[] args)
    {
        if (!text.StartsWith(prefix, StringComparison.Ordinal) || !text.EndsWith(')'))
        {
            args = Array.Empty<string>();
            return false;
        }

        int start = prefix.Length;
        int length = text.Length - start - 1;
        string inner = text.Substring(start, length);
        args = inner.Split(',');
        for (int i = 0; i < args.Length; i++)
        {
            args[i] = args[i].Trim();
        }

        return true;
    }
}
