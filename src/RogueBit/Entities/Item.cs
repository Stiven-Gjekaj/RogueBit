using System;
using SadRogue.Primitives;

namespace RogueBit.Entities;

public enum ItemType { Heart, Coin }

public class Item : Entity
{
    public ItemType Type { get; set; }
    private Item(Point pos, ItemType type) : base(pos)
    {
        Type = type;
    }

    public static Item Heart(Point pos)
    {
        var ascii = Environment.GetEnvironmentVariable("USE_ASCII");
        char glyph = (ascii == "1" || ascii?.Equals("true", StringComparison.OrdinalIgnoreCase) == true) ? 'h' : '♥';
        return new Item(pos, ItemType.Heart)
        {
            Glyph = glyph,
            Color = Color.Red,
            Blocks = false,
        };
    }

    public static Item Coin(Point pos)
    {
        return new Item(pos, ItemType.Coin)
        {
            Glyph = '$',
            Color = Color.Yellow,
            Blocks = false,
        };
    }
}

