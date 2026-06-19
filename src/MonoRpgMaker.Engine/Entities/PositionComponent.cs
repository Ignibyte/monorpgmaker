using MonoRpgMaker.Abstractions;

namespace MonoRpgMaker.Engine.Entities;

/// <summary>An entity's location on the map grid.</summary>
public sealed record PositionComponent(GridPoint Cell) : IComponent;
