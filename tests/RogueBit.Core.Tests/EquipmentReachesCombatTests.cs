using RogueBit.Core;
using RogueBit.Core.Combat;
using RogueBit.Core.Entities;
using RogueBit.Core.Items;
using Xunit;

namespace RogueBit.Core.Tests;

/// <summary>
/// Equipment used to be held beside the player rather than on it, so combat
/// asked the actor for its defence and never saw the armour. These tests pin
/// the bonus to the number combat actually reads.
/// </summary>
public class EquipmentReachesCombatTests
{
    private static readonly Position Origin = new(0, 0);

    [Fact]
    public void WornArmourRaisesTheDefenceCombatReads()
    {
        Player player = new(Origin);
        int bare = player.Defence;

        Item mail = Item.Armour(Origin, "chain mail", 3);
        player.Inventory.TryAdd(mail);
        player.Inventory.TryEquip(mail);

        Assert.Equal(bare + 3, player.Defence);
    }

    [Fact]
    public void WieldedWeaponRaisesThePowerCombatReads()
    {
        Player player = new(Origin);
        int bare = player.Power;

        Item sword = Item.Weapon(Origin, "a short sword", 4);
        player.Inventory.TryAdd(sword);
        player.Inventory.TryEquip(sword);

        Assert.Equal(bare + 4, player.Power);
    }

    [Fact]
    public void ArmourActuallyReducesTheDamageAnAttackDeals()
    {
        Player bare = new(Origin);
        Player armoured = new(Origin);

        Item mail = Item.Armour(Origin, "chain mail", 3);
        armoured.Inventory.TryAdd(mail);
        armoured.Inventory.TryEquip(mail);

        AttackResult onBare = CombatResolver.Resolve(power: 10, bare);
        AttackResult onArmoured = CombatResolver.Resolve(power: 10, armoured);

        Assert.Equal(onBare.Damage - 3, onArmoured.Damage);
    }

    [Fact]
    public void TakingTheArmourOffGivesTheDefenceBack()
    {
        Player player = new(Origin);
        int bare = player.Defence;

        Item mail = Item.Armour(Origin, "chain mail", 3);
        player.Inventory.TryAdd(mail);
        player.Inventory.TryEquip(mail);
        player.Inventory.TryUnequip(ItemKind.Armour);

        Assert.Equal(bare, player.Defence);
    }

    [Fact]
    public void CarryingEquipmentWithoutWearingItDoesNothing()
    {
        Player player = new(Origin);
        int bare = player.Defence;

        player.Inventory.TryAdd(Item.Armour(Origin, "chain mail", 3));

        Assert.Equal(bare, player.Defence);
    }
}
