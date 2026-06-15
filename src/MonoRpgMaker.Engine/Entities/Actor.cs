using System;
using Microsoft.Xna.Framework;

namespace MonoRpgMaker.Engine.Entities;

/// <summary>A stat-bearing character: the party, NPCs, enemies.</summary>
public sealed class Actor : Entity
{
    /// <summary>Create an actor at full health.</summary>
    public Actor(string name, Point cell, int maxHp)
        : base(name, cell)
    {
        MaxHp = maxHp;
        Hp = maxHp;
    }

    /// <summary>Maximum hit points.</summary>
    public int MaxHp { get; }

    /// <summary>Current hit points, clamped to <c>[0, MaxHp]</c>.</summary>
    public int Hp { get; private set; }

    /// <summary>True once HP has reached zero.</summary>
    public bool IsDefeated => Hp <= 0;

    /// <summary>Apply non-negative damage, flooring at zero HP.</summary>
    public void TakeDamage(int amount) => Hp = Math.Max(0, Hp - Math.Max(0, amount));

    /// <summary>Restore non-negative HP, capping at <see cref="MaxHp"/>.</summary>
    public void Heal(int amount) => Hp = Math.Min(MaxHp, Hp + Math.Max(0, amount));
}
