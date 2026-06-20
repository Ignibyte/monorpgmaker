using System;
using System.Collections.Generic;
using MonoRpgMaker.Abstractions;

namespace MonoRpgMaker.Engine.Sim;

/// <summary>
/// Applies the declarative <see cref="Outcome"/>s a map event returns to the live
/// <see cref="GameState"/> and a message sink — the Engine-side interpreter of the return-then-apply
/// model (D-0017). Events return effects; this is the only place those effects touch state.
/// </summary>
public sealed class OutcomeApplier
{
    private readonly GameState _state;
    private readonly Action<string> _showMessage;
    private readonly Action<Warp> _requestWarp;

    /// <summary>Create an applier over the live <paramref name="state"/>, a message sink, and a warp-request sink (the sim-host performs the map switch — D-0017).</summary>
    public OutcomeApplier(GameState state, Action<string> showMessage, Action<Warp> requestWarp)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(showMessage);
        ArgumentNullException.ThrowIfNull(requestWarp);

        _state = state;
        _showMessage = showMessage;
        _requestWarp = requestWarp;
    }

    /// <summary>Apply each outcome in <paramref name="outcomes"/>, in order.</summary>
    public void Apply(IReadOnlyList<Outcome> outcomes)
    {
        ArgumentNullException.ThrowIfNull(outcomes);

        foreach (Outcome outcome in outcomes)
        {
            switch (outcome)
            {
                case SetSwitch setSwitch:
                    _state.Set(setSwitch.Key, setSwitch.Value);
                    break;
                case AddCounter addCounter:
                    _state.Add(addCounter.Key, addCounter.Amount);
                    break;
                case ShowMessage showMessage:
                    _showMessage(showMessage.Text);
                    break;
                case Warp warp:
                    _requestWarp(warp);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(outcomes), outcome, "Unknown outcome kind.");
            }
        }
    }
}
