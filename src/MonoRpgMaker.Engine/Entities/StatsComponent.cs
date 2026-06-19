namespace MonoRpgMaker.Engine.Entities;

/// <summary>An entity's current and maximum hit points.</summary>
public sealed record StatsComponent(int CurrentHp, int MaxHp) : IComponent;
