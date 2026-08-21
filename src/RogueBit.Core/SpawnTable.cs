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

    /// <summary>
    /// How many traps are hidden on a floor. This rises with depth like
    /// everything else here, so walking into ground you have not seen costs
    /// more the further down you are.
    /// </summary>
    public static int TrapCount(int depth) => 2 + (depth / 2);

    /// <summary>
    /// What one trap takes off whoever steps on it. A trap is an ambush and
    /// not a fight, so this stays below what a monster of the same depth hits
    /// for.
    /// </summary>
    public static int TrapDamage(int depth) => 2 + (depth / 2);

    /// <summary>True when this floor holds a boss.</summary>
    public static bool HasBoss(int depth) => depth % 5 == 0 || GameRules.IsFinalDepth(depth);

    /// <summary>Builds one monster suited to the depth.</summary>
    public static Monster CreateMonster(Position position, int depth, SeededRandom random)
    {
        ArgumentNullException.ThrowIfNull(random);

        // Tougher kinds unlock with depth, so floor one is only ever goblins.
        //
        // One roll, read against thresholds that overlap. The order of these
        // lines is therefore part of the numbers: a band only gets what the
        // lines above it did not take, and moving one changes every kind below
        // it. What each is worth was counted rather than worked out, and the
        // counts are in the tests.
        int roll = random.Next(100);

        if (depth >= 4 && roll < 18) return Archer(position, depth);
        if (depth >= 2 && roll < 36) return Jackal(position, depth);
        if (depth >= 2 && roll < 54) return Scavenger(position, depth);

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

    public static Monster Scavenger(Position position, int depth) =>
        new(position, maxHealth: 4 + depth, power: 2 + (depth / 2), defence: 0)
        {
            Glyph = 's',
            Name = "a scavenger",
            Behaviour = MonsterBehaviour.Scavenger,
            AggroRadius = 9,

            // Worth more than a goblin. It comes at you while it is whole and
            // runs the moment it is not, so finishing one is work.
            CoinReward = 4,
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

    public static Monster Boss(Position position, int depth)
    {
        bool last = GameRules.IsFinalDepth(depth);

        return new Monster(
            position,
            maxHealth: (25 + (depth * 4)) * (last ? 2 : 1),
            power: 5 + depth,
            defence: 1 + (depth / 5))
        {
            Glyph = 'B',

            // The one at the bottom is named apart, so a player meeting it
            // knows the run is nearly over rather than merely hard.
            Name = last ? "the deep warden" : "the warden",
            Behaviour = MonsterBehaviour.Boss,
            AggroRadius = 14,
            CoinReward = (25 + (depth * 5)) * (last ? 2 : 1),
        };
    }

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
