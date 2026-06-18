using System;
using MonoRpgMaker.Abstractions;

namespace MonoRpgMaker.Engine.Sim;

/// <summary>
/// The Engine-side implementation of the read-only <see cref="IEventContext"/> seam: the switch +
/// counter reads a map event branches on, delegated to a live <see cref="GameState"/>. Events read
/// through this seam and <em>return</em> their effects as <see cref="Outcome"/>s; an
/// <see cref="OutcomeApplier"/> applies them. The context never mutates state.
/// </summary>
public sealed class EventContext : IEventContext
{
    private readonly GameState _state;

    /// <summary>Create a read context over the live <paramref name="state"/>.</summary>
    public EventContext(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _state = state;
    }

    /// <inheritdoc />
    public bool GetSwitch(string key) => _state.Get(key);

    /// <inheritdoc />
    public int GetCounter(string key) => _state.GetCount(key);
}
