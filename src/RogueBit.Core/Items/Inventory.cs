namespace RogueBit.Core.Items;

using RogueBit.Core.Entities;

/// <summary>
/// What the player is carrying, and what is in the two equipment slots.
///
/// The slots hold the item itself rather than a copy of its numbers, so taking
/// a sword off cannot leave its bonus behind. <see cref="TotalPower"/> and
/// <see cref="TotalDefence"/> read straight through to whatever is worn.
/// </summary>
public sealed class Inventory
{
    private readonly List<Item> items = [];

    public int Capacity { get; }

    public Inventory(int capacity = 16)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        Capacity = capacity;
    }

    public IReadOnlyList<Item> Items => items;

    public Item? Weapon { get; private set; }

    public Item? Armour { get; private set; }

    public bool IsFull => items.Count >= Capacity;

    /// <summary>Takes an item, unless there is no room left.</summary>
    public bool TryAdd(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (IsFull) return false;

        Insert(item);
        return true;
    }

    /// <summary>
    /// Where a kind sits in the pack. Potions come first, because they are
    /// what somebody reaches for in a hurry.
    /// </summary>
    private static int Rank(ItemKind kind) => kind switch
    {
        ItemKind.Potion => 0,
        ItemKind.Weapon => 1,
        _ => 2,
    };

    /// <summary>
    /// Puts an item into its group, behind everything already in that group.
    ///
    /// The pack is kept in order rather than sorted when it is read. Reading it
    /// happens every frame the pack is open, and the letter a player presses
    /// for a potion has to be the same letter each time it is opened, which is
    /// a property of the pack rather than of whoever draws it.
    /// </summary>
    private void Insert(Item item)
    {
        int rank = Rank(item.Kind);
        int at = items.Count;

        for (int i = 0; i < items.Count; i++)
        {
            if (Rank(items[i].Kind) <= rank) continue;

            at = i;
            break;
        }

        items.Insert(at, item);
    }

    public bool Remove(Item item) => items.Remove(item);

    /// <summary>
    /// Puts an item into its slot. Whatever was in that slot goes back into the
    /// carried items, so nothing is ever destroyed by equipping over it.
    /// </summary>
    public bool TryEquip(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!item.IsEquipment) return false;
        if (!items.Contains(item)) return false;

        Item? displaced = item.Kind == ItemKind.Weapon ? Weapon : Armour;

        items.Remove(item);

        if (item.Kind == ItemKind.Weapon) Weapon = item;
        else Armour = item;

        if (displaced is not null) Insert(displaced);

        return true;
    }

    /// <summary>Takes an item out of its slot and carries it instead.</summary>
    public bool TryUnequip(ItemKind slot)
    {
        Item? worn = slot switch
        {
            ItemKind.Weapon => Weapon,
            ItemKind.Armour => Armour,
            _ => null,
        };

        if (worn is null || IsFull) return false;

        if (slot == ItemKind.Weapon) Weapon = null;
        else Armour = null;

        Insert(worn);
        return true;
    }

    /// <summary>The bonus the wielded weapon adds.</summary>
    public int TotalPower => Weapon?.Bonus ?? 0;

    /// <summary>The bonus the worn armour adds.</summary>
    public int TotalDefence => Armour?.Bonus ?? 0;

    public void Clear()
    {
        items.Clear();
        Weapon = null;
        Armour = null;
    }
}
