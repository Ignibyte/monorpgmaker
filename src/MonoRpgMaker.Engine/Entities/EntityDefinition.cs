using MonoRpgMaker.Engine.Data;

namespace MonoRpgMaker.Engine.Entities;

/// <summary>
/// An immutable, id-keyed entity template — the flyweight <em>definition</em>. A stateful
/// <see cref="EntityInstance"/> references it by id; the editor authors definitions and the running game
/// spawns instances from them.
/// </summary>
public sealed record EntityDefinition(int Id, string Name) : IRecord;
