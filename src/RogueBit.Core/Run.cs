namespace RogueBit.Core;

using RogueBit.Core.Combat;
using RogueBit.Core.Entities;
using RogueBit.Core.Items;
using RogueBit.Core.Map;
using RogueBit.Core.Pathing;
using RogueBit.Core.Vision;

/// <summary>What the player asked to do this turn.</summary>
public enum ActionResult
{
    /// <summary>Nothing happened and no turn was taken.</summary>
    Refused,

    /// <summary>The action happened and every monster has now moved.</summary>
    Took,
}

/// <summary>
/// One game, from the first floor to the death that ends it.
///
/// The run owns every rule and knows nothing about drawing. That is what lets
/// the whole game be played out in a test with no display attached, and what
/// makes a seed enough to replay a run exactly.
/// </summary>
public sealed class Run
{
    public const int MapWidth = 78;
    public const int MapHeight = 34;
    public const int VisionRadius = 9;

    private readonly List<Monster> monsters = [];
    private readonly List<Item> items = [];
    private readonly List<TurnEvent> turnEvents = [];

    public int Seed { get; }

    public SeededRandom Random { get; private set; }

    public DungeonMap Map { get; private set; } = null!;

    public Player Player { get; private set; } = null!;

    public Inventory Inventory => Player.Inventory;

    public MessageLog Log { get; } = new();

    public int Depth { get; private set; }

    /// <summary>How many turns the player has taken in this run.</summary>
    public int Turns { get; private set; }

    /// <summary>True when the deep warden is dead and the run was won.</summary>
    public bool HasWon { get; private set; }

    /// <summary>True when the run has ended, whichever way it ended.</summary>
    public bool IsOver => !Player.IsAlive || HasWon;

    public IReadOnlyList<Monster> Monsters => monsters;

    public IReadOnlyList<Item> Items => items;

    /// <summary>
    /// What happened during the last action, for a frontend to draw. Cleared at
    /// the start of every action, so it never describes an older turn.
    /// </summary>
    public IReadOnlyList<TurnEvent> LastTurnEvents => turnEvents;

    /// <summary>
    /// The score: coins gathered, a bonus for every floor descended, and a
    /// large one for reaching the bottom and killing what lives there.
    /// </summary>
    public int Score => Player.Coins + ((Depth - 1) * 10) + (HasWon ? GameRules.VictoryBonus : 0);

    public Run(int seed)
    {
        Seed = seed;
        Random = new SeededRandom(seed);
        Player = new Player(new Position(0, 0));

        EnterFloor(1);
        Log.Add($"You enter the dungeon. Seed {seed}.", MessageKind.Good);
    }

    private Run(SeededRandom random, DungeonMap map, Player player, int depth, int turns)
    {
        Seed = random.Seed;
        Random = random;
        Map = map;
        Player = player;
        Depth = depth;
        Turns = turns;
    }

    /// <summary>
    /// Rebuilds a run that was saved. Everything is handed in rather than
    /// generated, because a resumed run has to be the run that was left and not
    /// a fresh one on the same seed.
    /// </summary>
    public static Run Resume(
        SeededRandom random,
        DungeonMap map,
        Player player,
        IEnumerable<Monster> monsters,
        IEnumerable<Item> items,
        int depth,
        int turns,
        IEnumerable<(string Text, MessageKind Kind, int Count)> log)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(monsters);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(log);

        Run run = new(random, map, player, depth, turns);
        run.monsters.AddRange(monsters);
        run.items.AddRange(items);

        foreach ((string text, MessageKind kind, int count) in log)
        {
            for (int i = 0; i < Math.Max(1, count); i++) run.Log.Add(text, kind);
        }

