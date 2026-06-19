using System;
using System.Collections.Generic;
using System.Linq;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Engine.Sim;

namespace MonoRpgMaker.Editor.Expectations;

/// <summary>
/// Runs parsed <see cref="ExpectationRow"/>s against the real handler and reports the rows whose
/// returned outcomes differ from the expected ones. Pure: each row seeds a fresh
/// <see cref="GameState"/>, runs the handler over a read-only <see cref="EventContext"/>, and
/// value-equal compares the returned <see cref="Outcome"/>s (kind + payload + order, via the DU's
/// record equality) — the same comparison the engine's behaviour rests on.
/// </summary>
public static class ExpectationRunner
{
    /// <summary>
    /// Check every row of <paramref name="module"/> against a fresh handler from
    /// <paramref name="handlerFactory"/>; return one <see cref="Mismatch"/> per non-matching row.
    /// </summary>
    public static IReadOnlyList<Mismatch> Check(
        string module, IReadOnlyList<ExpectationRow> rows, Func<IMapEvent> handlerFactory)
    {
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(handlerFactory);

        var mismatches = new List<Mismatch>();
        foreach (ExpectationRow row in rows)
        {
            var state = new GameState();
            foreach (KeyValuePair<string, bool> sw in row.Switches)
            {
                state.Set(sw.Key, sw.Value);
            }

            foreach (KeyValuePair<string, int> counter in row.Counters)
            {
                state.Add(counter.Key, counter.Value);
            }

            IReadOnlyList<Outcome> actual = handlerFactory().Run(new EventContext(state));
            if (!actual.SequenceEqual(row.Expected))
            {
                mismatches.Add(new Mismatch(module, row, actual));
            }
        }

        return mismatches;
    }
}
