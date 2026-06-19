using System;

namespace MonoRpgMaker.Abstractions;

/// <summary>
/// Marks a method as a <em>state mutator</em> — it changes mutable game state in place. The
/// outcome-return purity analyzer (<c>MRM1006</c>) forbids calling a marked method from an
/// <em>outcome-returning handler</em> (one whose return type is
/// <see cref="System.Collections.Generic.IReadOnlyList{T}"/> of <see cref="Outcome"/>): such handlers
/// must be pure over their read context and <em>return</em> their changes as <see cref="Outcome"/>s for
/// an applier to apply (D-0017). The canonical bearers are the <c>GameState</c> switch/counter writers (the
/// only methods marked in P0; further mutators are marked as more handler kinds land); an outcome applier
/// (which returns <see langword="void"/>) is never a handler, so it may call them freely.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class StateMutatorAttribute : Attribute
{
}
