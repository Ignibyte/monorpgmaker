using System;
using System.Collections.Generic;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Engine.Data;
using MonoRpgMaker.Engine.Entities;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

public class EntityInstanceTests
{
    private static readonly EntityDefinition Slime = new(7, "Slime");

    private static EntityInstance SpawnStatsOnly(int currentHp = 10) =>
        EntityInstance.Spawn(1, Slime, new IComponent[] { new StatsComponent(currentHp, 10) });

    [Fact] // T1 (REQ-001) — definition is an immutable id-keyed IRecord
    public void EntityDefinition_IsAnIdKeyedRecord()
    {
        Assert.Equal(7, Slime.Id);
        Assert.Equal("Slime", Slime.Name);
        Assert.IsAssignableFrom<IRecord>(Slime);
    }

    [Fact] // T2 (REQ-002/003) — spawn references the definition + carries the components
    public void Spawn_ReferencesDefinition_AndCarriesComponents()
    {
        EntityInstance instance = EntityInstance.Spawn(
            1, Slime, new IComponent[] { new StatsComponent(10, 10), new PositionComponent(new GridPoint(3, 4)) });

        Assert.Equal(1, instance.Id);
        Assert.Equal(7, instance.DefinitionId);
        Assert.Equal(2, instance.Components.Count);
    }

    [Fact] // T3 (REQ-004) — Get returns the component / null; Has matches
    public void Get_And_Has_FindComponentsByType()
    {
        EntityInstance instance = SpawnStatsOnly();

        Assert.Equal(10, instance.Get<StatsComponent>()!.CurrentHp);
        Assert.Null(instance.Get<PositionComponent>());
        Assert.True(instance.Has<StatsComponent>());
        Assert.False(instance.Has<PositionComponent>());
    }

    [Fact] // T4 (REQ-005) — With is copy-on-write: the original is unchanged
    public void With_IsCopyOnWrite_OriginalUnchanged()
    {
        EntityInstance a = SpawnStatsOnly(currentHp: 10);

        EntityInstance b = a.With(new StatsComponent(5, 10));

        Assert.Equal(5, b.Get<StatsComponent>()!.CurrentHp);
        Assert.Equal(10, a.Get<StatsComponent>()!.CurrentHp);
    }

    [Fact] // T5a (REQ-005) — With on an existing type REPLACES (one of that type, new value)
    public void With_ExistingType_Replaces()
    {
        EntityInstance a = SpawnStatsOnly(currentHp: 10);

        EntityInstance b = a.With(new StatsComponent(5, 10));

        Assert.Single(b.Components);
        Assert.Equal(5, b.Get<StatsComponent>()!.CurrentHp);
    }

    [Fact] // T5b (REQ-005) — With on an absent type APPENDS, KEEPING the existing component
    public void With_AbsentType_Appends_KeepingExisting()
    {
        EntityInstance a = SpawnStatsOnly(currentHp: 10);

        EntityInstance b = a.With(new PositionComponent(new GridPoint(1, 1)));

        Assert.Equal(2, b.Components.Count);
        Assert.NotNull(b.Get<StatsComponent>());
        Assert.NotNull(b.Get<PositionComponent>());
    }

    [Fact] // T6 (REQ-005) — two instances of one definition have independent state
    public void TwoInstances_OfOneDefinition_AreIndependent()
    {
        EntityInstance a = EntityInstance.Spawn(1, Slime, new IComponent[] { new StatsComponent(10, 10) });
        EntityInstance b = EntityInstance.Spawn(2, Slime, new IComponent[] { new StatsComponent(10, 10) });

        a.With(new StatsComponent(1, 10));   // returns a new instance; b is untouched

        Assert.Equal(10, b.Get<StatsComponent>()!.CurrentHp);
    }

    [Fact] // T7 (REQ-004) — components iterate in deterministic insertion order; a replace keeps order
    public void Components_AreInDeterministicInsertionOrder()
    {
        EntityInstance instance = EntityInstance.Spawn(
            1, Slime, new IComponent[] { new StatsComponent(10, 10), new PositionComponent(new GridPoint(3, 4)) });

        Assert.IsType<StatsComponent>(instance.Components[0]);
        Assert.IsType<PositionComponent>(instance.Components[1]);

        EntityInstance replaced = instance.With(new StatsComponent(5, 10));
        Assert.IsType<PositionComponent>(replaced.Components[0]);   // the kept one stays, the new appends last
        Assert.IsType<StatsComponent>(replaced.Components[1]);
    }

    [Fact] // T8 (REQ-002) — null guards throw
    public void NullArguments_Throw()
    {
        EntityInstance a = SpawnStatsOnly();

        Assert.Throws<ArgumentNullException>(() => EntityInstance.Spawn(1, null!, new IComponent[] { new StatsComponent(1, 1) }));
        Assert.Throws<ArgumentNullException>(() => EntityInstance.Spawn(1, Slime, null!));
        Assert.Throws<ArgumentNullException>(() => a.With<StatsComponent>(null!));
    }

    [Fact] // T-IMMUTABLE (inspect fix) — Components cannot be cast back to a mutable List
    public void Components_CannotBeCastToMutableList()
    {
        EntityInstance a = SpawnStatsOnly();

        Assert.False(a.Components is List<IComponent>);
        Assert.Throws<InvalidCastException>(() => (List<IComponent>)a.Components);
    }
}
