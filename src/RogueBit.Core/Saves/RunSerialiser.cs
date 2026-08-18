namespace RogueBit.Core.Saves;

using RogueBit.Core.Entities;
using RogueBit.Core.Items;
using RogueBit.Core.Map;

/// <summary>Turns a run into something writable, and back again.</summary>
public static class RunSerialiser
{
    private const char Wall = '#';
    private const char Ground = '.';
    private const char Down = '>';
    private const char Up = '<';
    private const char Seen = '1';
    private const char Unseen = '0';

    public static SaveData Capture(Run run)
    {
        ArgumentNullException.ThrowIfNull(run);

        (int seed, ulong state, ulong increment) = run.Random.Snapshot();

        return new SaveData
        {
            Seed = seed,
            Depth = run.Depth,
            DeepestDepth = run.DeepestDepth,
            Turns = run.Turns,
            RandomState = state,
            RandomIncrement = increment,
            Player = new SavedPlayer(
                run.Player.Position.X,
                run.Player.Position.Y,
                run.Player.Health,
                run.Player.MaxHealth,
                run.Player.Coins),
            Floors = [.. run.Floors.Select(Capture)],
            Carried = [.. run.Inventory.Items.Select(Capture)],
            Weapon = run.Inventory.Weapon is { } weapon ? Capture(weapon) : null,
            Armour = run.Inventory.Armour is { } armour ? Capture(armour) : null,
            Log = [.. run.Log.Messages.Select(m => new SavedMessage(m.Text, m.Kind.ToString(), m.Count))],
        };
    }

    private static SavedFloor Capture(Floor floor) => new()
    {
        Depth = floor.Depth,
        Width = floor.Map.Width,
        Height = floor.Map.Height,
        Tiles = WriteTiles(floor.Map),
        Explored = WriteExplored(floor.Map),
        EntranceX = floor.Map.Entrance.X,
        EntranceY = floor.Map.Entrance.Y,
        StairsX = floor.Map.StairsDown.X,
        StairsY = floor.Map.StairsDown.Y,
        Monsters = [.. floor.Monsters.Where(m => m.IsAlive).Select(Capture)],
        Items = [.. floor.Items.Select(Capture)],
    };

    public static Run Restore(SaveData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        Player player = new(new Position(data.Player.X, data.Player.Y));
        player.TakeCoins(data.Player.Coins);
        player.TakeDamage(player.MaxHealth - Math.Clamp(data.Player.Health, 0, player.MaxHealth));

        foreach (SavedItem item in data.Carried) player.Inventory.TryAdd(Restore(item));

        if (data.Weapon is { } savedWeapon)
        {
            Item weapon = Restore(savedWeapon);
            player.Inventory.TryAdd(weapon);
            player.Inventory.TryEquip(weapon);
        }

        if (data.Armour is { } savedArmour)
        {
            Item armour = Restore(savedArmour);
            player.Inventory.TryAdd(armour);
            player.Inventory.TryEquip(armour);
        }

        return Run.Resume(
            random: SeededRandom.Restore(data.Seed, data.RandomState, data.RandomIncrement),
            player: player,
            floors: [.. data.Floors.Select(Restore)],
            depth: data.Depth,
            deepestDepth: data.DeepestDepth,
            turns: data.Turns,
            log: data.Log.Select(m => (m.Text, Enum.Parse<MessageKind>(m.Kind), m.Count)));
    }

    private static Floor Restore(SavedFloor saved)
    {
        DungeonMap map = new(saved.Width, saved.Height);

        for (int y = 0; y < saved.Height; y++)
        {
            for (int x = 0; x < saved.Width; x++)
            {
                int index = (y * saved.Width) + x;
                Position cell = new(x, y);

                map[cell] = saved.Tiles[index] switch
                {
                    Ground => TileKind.Floor,
                    Down => TileKind.StairsDown,
                    Up => TileKind.StairsUp,
                    _ => TileKind.Wall,
                };

                if (saved.Explored[index] == Seen) map.MarkVisible(cell);
            }
        }

        map.StairsDown = new Position(saved.StairsX, saved.StairsY);
        map.Entrance = new Position(saved.EntranceX, saved.EntranceY);

        // Everything comes back unlit. The run computes what the player can see
        // from where it is standing as soon as it is put back together, and a
        // floor it is not standing on is not lit by anything.
        map.ClearVisible();

        Floor floor = new() { Depth = saved.Depth, Map = map };
        floor.Monsters.AddRange(saved.Monsters.Select(Restore));
        floor.Items.AddRange(saved.Items.Select(Restore));
        return floor;
    }

    private static SavedMonster Capture(Monster monster) => new(
        monster.Position.X,
        monster.Position.Y,
        monster.Health,
        monster.MaxHealth,
        monster.BasePower,
        monster.BaseDefence,
        monster.Name,
        monster.Glyph,
        monster.Behaviour.ToString(),
        monster.AggroRadius,
        monster.Range,
        monster.CoinReward);

    private static Monster Restore(SavedMonster saved)
    {
        Monster monster = new(
            new Position(saved.X, saved.Y),
            saved.MaxHealth,
            saved.Power,
            saved.Defence)
        {
            Glyph = saved.Glyph,
            Name = saved.Name,
            Behaviour = Enum.Parse<MonsterBehaviour>(saved.Behaviour),
            AggroRadius = saved.AggroRadius,
            Range = saved.Range,
            CoinReward = saved.CoinReward,
        };

        monster.TakeDamage(saved.MaxHealth - Math.Clamp(saved.Health, 0, saved.MaxHealth));
        return monster;
    }

    private static SavedItem Capture(Item item) => new(
        item.Position.X,
        item.Position.Y,
        item.Kind.ToString(),
        item.Name,
        item.Glyph,
        item.Value,
        item.Healing,
        item.Bonus);

    private static Item Restore(SavedItem saved)
    {
        Position position = new(saved.X, saved.Y);

        return Enum.Parse<ItemKind>(saved.Kind) switch
        {
            ItemKind.Coin => Item.Coin(position, saved.Value),
            ItemKind.Potion => Item.Potion(position, saved.Healing),
            ItemKind.Weapon => Item.Weapon(position, saved.Name, saved.Bonus),
            _ => Item.Armour(position, saved.Name, saved.Bonus),
        };
    }

    private static string WriteTiles(DungeonMap map)
    {
        char[] cells = new char[map.Width * map.Height];

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                cells[(y * map.Width) + x] = map[new Position(x, y)] switch
                {
                    TileKind.Floor => Ground,
                    TileKind.StairsDown => Down,
                    TileKind.StairsUp => Up,
                    _ => Wall,
                };
            }
        }

        return new string(cells);
    }

    private static string WriteExplored(DungeonMap map)
    {
        char[] cells = new char[map.Width * map.Height];

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                cells[(y * map.Width) + x] = map.IsExplored(new Position(x, y)) ? Seen : Unseen;
            }
        }

        return new string(cells);
    }
}
