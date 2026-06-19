using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MonoRpgMaker.Abstractions;

namespace MonoRpgMaker.Editor.Expectations;

/// <summary>
/// One parsed <c>.expect</c> row: the seed game-state (switches + counters) and the ordered
/// <see cref="Outcome"/> sequence the handler is expected to return for it.
/// </summary>
public sealed class ExpectationRow
{
    /// <summary>Create a row anchored at its 1-based source <paramref name="line"/>.</summary>
    public ExpectationRow(
        int line,
        IReadOnlyDictionary<string, bool> switches,
        IReadOnlyDictionary<string, int> counters,
        IReadOnlyList<Outcome> expected)
    {
        Line = line;
        Switches = switches;
        Counters = counters;
        Expected = expected;
    }

    /// <summary>The 1-based line in the source file this row came from.</summary>
    public int Line { get; }

    /// <summary>The switches to seed before running (key → value).</summary>
    public IReadOnlyDictionary<string, bool> Switches { get; }

    /// <summary>The counters to seed before running (key → amount).</summary>
    public IReadOnlyDictionary<string, int> Counters { get; }

    /// <summary>The outcomes the handler should return, in order.</summary>
    public IReadOnlyList<Outcome> Expected { get; }
}

/// <summary>A parse failure for a <c>.expect</c> file: the 1-based <see cref="Line"/> and why.</summary>
public sealed class ParseError
{
    /// <summary>Create a parse error at <paramref name="line"/> with <paramref name="message"/>.</summary>
    public ParseError(int line, string message)
    {
        Line = line;
        Message = message;
    }

    /// <summary>The 1-based line the error was found on.</summary>
    public int Line { get; }

    /// <summary>A human-readable reason.</summary>
    public string Message { get; }
}

/// <summary>The result of parsing a <c>.expect</c> file: either rows or a single <see cref="ParseError"/>.</summary>
public sealed class ParseResult
{
    private ParseResult(IReadOnlyList<ExpectationRow>? rows, ParseError? error)
    {
        Rows = rows;
        Error = error;
    }

    /// <summary>The parsed rows when <see cref="Ok"/>; otherwise <see langword="null"/>.</summary>
    public IReadOnlyList<ExpectationRow>? Rows { get; }

    /// <summary>The failure when not <see cref="Ok"/>; otherwise <see langword="null"/>.</summary>
    public ParseError? Error { get; }

    /// <summary>Whether the parse succeeded.</summary>
    public bool Ok => Error is null;

    /// <summary>A successful parse carrying <paramref name="rows"/>.</summary>
    public static ParseResult Success(IReadOnlyList<ExpectationRow> rows) => new(rows, null);

    /// <summary>A failed parse carrying <paramref name="error"/>.</summary>
    public static ParseResult Failure(ParseError error) => new(null, error);
}

/// <summary>A row whose handler output differed from its expected outcomes — an <c>MRM0006</c>.</summary>
public sealed class Mismatch
{
    /// <summary>Create a mismatch for <paramref name="module"/>'s <paramref name="row"/>.</summary>
    public Mismatch(string module, ExpectationRow row, IReadOnlyList<Outcome> actual)
    {
        Module = module;
        Row = row;
        Actual = actual;
    }

    /// <summary>The module (the <c>.expect</c> file name, sans extension).</summary>
    public string Module { get; }

    /// <summary>The offending row.</summary>
    public ExpectationRow Row { get; }

    /// <summary>What the handler actually returned.</summary>
    public IReadOnlyList<Outcome> Actual { get; }
}

/// <summary>
/// Formats outcomes (and a row's seed state) back to the <c>.expect</c> grammar — used for the
/// "OK" echo and the <c>MRM0006</c> expected-vs-actual report. Pure + culture-invariant.
/// </summary>
public static class OutcomeFormat
{
    /// <summary>Format an ordered outcome list as <c>a; b; c</c>, or <c>(none)</c> when empty.</summary>
    public static string Format(IReadOnlyList<Outcome> outcomes)
    {
        if (outcomes.Count == 0)
        {
            return "(none)";
        }

        var sb = new StringBuilder();
        for (int i = 0; i < outcomes.Count; i++)
        {
            if (i > 0)
            {
                sb.Append("; ");
            }

            sb.Append(Format(outcomes[i]));
        }

        return sb.ToString();
    }

    /// <summary>Format a single outcome in the grammar's surface syntax.</summary>
    public static string Format(Outcome outcome) => outcome switch
    {
        SetSwitch s => "SetSwitch(" + s.Key + ", " + Bool(s.Value) + ")",
        AddCounter a => "AddCounter(" + a.Key + ", " + a.Amount.ToString(CultureInfo.InvariantCulture) + ")",
        ShowMessage m => "ShowMessage(\"" + m.Text + "\")",
        _ => outcome.ToString() ?? string.Empty,
    };

    /// <summary>Format a row's seed state as <c>k=v …</c>, or <c>(initial)</c> when empty.</summary>
    public static string FormatInput(ExpectationRow row)
    {
        var parts = new List<string>();
        foreach (KeyValuePair<string, bool> s in row.Switches)
        {
            parts.Add(s.Key + "=" + Bool(s.Value));
        }

        foreach (KeyValuePair<string, int> c in row.Counters)
        {
            parts.Add(c.Key + "=" + c.Value.ToString(CultureInfo.InvariantCulture));
        }

        return parts.Count == 0 ? "(initial)" : string.Join(" ", parts);
    }

    private static string Bool(bool value) => value ? "true" : "false";
}
