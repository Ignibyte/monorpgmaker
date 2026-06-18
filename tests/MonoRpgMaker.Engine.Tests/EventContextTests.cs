using System.Collections.Generic;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Engine.Sim;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

public class EventContextTests
{
    [Fact] // EC1 — REQ-003 (verbs delegate to the backing GameState + sink, write-through)
    public void Verbs_DelegateToBackingState_AndSink()
    {
        var state = new GameState();
        var messages = new List<string>();
        IEventContext ctx = new EventContext(state, messages.Add);

        Assert.False(ctx.GetSwitch("door"));
        ctx.SetSwitch("door", true);
        Assert.True(ctx.GetSwitch("door"));
        Assert.True(state.Get("door"));               // write-through (kills the SetSwitch no-op mutant)

        Assert.Equal(0, ctx.GetCounter("potions"));
        ctx.AddCounter("potions", 3);
        Assert.Equal(3, ctx.GetCounter("potions"));
        Assert.Equal(3, state.GetCount("potions"));   // write-through (kills the AddCounter no-op mutant)

        ctx.ShowMessage("hello");
        Assert.Equal("hello", Assert.Single(messages));
    }
}
