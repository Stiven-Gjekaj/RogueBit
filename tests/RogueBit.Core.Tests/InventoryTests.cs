using RogueBit.Core;
using RogueBit.Core.Items;
using Xunit;

namespace RogueBit.Core.Tests;

public class InventoryTests
{
    private static readonly Position Origin = new(0, 0);

    [Fact]
    public void KeepsThePackGroupedByKindWithPotionsFirst()
    {
        Inventory pack = new();

        // Picked up in the least helpful order there is.
        Item mail = Item.Armour(Origin, "chain mail", 2);
        Item sword = Item.Weapon(Origin, "a short sword", 3);
        Item potion = Item.Potion(Origin);

        pack.TryAdd(mail);
        pack.TryAdd(sword);
        pack.TryAdd(potion);

        Assert.Equal([potion, sword, mail], pack.Items);
    }

    [Fact]
    public void KeepsThingsOfOneKindInTheOrderTheyWerePickedUp()
    {
        Inventory pack = new();

        Item first = Item.Potion(Origin, 3);
        Item second = Item.Potion(Origin, 5);
        Item third = Item.Potion(Origin, 7);

        pack.TryAdd(first);
        pack.TryAdd(second);
        pack.TryAdd(third);

        Assert.Equal([first, second, third], pack.Items);
    }

    [Fact]
    public void TheLetterForAPotionDoesNotMoveWhenSomethingElseIsPickedUp()
    {
        // This is the whole point. A potion at 'a' that becomes 'b' because a
        // sword was picked up is how somebody drinks a sword.
        Inventory pack = new();
        Item potion = Item.Potion(Origin);

        pack.TryAdd(potion);
        Assert.Same(potion, pack.Items[0]);

        pack.TryAdd(Item.Weapon(Origin, "a war axe", 3));
        pack.TryAdd(Item.Armour(Origin, "a scale hauberk", 3));

        Assert.Same(potion, pack.Items[0]);
    }

    [Fact]
    public void PuttingSomethingOnAndTakingItOffDoesNotShuffleThePack()
    {
        Inventory pack = new();

        Item potion = Item.Potion(Origin);
        Item sword = Item.Weapon(Origin, "a short sword", 3);
        Item mail = Item.Armour(Origin, "chain mail", 2);

        pack.TryAdd(potion);
        pack.TryAdd(sword);
        pack.TryAdd(mail);

        Item[] before = [.. pack.Items];

        pack.TryEquip(sword);
        pack.TryUnequip(ItemKind.Weapon);

        Assert.Equal(before, pack.Items);
    }

    [Fact]
    public void ADisplacedWeaponGoesBackWithTheWeapons()
    {
        Inventory pack = new();

        Item knife = Item.Weapon(Origin, "a rusty knife", 1);
        Item sword = Item.Weapon(Origin, "a short sword", 3);
        Item mail = Item.Armour(Origin, "chain mail", 2);

        pack.TryAdd(knife);
        pack.TryAdd(sword);
        pack.TryAdd(mail);
        pack.TryEquip(knife);

        // The sword is worn and the knife comes back. It belongs with the
        // weapons, ahead of the armour, not on the end of the list.
        pack.TryEquip(sword);

        Assert.Equal([knife, mail], pack.Items);
    }

    [Fact]
    public void TakesAnItemUpToItsCapacity()
    {
        Inventory inventory = new(capacity: 2);

        Assert.True(inventory.TryAdd(Item.Potion(Origin)));
        Assert.True(inventory.TryAdd(Item.Potion(Origin)));
        Assert.False(inventory.TryAdd(Item.Potion(Origin)));
        Assert.Equal(2, inventory.Items.Count);
    }

    [Fact]
    public void EquippingMovesTheItemOutOfTheCarriedList()
    {
        Inventory inventory = new();
        Item sword = Item.Weapon(Origin, "a short sword", 3);
        inventory.TryAdd(sword);

        Assert.True(inventory.TryEquip(sword));

        Assert.Same(sword, inventory.Weapon);
        Assert.DoesNotContain(sword, inventory.Items);
    }

    [Fact]
    public void EquippingOverSomethingPutsTheOldOneBackRatherThanLosingIt()
    {
        Inventory inventory = new();
        Item old = Item.Weapon(Origin, "a rusty knife", 1);
        Item better = Item.Weapon(Origin, "a short sword", 3);
        inventory.TryAdd(old);
        inventory.TryAdd(better);
        inventory.TryEquip(old);

        inventory.TryEquip(better);

        Assert.Same(better, inventory.Weapon);
        Assert.Contains(old, inventory.Items);
    }

    [Fact]
    public void AWeaponAndArmourUseDifferentSlots()
    {
        Inventory inventory = new();
        Item sword = Item.Weapon(Origin, "a short sword", 3);
        Item mail = Item.Armour(Origin, "chain mail", 2);
        inventory.TryAdd(sword);
        inventory.TryAdd(mail);

        inventory.TryEquip(sword);
        inventory.TryEquip(mail);

        Assert.Same(sword, inventory.Weapon);
        Assert.Same(mail, inventory.Armour);
    }

    [Fact]
    public void BonusesReadThroughToWhatIsWorn()
    {
        Inventory inventory = new();
        Item sword = Item.Weapon(Origin, "a short sword", 3);
        Item mail = Item.Armour(Origin, "chain mail", 2);
        inventory.TryAdd(sword);
        inventory.TryAdd(mail);
        inventory.TryEquip(sword);
        inventory.TryEquip(mail);

        Assert.Equal(3, inventory.TotalPower);
        Assert.Equal(2, inventory.TotalDefence);

        inventory.TryUnequip(ItemKind.Weapon);

        Assert.Equal(0, inventory.TotalPower);
        Assert.Equal(2, inventory.TotalDefence);
    }

    [Fact]
    public void APotionCannotBeEquipped()
    {
        Inventory inventory = new();
        Item potion = Item.Potion(Origin);
        inventory.TryAdd(potion);

        Assert.False(inventory.TryEquip(potion));
        Assert.Null(inventory.Weapon);
    }

    [Fact]
    public void CannotEquipSomethingItIsNotCarrying()
    {
        Inventory inventory = new();

        Assert.False(inventory.TryEquip(Item.Weapon(Origin, "a sword", 3)));
    }

    [Fact]
    public void CannotTakeSomethingOffWithNoRoomToCarryIt()
    {
        Inventory inventory = new(capacity: 1);
        Item sword = Item.Weapon(Origin, "a sword", 3);
        inventory.TryAdd(sword);
        inventory.TryEquip(sword);
        inventory.TryAdd(Item.Potion(Origin));

        Assert.True(inventory.IsFull);
        Assert.False(inventory.TryUnequip(ItemKind.Weapon));
        Assert.Same(sword, inventory.Weapon);
    }

    [Fact]
    public void UnequippingAnEmptySlotDoesNothing()
    {
        Inventory inventory = new();

        Assert.False(inventory.TryUnequip(ItemKind.Weapon));
    }

    [Fact]
    public void CoinsAreTakenByWalkingOverThemAndOtherThingsAreNot()
    {
        Assert.True(Item.Coin(Origin).IsPickedUpByWalkingOver);
        Assert.False(Item.Potion(Origin).IsPickedUpByWalkingOver);
        Assert.False(Item.Weapon(Origin, "a sword", 1).IsPickedUpByWalkingOver);
    }
}
