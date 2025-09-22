using SadRogue.Primitives;

namespace RogueBit.Entities;

public class Player : Entity
{
    public int Coins { get; set; }

    public Player(Point pos) : base(pos)
    {
    }
}

