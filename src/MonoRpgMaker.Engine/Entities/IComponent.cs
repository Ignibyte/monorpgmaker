namespace MonoRpgMaker.Engine.Entities;

/// <summary>
/// An attachable, immutable capability / state aspect of an <see cref="EntityInstance"/> — composition over
/// inheritance, so any entity can gain any capability without a class hierarchy. v1 lives in the Engine;
/// promote to <c>MonoRpgMaker.Abstractions</c> when components become agent-authored.
/// </summary>
public interface IComponent
{
}