        run.UpdateVision();
        return run;
    }

    /// <summary>Starts the same run again from the top, on the same seed.</summary>
    public Run Restart() => new(Seed);

    /// <summary>The monster standing on a cell, if one is.</summary>
    public Monster? MonsterAt(Position position) =>
        monsters.FirstOrDefault(m => m.IsAlive && m.Position == position);

    /// <summary>The items lying on a cell.</summary>
    public IEnumerable<Item> ItemsAt(Position position) => items.Where(i => i.Position == position);

    /// <summary>True when something is standing on a cell already.</summary>
    public bool IsOccupied(Position position) =>
        (Player.IsAlive && Player.Position == position) || MonsterAt(position) is not null;

    /// <summary>
    /// Walks one step, or attacks whatever is in the way. A move into a wall is
    /// refused and costs no turn, which is what stops a misread key killing the
    /// player.
    /// </summary>
    public ActionResult Move(Position step)
    {
        if (IsOver) return ActionResult.Refused;

        Position target = Player.Position + step;

        if (MonsterAt(target) is { } monster)
        {
            BeginTurn();
            AttackMonster(monster);
            EndTurn();
            return ActionResult.Took;
        }

        if (!Map.IsWalkable(target)) return ActionResult.Refused;

        BeginTurn();
        Player.Position = target;
        TakeCoinsUnderfoot();
        AnnounceWhatIsUnderfoot();
        EndTurn();
        return ActionResult.Took;
    }

    /// <summary>Stands still for a turn, which lets the monsters close in.</summary>
    public ActionResult Wait()
    {
        if (IsOver) return ActionResult.Refused;

        BeginTurn();
        EndTurn();
        return ActionResult.Took;
    }

    /// <summary>Picks up whatever is underfoot that is not taken automatically.</summary>
    public ActionResult PickUp()
    {
        if (IsOver) return ActionResult.Refused;

        Item? item = ItemsAt(Player.Position).FirstOrDefault(i => !i.IsPickedUpByWalkingOver);

        if (item is null)
        {
            Log.Add("There is nothing here to pick up.");
            return ActionResult.Refused;
        }

        if (!Inventory.TryAdd(item))
        {
            Log.Add("You are carrying too much already.", MessageKind.Warning);
            return ActionResult.Refused;
        }

        BeginTurn();
        items.Remove(item);
        Log.Add($"You pick up {item.Name}.", MessageKind.Good);
        turnEvents.Add(new TurnEvent(TurnEventKind.Pickup, Player.Position));
        EndTurn();
        return ActionResult.Took;
    }

    /// <summary>Drinks a potion, or puts a piece of equipment on.</summary>
    public ActionResult Use(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (IsOver) return ActionResult.Refused;

        if (item.IsEquipment)
        {
            if (!Inventory.TryEquip(item)) return ActionResult.Refused;

            BeginTurn();
            Log.Add($"You ready {item.Name}.", MessageKind.Good);
            EndTurn();
            return ActionResult.Took;
        }

        if (item.Kind != ItemKind.Potion) return ActionResult.Refused;

        BeginTurn();
        int restored = Player.Heal(item.Healing);
        Inventory.Remove(item);

        Log.Add(
            restored > 0 ? $"You drink {item.Name} and recover {restored}." : "You are already whole.",
            restored > 0 ? MessageKind.Good : MessageKind.Normal);

        if (restored > 0) turnEvents.Add(new TurnEvent(TurnEventKind.Heal, Player.Position, restored));

        EndTurn();
        return ActionResult.Took;
    }

    /// <summary>Goes down the stairs, if the player is standing on them.</summary>
    public ActionResult Descend()
    {
        if (IsOver) return ActionResult.Refused;

        if (GameRules.IsFinalDepth(Depth))
        {
            Log.Add("Nothing goes deeper than this. The warden is down here with you.", MessageKind.Warning);
            return ActionResult.Refused;
        }

        if (Map[Player.Position] != TileKind.StairsDown)
        {
            Log.Add("There are no stairs here.");
            return ActionResult.Refused;
        }

        BeginTurn();
        EnterFloor(Depth + 1);
        Log.Add($"You descend to floor {Depth}.", MessageKind.Good);
        turnEvents.Add(new TurnEvent(TurnEventKind.Descend, Player.Position));
        return ActionResult.Took;
    }

    private void EnterFloor(int depth)
    {
        Depth = depth;

        // Alternate the two generators, so a run does not look the same all the
        // way down. The choice is a function of depth, so a seed still replays.
        IDungeonGenerator generator = depth % 2 == 1
            ? new BspDungeonGenerator()
            : new DrunkardWalkGenerator();

        Map = generator.Generate(MapWidth, MapHeight, Random);
        Player.Position = Map.Entrance;

        // The bottom floor has nowhere to go on to, so its stairs are filled in.
        if (GameRules.IsFinalDepth(depth)) Map[Map.StairsDown] = TileKind.Floor;

        // The first floor is the way in from outside, and that way does not
        // open again. Below it, the player arrives on the stairs back up.
        if (depth > 1) Map[Map.Entrance] = TileKind.StairsUp;

        monsters.Clear();
        items.Clear();

        PopulateFloor(depth);
        UpdateVision();
    }

    private void PopulateFloor(int depth)
    {
        List<Position> free = [.. Map.WalkableCells().Where(c => c != Map.Entrance)];
        Random.Shuffle(free);

        int next = 0;
        Position? Take() => next < free.Count ? free[next++] : null;

        if (SpawnTable.HasBoss(depth) && Take() is { } bossCell)
        {
            monsters.Add(SpawnTable.Boss(bossCell, depth));
        }

        for (int i = 0; i < SpawnTable.MonsterCount(depth); i++)
        {
            if (Take() is not { } cell) break;
            monsters.Add(SpawnTable.CreateMonster(cell, depth, Random));
        }

        for (int i = 0; i < SpawnTable.CoinCount(depth); i++)
        {
            if (Take() is not { } cell) break;
            items.Add(Item.Coin(cell, 1 + (depth / 3)));
        }

        for (int i = 0; i < SpawnTable.PotionCount(depth); i++)
        {
            if (Take() is not { } cell) break;
            items.Add(Item.Potion(cell));
        }

        if (Take() is { } equipmentCell && SpawnTable.CreateEquipment(equipmentCell, depth, Random) is { } equipment)
        {
            items.Add(equipment);
        }
    }

    private void AttackMonster(Monster monster)
    {
        AttackResult result = CombatResolver.Resolve(Player.Power, monster);
        Log.Add(
            CombatResolver.Describe("you", monster.Name, result, attackerIsPlayer: true),
            result.Killed ? MessageKind.Good : MessageKind.Normal);

        turnEvents.Add(new TurnEvent(
            result.Hit ? TurnEventKind.Hit : TurnEventKind.Blocked,
            monster.Position,
            result.Damage));

        if (!result.Killed) return;

        turnEvents.Add(new TurnEvent(TurnEventKind.Death, monster.Position));
        Player.TakeCoins(monster.CoinReward);
        monsters.Remove(monster);

        if (monster.Behaviour == MonsterBehaviour.Boss && GameRules.IsFinalDepth(Depth))
        {
            HasWon = true;
            Log.Add($"{Capitalise(monster.Name)} falls. You have reached the bottom and lived.", MessageKind.Good);
        }
    }

    private void TakeCoinsUnderfoot()
    {
        foreach (Item coin in ItemsAt(Player.Position).Where(i => i.IsPickedUpByWalkingOver).ToList())
        {
            Player.TakeCoins(coin.Value);
            items.Remove(coin);
        }
    }

    private void AnnounceWhatIsUnderfoot()
    {
        foreach (Item item in ItemsAt(Player.Position))
        {
            Log.Add($"You see {item.Name} here.");
        }

        if (Map[Player.Position] == TileKind.StairsDown)
        {
            Log.Add("A staircase leads down from here.");
        }
    }

    /// <summary>
    /// Marks the point where an action is certain to happen. Everything before
    /// this can still be refused, and a refusal must leave the previous turn's
    /// events alone, because nothing has happened to replace them.
    /// </summary>
    private void BeginTurn() => turnEvents.Clear();

    private void EndTurn()
    {
        Turns++;

        // A run that has just been won is over. Letting the monsters take one
        // more turn here would let them kill a player who has already finished.
        if (!HasWon) MonstersAct();

        UpdateVision();

        if (!Player.IsAlive)
        {
            Log.Add($"You die on floor {Depth}, with {Score} points.", MessageKind.Bad);
        }
        else if (HasWon)
        {
            Log.Add($"You win, on turn {Turns}, with {Score} points.", MessageKind.Good);
        }
    }

    private void MonstersAct()
    {
        // Copy the list: a monster can die during the loop.
        foreach (Monster monster in monsters.ToList())
        {
            if (!monster.IsAlive || !Player.IsAlive) continue;

            for (int step = 0; step < monster.Speed && Player.IsAlive; step++)
            {
                TakeMonsterTurn(monster);
            }
        }
    }

    private void TakeMonsterTurn(Monster monster)
    {
        int distance = monster.Position.ManhattanDistanceTo(Player.Position);

        if (distance > monster.AggroRadius)
        {
            Wander(monster);
            return;
        }

        if (monster.Behaviour == MonsterBehaviour.Archer && TryShoot(monster, distance)) return;

        if (distance == 1)
        {
            AttackPlayer(monster);
            return;
        }

        Position? next = PathFinder.NextStep(Map, monster.Position, Player.Position, IsOccupied);
        if (next is { } cell && !IsOccupied(cell)) monster.Position = cell;
    }

    private bool TryShoot(Monster archer, int distance)
    {
        // An archer standing next to its target is being mobbed, and swings
        // instead of shooting.
        if (distance <= 1 || distance > archer.Range) return false;
        if (!Line.IsClear(Map, archer.Position, Player.Position)) return false;

        AttackResult result = CombatResolver.Resolve(archer.EffectivePower, Player);
        Log.Add($"{Capitalise(archer.Name)} shoots you for {result.Damage}.", MessageKind.Bad);
        turnEvents.Add(new TurnEvent(TurnEventKind.Shot, archer.Position));
        turnEvents.Add(new TurnEvent(TurnEventKind.Hit, Player.Position, result.Damage, AgainstPlayer: true));
        return true;
    }

    private void AttackPlayer(Monster monster)
    {
        AttackResult result = CombatResolver.Resolve(monster.EffectivePower, Player);
        string name = monster.IsEnraged ? $"{monster.Name}, enraged," : monster.Name;
        Log.Add(CombatResolver.Describe(name, "you", result, attackerIsPlayer: false), MessageKind.Bad);

        turnEvents.Add(new TurnEvent(
            result.Hit ? TurnEventKind.Hit : TurnEventKind.Blocked,
            Player.Position,
            result.Damage,
            AgainstPlayer: true));

        if (!Player.IsAlive) turnEvents.Add(new TurnEvent(TurnEventKind.Death, Player.Position, AgainstPlayer: true));
    }

    private void Wander(Monster monster)
    {
        Position target = monster.Position + Random.Pick(Directions.Cardinal);

        if (Map.IsWalkable(target) && !IsOccupied(target)) monster.Position = target;
    }

    private void UpdateVision() => FieldOfView.Compute(Map, Player.Position, VisionRadius);

    private static string Capitalise(string text) =>
        text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];
}
