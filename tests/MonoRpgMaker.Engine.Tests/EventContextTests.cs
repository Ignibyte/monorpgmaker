using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Engine.Sim;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

public class EventContextTests
{
    [Fact] // EC1 — the read context delegates switch + counter reads to the backing GameState
    public void ReadVerbs_DelegateToBackingState()
    {
        var state = new GameState();
        IEventContext ctx = new EventContext(state);

        Assert.False(ctx.GetSwitch("door"));            // an unset switch reads false
        state.Set("door", true);
        Assert.True(ctx.GetSwitch("door"));             // reflects the backing write-through

        Assert.Equal(0, ctx.GetCounter("potions"));     // an unset counter reads zero
        state.Add("potions", 3);
        Assert.Equal(3, ctx.GetCounter("potions"));
    }
}
