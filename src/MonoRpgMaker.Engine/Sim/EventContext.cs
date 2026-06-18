using System;
using MonoRpgMaker.Abstractions;

namespace MonoRpgMaker.Engine.Sim;

/// <summary>
/// The Engine-side implementation of the <see cref="IEventContext"/> seam: the verbs a
/// map event may use, delegated to a live <see cref="GameState"/> and a message sink.
/// Events speak verbs through the interface; they never touch tiles or rendering directly.
/// </summary>
public sealed class EventContext : IEventContext
{
    private readonly GameState _state;
    private readonly Action<string> _showMessage;

    /// <summary>Create a context over the live <paramref name="state"/> and a message sink.</summary>
    public EventContext(GameState state, Action<string> showMessage)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(showMessage);

        _state = state;
        _showMessage = showMessage;
    }

    /// <inheritdoc />
    public bool GetSwitch(string key) => _state.Get(key);

    /// <inheritdoc />
    public void SetSwitch(string key, bool value) => _state.Set(key, value);

    /// <inheritdoc />
    public int GetCounter(string key) => _state.GetCount(key);

    /// <inheritdoc />
    public void AddCounter(string key, int amount) => _state.Add(key, amount);

    /// <inheritdoc />
    public void ShowMessage(string text) => _showMessage(text);
}
