using System;
using System.Collections.Generic;

namespace MonoRpgMaker.Editor;

/// <summary>
/// One reversible edit: <see cref="Apply"/> (re-)performs it for redo, <see cref="Revert"/> undoes it. The pair
/// captures the minimal forward/backward state. The originating edit is performed by the caller; the command only
/// records how to replay or reverse it (see <see cref="EditHistory.Record"/>).
/// </summary>
/// <param name="Apply">Re-performs the edit (used by redo).</param>
/// <param name="Revert">Reverses the edit (used by undo).</param>
internal sealed record EditCommand(Action Apply, Action Revert);

/// <summary>
/// An undo/redo stack of reversible <see cref="EditCommand"/>s. The caller performs an edit and then
/// <see cref="Record"/>s its inverse; recording clears the redo stack (a fresh edit invalidates the redo future).
/// <see cref="Undo"/>/<see cref="Redo"/> move commands between the two stacks. <see cref="Changed"/> fires on
/// every mutation so a host can refresh its UI (button enablement, repaint).
/// </summary>
internal sealed class EditHistory
{
    private readonly Stack<EditCommand> _undo = new();
    private readonly Stack<EditCommand> _redo = new();

    /// <summary>Raised after any change to the history (record / undo / redo / clear).</summary>
    public event EventHandler? Changed;

    /// <summary>Whether there is an edit to undo.</summary>
    public bool CanUndo => _undo.Count > 0;

    /// <summary>Whether there is an undone edit to redo.</summary>
    public bool CanRedo => _redo.Count > 0;

    /// <summary>
    /// Record an already-performed edit (its <paramref name="command"/> carries the replay/reverse pair) and clear
    /// the redo stack. The command is NOT applied here — the caller already performed the edit.
    /// </summary>
    public void Record(EditCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        _undo.Push(command);
        _redo.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Undo the most recent edit; returns <see langword="false"/> when there is nothing to undo.</summary>
    public bool Undo()
    {
        if (_undo.Count == 0)
        {
            return false;
        }

        EditCommand command = _undo.Pop();
        command.Revert();
        _redo.Push(command);
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Redo the most recently undone edit; returns <see langword="false"/> when there is nothing to redo.</summary>
    public bool Redo()
    {
        if (_redo.Count == 0)
        {
            return false;
        }

        EditCommand command = _redo.Pop();
        command.Apply();
        _undo.Push(command);
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Discard all history (e.g. on New / Load — you cannot undo across a document reset).</summary>
    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
