using SadRogue.Primitives;

namespace RogueBit.Entities;

public class Entity
{
    public Point Pos { get; set; }
    public char Glyph { get; set; }
    public Color Color { get; set; } = Color.White;
    public bool Blocks { get; set; }
    public int HP { get; set; }
    public int MaxHP { get; set; }

    public Entity(Point pos)
    {
        Pos = pos;
    }
}

