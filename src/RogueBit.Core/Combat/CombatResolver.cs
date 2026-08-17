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

    /// <summary>
    /// Describes an attack in the words the log shows.
    ///
    /// The verb agrees with the attacker, because the player is addressed in
    /// the second person and everything else in the third. The kill clause names
    /// the target with a pronoun, so the sentence does not say the same noun
    /// twice.
    /// </summary>
    public static string Describe(string attacker, string defender, AttackResult result, bool attackerIsPlayer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attacker);
        ArgumentException.ThrowIfNullOrWhiteSpace(defender);

        string hits = attackerIsPlayer ? "hit" : "hits";

        if (!result.Hit) return Capitalise($"{attacker} {hits} {defender}, and the blow is turned aside");

        string sentence = $"{attacker} {hits} {defender} for {result.Damage}";

        if (!result.Killed) return Capitalise(sentence);

        string kills = attackerIsPlayer ? "kill" : "kills";
        string target = defender.Equals("you", StringComparison.OrdinalIgnoreCase) ? "you" : "it";

        return Capitalise($"{sentence}, and {kills} {target}");
    }

    /// <summary>Raises the first letter, so a monster name can open a sentence.</summary>
    private static string Capitalise(string sentence) =>
        sentence.Length == 0 ? sentence : char.ToUpperInvariant(sentence[0]) + sentence[1..];
}
