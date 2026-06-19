using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MonoRpgMaker.Engine.Entities;

/// <summary>
/// A stateful entity — a stable id, a reference to its <see cref="EntityDefinition"/>, and a set of
/// composed <see cref="IComponent"/>s. Everything in the game (player, NPC, item, token) is one of these:
/// capabilities are attached as components rather than baked into a class hierarchy. Immutable — mutating
/// operations (<see cref="With{T}"/>) return a new instance, so simulation stays deterministically replayable.
/// </summary>
public sealed class EntityInstance
{
    // A ReadOnlyCollection (not a bare IReadOnlyList over a List) so a caller cannot cast Components back to a
    // mutable List and corrupt entity state — immutability must hold for deterministic replay.
    private readonly ReadOnlyCollection<IComponent> _components;

    private EntityInstance(int id, int definitionId, ReadOnlyCollection<IComponent> components)
    {
        Id = id;
        DefinitionId = definitionId;
        _components = components;
    }

    /// <summary>This instance's stable id.</summary>
    public int Id { get; }

    /// <summary>The id of the <see cref="EntityDefinition"/> this instance was spawned from.</summary>
    public int DefinitionId { get; }

    /// <summary>The attached components, in deterministic insertion order.</summary>
    public IReadOnlyList<IComponent> Components => _components;

    /// <summary>Spawn an instance of <paramref name="definition"/> carrying <paramref name="components"/>.</summary>
    public static EntityInstance Spawn(int id, EntityDefinition definition, IEnumerable<IComponent> components)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(components);
        return new EntityInstance(id, definition.Id, new List<IComponent>(components).AsReadOnly());
    }

    /// <summary>The first attached component of type <typeparamref name="T"/>, or <see langword="null"/>.</summary>
    public T? Get<T>()
        where T : class, IComponent
    {
        foreach (IComponent component in _components)
        {
            if (component is T match)
            {
                return match;
            }
        }

        return null;
    }

    /// <summary>Whether a component of type <typeparamref name="T"/> is attached.</summary>
    public bool Has<T>()
        where T : class, IComponent => Get<T>() is not null;

    /// <summary>
    /// Return a NEW instance with <paramref name="component"/> set — replacing any existing component of type
    /// <typeparamref name="T"/> (else appending it). The original instance is unchanged (copy-on-write).
    /// </summary>
    public EntityInstance With<T>(T component)
        where T : class, IComponent
    {
        ArgumentNullException.ThrowIfNull(component);

        var next = new List<IComponent>(_components.Count + 1);
        foreach (IComponent existing in _components)
        {
            if (existing is not T)
            {
                next.Add(existing);
            }
        }

        next.Add(component);
        return new EntityInstance(Id, DefinitionId, next.AsReadOnly());
    }
}
