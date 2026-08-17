namespace RogueBit.Core.Combat;

using RogueBit.Core.Entities;

/// <summary>What one attack did.</summary>
public readonly record struct AttackResult(int Damage, bool Killed)
{
    public bool Hit => Damage > 0;
}

/// <summary>Works out what one attack does to its target.</summary>
public static class CombatResolver
{
    /// <summary>
    /// Resolves an attack. Damage is the attacker's power less the defender's
    /// defence, and never less than nothing. A blow that is fully turned aside
    /// still counts as a turn taken.
    /// </summary>
    public static AttackResult Resolve(int power, Actor defender)
    {
        ArgumentNullException.ThrowIfNull(defender);

        int damage = Math.Max(0, power - defender.Defence);
        int landed = defender.TakeDamage(damage);

        return new AttackResult(landed, !defender.IsAlive);
    }

    /// <summary>Describes an attack in the words the log shows.</summary>
    public static string Describe(string attacker, string defender, AttackResult result)
    {
        if (!result.Hit) return $"{attacker} hits {defender}, and the blow is turned aside";

        return result.Killed
            ? $"{attacker} hits {defender} for {result.Damage}, and kills it"
            : $"{attacker} hits {defender} for {result.Damage}";
    }
}
