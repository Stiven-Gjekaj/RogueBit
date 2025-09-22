using RogueBit.Entities;

namespace RogueBit.Systems;

public class CombatSystem
{
    private readonly Game game;
    public CombatSystem(Game game) { this.game = game; }

    public void ResolveAttack(Entity attacker, Entity defender)
    {
        int dmg = 1;
        defender.HP -= dmg;
        if (defender.HP < 0) defender.HP = 0;
    }
}

