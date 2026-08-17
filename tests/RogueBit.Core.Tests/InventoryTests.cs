using RogueBit.Core;
using RogueBit.Core.Items;
using Xunit;

namespace RogueBit.Core.Tests;

public class InventoryTests
{
    private static readonly Position Origin = new(0, 0);

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
