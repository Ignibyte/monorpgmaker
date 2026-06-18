using MonoRpgMaker.Abstractions;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

public class OutcomeTests
{
    [Fact] // T1 — SetSwitch carries its payload and is value-equal
    public void SetSwitch_CarriesKeyAndValue_AndIsValueEqual()
    {
        var outcome = new SetSwitch("door", true);

        Assert.Equal("door", outcome.Key);
        Assert.True(outcome.Value);
        Assert.Equal(new SetSwitch("door", true), outcome);     // value equality
        Assert.NotEqual(new SetSwitch("door", false), outcome); // Value distinguishes
        Assert.NotEqual(new SetSwitch("gate", true), outcome);  // Key distinguishes
    }

    [Fact] // T1 — AddCounter carries its payload and is value-equal
    public void AddCounter_CarriesKeyAndAmount_AndIsValueEqual()
    {
        var outcome = new AddCounter("potions", 3);

        Assert.Equal("potions", outcome.Key);
        Assert.Equal(3, outcome.Amount);
        Assert.Equal(new AddCounter("potions", 3), outcome);
        Assert.NotEqual(new AddCounter("potions", 1), outcome); // Amount distinguishes
        Assert.NotEqual(new AddCounter("elixirs", 3), outcome); // Key distinguishes
    }

    [Fact] // T1 — ShowMessage carries its payload and is value-equal
    public void ShowMessage_CarriesText_AndIsValueEqual()
    {
        var outcome = new ShowMessage("hello");

        Assert.Equal("hello", outcome.Text);
        Assert.Equal(new ShowMessage("hello"), outcome);
        Assert.NotEqual(new ShowMessage("goodbye"), outcome);
    }

    [Fact] // T1 — the three cases are distinct Outcome subtypes
    public void Outcome_Cases_AreDistinctSubtypes()
    {
        Assert.IsType<SetSwitch>((Outcome)new SetSwitch("k", true));
        Assert.IsType<AddCounter>((Outcome)new AddCounter("k", 1));
        Assert.IsType<ShowMessage>((Outcome)new ShowMessage("k"));
    }
}
