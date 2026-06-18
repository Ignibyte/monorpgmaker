namespace MonoRpgMaker.Abstractions;

/// <summary>
/// The semantic-verb surface a map event may touch when it runs: read/write game
/// switches and counters, and show a message. Events call verbs — they never poke a
/// concrete state object, the tiles, or rendering directly. This is the published seam
/// an authored event's <see cref="IMapEvent.Run"/> programs against.
/// </summary>
public interface IEventContext
{
    /// <summary>The value of switch <paramref name="key"/>, or false if never set.</summary>
    bool GetSwitch(string key);

    /// <summary>Set switch <paramref name="key"/> to <paramref name="value"/>.</summary>
    void SetSwitch(string key, bool value);

    /// <summary>The value of counter <paramref name="key"/>, or zero if never added to.</summary>
    int GetCounter(string key);

    /// <summary>Add <paramref name="amount"/> to counter <paramref name="key"/>.</summary>
    void AddCounter(string key, int amount);

    /// <summary>Display <paramref name="text"/> to the player.</summary>
    void ShowMessage(string text);
}
