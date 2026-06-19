using System.Collections.Generic;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Editor.Expectations;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

public class ExpectationParserTests
{
    [Fact] // T1 — a two-row file parses to the exact rows (input + ordered outcomes + line numbers)
    public void Parse_TwoRows_ProducesExactRows()
    {
        ParseResult result = ExpectationParser.Parse(
            "=> ShowMessage(\"hi\"); SetSwitch(door_open, true)\ndoor_open=true => (none)");

        Assert.True(result.Ok);
        Assert.Equal(2, result.Rows!.Count);

        ExpectationRow first = result.Rows[0];
        Assert.Equal(1, first.Line);
        Assert.Empty(first.Switches);
        Assert.Empty(first.Counters);
        Assert.Equal(new Outcome[] { new ShowMessage("hi"), new SetSwitch("door_open", true) }, first.Expected);

        ExpectationRow second = result.Rows[1];
        Assert.Equal(2, second.Line);
        Assert.True(second.Switches["door_open"]);
        Assert.Empty(second.Expected);
    }

    [Fact] // T2 — '#' comments and blank lines are ignored; the surviving row keeps its true line number
    public void Parse_IgnoresCommentsAndBlanks_KeepsLineNumbers()
    {
        ParseResult result = ExpectationParser.Parse("# header\n\n=> (none)\n");

        Assert.True(result.Ok);
        ExpectationRow row = Assert.Single(result.Rows!);
        Assert.Equal(3, row.Line);
        Assert.Empty(row.Expected);
    }

    [Fact] // T3 — key=true/false is a switch; key=<int> is a counter
    public void Parse_DisambiguatesSwitchesFromCounters()
    {
        ParseResult result = ExpectationParser.Parse("flag=false potions=5 => (none)");

        ExpectationRow row = Assert.Single(result.Rows!);
        Assert.False(Assert.Contains("flag", row.Switches));
        Assert.Equal(5, Assert.Contains("potions", row.Counters));
        Assert.Single(row.Switches);
        Assert.Single(row.Counters);
    }

    [Fact] // T4 — (none) is an empty outcome list
    public void Parse_None_IsEmptyOutcomes()
    {
        ParseResult result = ExpectationParser.Parse("=> (none)");
        Assert.Empty(Assert.Single(result.Rows!).Expected);
    }

    [Fact] // T5 — each outcome kind round-trips its payload (incl. quoted text with comma + punctuation)
    public void Parse_EachOutcomeKind_RoundTripsPayload()
    {
        Assert.Equal(new Outcome[] { new SetSwitch("k", true) }, ParseExpected("=> SetSwitch(k, true)"));
        Assert.Equal(new Outcome[] { new SetSwitch("k", false) }, ParseExpected("=> SetSwitch(k, false)"));
        Assert.Equal(new Outcome[] { new AddCounter("potions", 3) }, ParseExpected("=> AddCounter(potions, 3)"));
        Assert.Equal(new Outcome[] { new AddCounter("debt", -2) }, ParseExpected("=> AddCounter(debt, -2)"));
        Assert.Equal(new Outcome[] { new ShowMessage("a, b. c!") }, ParseExpected("=> ShowMessage(\"a, b. c!\")"));
    }

    [Theory] // T6 — malformed input is a typed ParseError on the right line, NEVER an exception (§14)
    [InlineData("ShowMessage(\"hi\")")]                     // missing '=>'
    [InlineData("door_open=maybe => (none)")]               // input value neither bool nor int
    [InlineData("=> SetSwich(k, true)")]                    // misspelled outcome
    [InlineData("=> SetSwitch(a, b, true)")]                // too many args
    [InlineData("=> SetSwitch(door_open, true) trailing")]  // trailing junk after a valid outcome
    [InlineData("=> AddCounter(k, notanumber)")]            // counter amount not an int
    [InlineData("=> ShowMessage(\")")]                      // inspect regression: lone quote (no crash)
    [InlineData("=key => (none)")]                          // empty key
    public void Parse_Malformed_ReturnsTypedError_NeverThrows(string text)
    {
        ParseResult result = ExpectationParser.Parse(text);   // must not throw

        Assert.False(result.Ok);
        Assert.Null(result.Rows);
        Assert.Equal(1, result.Error!.Line);
        Assert.NotEmpty(result.Error.Message);
    }

    private static IReadOnlyList<Outcome> ParseExpected(string line)
    {
        ParseResult result = ExpectationParser.Parse(line);
        Assert.True(result.Ok);
        return Assert.Single(result.Rows!).Expected;
    }
}
