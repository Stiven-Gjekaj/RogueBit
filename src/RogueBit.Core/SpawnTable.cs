namespace RogueBit.Core;

using RogueBit.Core.Entities;
using RogueBit.Core.Items;

/// <summary>
/// Decides what lives on a floor, and how much of it.
///
/// Everything scales with depth: more monsters, harder ones, and fewer potions
/// for each of them. The numbers live here rather than in the run, so the
/// difficulty curve can be read and tested in one place.
/// </summary>
public static class SpawnTable
{
    /// <summary>How many monsters belong on a floor.</summary>
    public static int MonsterCount(int depth) => 6 + (depth * 2);

    /// <summary>How many coins are scattered on a floor.</summary>
    public static int CoinCount(int depth) => 12 + depth;

    /// <summary>
    /// How many potions are left lying about. This falls as the floors get
    /// harder, which is what turns depth into pressure rather than a number.
    /// </summary>
    public static int PotionCount(int depth) => Math.Max(1, 5 - (depth / 3));

    /// <summary>True when this floor holds a boss.</summary>
    public static bool HasBoss(int depth) => depth % 5 == 0;

    /// <summary>Builds one monster suited to the depth.</summary>
    public static Monster CreateMonster(Position position, int depth, SeededRandom random)
    {
        ArgumentNullException.ThrowIfNull(random);

        // Tougher kinds unlock with depth, so floor one is only ever goblins.
        int roll = random.Next(100);

        if (depth >= 4 && roll < 20) return Archer(position, depth);
        if (depth >= 2 && roll < 45) return Jackal(position, depth);

        return Goblin(position, depth);
    }

    public static Monster Goblin(Position position, int depth) =>
        new(position, maxHealth: 5 + depth, power: 3 + (depth / 2), defence: depth / 4)
        {
            Glyph = 'g',
            Name = "a goblin",
            Behaviour = MonsterBehaviour.Chaser,
            AggroRadius = 8,
            CoinReward = 2,
        };

    public static Monster Jackal(Position position, int depth) =>
        new(position, maxHealth: 3 + depth, power: 2 + (depth / 2), defence: 0)
        {
            Glyph = 'j',
            Name = "a jackal",
            Behaviour = MonsterBehaviour.Swift,
            AggroRadius = 11,
            CoinReward = 3,
        };

    public static Monster Archer(Position position, int depth) =>
        new(position, maxHealth: 4 + depth, power: 3 + (depth / 3), defence: 0)
        {
            Glyph = 'a',
            Name = "an archer",
            Behaviour = MonsterBehaviour.Archer,
            AggroRadius = 9,
            Range = 6,
            CoinReward = 4,
        };

    public static Monster Boss(Position position, int depth) =>
        new(position, maxHealth: 25 + (depth * 4), power: 5 + depth, defence: 1 + (depth / 5))
        {
            Glyph = 'B',
            Name = "the warden",
            Behaviour = MonsterBehaviour.Boss,
            AggroRadius = 14,
            CoinReward = 25 + (depth * 5),
        };

    /// <summary>Builds one piece of equipment suited to the depth, or nothing.</summary>
    public static Item? CreateEquipment(Position position, int depth, SeededRandom random)
    {
        ArgumentNullException.ThrowIfNull(random);

        if (!random.Chance(0.5)) return null;

        int bonus = 1 + (depth / 2);

        return random.Chance(0.5)
            ? Item.Weapon(position, WeaponName(bonus), bonus)
            : Item.Armour(position, ArmourName(bonus), bonus);
    }

    private static string WeaponName(int bonus) => bonus switch
    {
        <= 1 => "a rusty knife",
        2 => "a short sword",
        3 => "a war axe",
        _ => "a rune blade",
    };

    private static string ArmourName(int bonus) => bonus switch
    {
        <= 1 => "a leather jerkin",
        2 => "a chain shirt",
        3 => "a scale hauberk",
        _ => "a rune plate",
    };
}
