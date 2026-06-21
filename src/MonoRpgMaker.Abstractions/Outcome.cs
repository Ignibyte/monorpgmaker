namespace MonoRpgMaker.Abstractions;

/// <summary>
/// A declarative effect a map event <em>returns</em> to describe a change to game state, rather than
/// mutating state directly (D-0017): events are pure over a read context and return outcomes, so the
/// applier is the only place an effect touches state. The case set is <b>closed</b> to the authored
/// Project layer — events construct and return the cases below but cannot invent new ones — and is
/// extended additively here (P2's battle/effect spine returns the same vocabulary: one registry, not two).
/// </summary>
public abstract record Outcome
{
    // Non-public: only this assembly defines outcome cases. Authored events return the public cases
    // below; new kinds are added here additively (never by the Project layer).
    private protected Outcome()
    {
    }
}

/// <summary>Set game switch <paramref name="Key"/> to <paramref name="Value"/>.</summary>
public sealed record SetSwitch(string Key, bool Value) : Outcome;

/// <summary>Add <paramref name="Amount"/> to game counter <paramref name="Key"/>.</summary>
public sealed record AddCounter(string Key, int Amount) : Outcome;

/// <summary>Show <paramref name="Text"/> to the player.</summary>
public sealed record ShowMessage(string Text) : Outcome;

/// <summary>Transition the player to map <paramref name="MapId"/> at target <paramref name="Cell"/>. The sim-host applies the switch (the behaviour only returns this — D-0017).</summary>
public sealed record Warp(string MapId, GridPoint Cell) : Outcome;

/// <summary>Open a buy/sell shop modal carrying the encoded <paramref name="Offers"/> (<c>id:buy:sell,…</c>). The one host-modal outcome beyond <see cref="ShowMessage"/> — the sim-host renders the screen (the behaviour only returns this — D-0017).</summary>
public sealed record OpenShop(string Offers) : Outcome;
