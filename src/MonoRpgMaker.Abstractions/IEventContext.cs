namespace MonoRpgMaker.Abstractions;

/// <summary>
/// The read-only state surface a map event may consult when it runs: the game switches and counters
/// it branches on. Events <em>read</em> through this seam and <em>return</em> their effects as
/// <see cref="Outcome"/>s — they never mutate state, the tiles, or rendering directly (D-0017). This
/// is the published seam an authored event's <see cref="IMapEvent.Run"/> programs against.
/// </summary>
public interface IEventContext
{
    /// <summary>The value of switch <paramref name="key"/>, or false if never set.</summary>
    bool GetSwitch(string key);

    /// <summary>The value of counter <paramref name="key"/>, or zero if never added to.</summary>
    int GetCounter(string key);
}
