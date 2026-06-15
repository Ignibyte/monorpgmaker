using System.Collections.Generic;

namespace MonoRpgMaker.Engine.Data;

/// <summary>A game-data record with a stable integer id.</summary>
public interface IRecord
{
    /// <summary>Unique id within its table.</summary>
    int Id { get; }
}

/// <summary>An RPG Maker style id-keyed table of game-data records.</summary>
public sealed class Database<T>
    where T : IRecord
{
    private readonly Dictionary<int, T> _byId = new();

    /// <summary>Insert or replace a record by its id.</summary>
    public void Add(T record) => _byId[record.Id] = record;

    /// <summary>Look up a record, returning false when absent.</summary>
    public bool TryGet(int id, out T record) => _byId.TryGetValue(id, out record!);

    /// <summary>Fetch a record by id; throws when absent.</summary>
    public T Get(int id) => _byId[id];

    /// <summary>Number of records in the table.</summary>
    public int Count => _byId.Count;

    /// <summary>All records, in no particular order.</summary>
    public IReadOnlyCollection<T> All => _byId.Values;
}

/// <summary>A purchasable / usable item definition.</summary>
public readonly record struct ItemRecord(int Id, string Name, int Price) : IRecord;

/// <summary>A playable/templated actor definition.</summary>
public readonly record struct ActorRecord(int Id, string Name, int MaxHp) : IRecord;
