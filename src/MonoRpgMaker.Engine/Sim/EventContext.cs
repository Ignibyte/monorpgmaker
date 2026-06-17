using System;

namespace MonoRpgMaker.Engine.Sim;

/// <summary>
/// The minimal surface a map event may touch when it runs: read/write game state
/// and show a message. This is the embryonic semantic-verb surface the future
/// EventContext seam grows from — events never poke tiles or rendering directly.
/// </summary>
public sealed class EventContext
{
    private readonly Action<string> _showMessage;

    /// <summary>Create a context over the live <paramref name="state"/> and a message sink.</summary>
    public EventContext(GameState state, Action<string> showMessage)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(showMessage);

        State = state;
        _showMessage = showMessage;
    }

    /// <summary>The live switch store.</summary>
    public GameState State { get; }

    /// <summary>Display <paramref name="text"/> to the player.</summary>
    public void ShowMessage(string text) => _showMessage(text);
}
