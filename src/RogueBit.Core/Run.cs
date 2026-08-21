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

    private readonly List<TurnEvent> turnEvents = [];

    /// <summary>
    /// Every floor the run has been on, by depth.
    ///
    /// A floor is kept rather than built again, because building it again is
    /// not the same floor. The monsters would be back, the potion the player
    /// walked past would be somewhere else, and turning round would cost
    /// nothing, because the ground behind would be new ground. Keeping it is
    /// what makes going back up a decision instead of a reset.
    /// </summary>
    private readonly Dictionary<int, Floor> floors = [];

    private Floor floor = null!;

    public int Seed { get; }

    public SeededRandom Random { get; private set; }

    public DungeonMap Map => floor.Map;

    public Player Player { get; private set; } = null!;

    public Inventory Inventory => Player.Inventory;

    public MessageLog Log { get; } = new();

    public int Depth { get; private set; }

    /// <summary>
    /// The deepest floor this run has reached. Turning back does not undo
    /// having been down there, so this is what the score is paid on.
    /// </summary>
    public int DeepestDepth { get; private set; }

    /// <summary>How many turns the player has taken in this run.</summary>
    public int Turns { get; private set; }

    /// <summary>True when the deep warden is dead and the run was won.</summary>
    public bool HasWon { get; private set; }

    /// <summary>True when the run has ended, whichever way it ended.</summary>
    public bool IsOver => !Player.IsAlive || HasWon;

    /// <summary>Every floor this run has been on, the shallowest first.</summary>
    public IEnumerable<Floor> Floors => floors.Values.OrderBy(f => f.Depth);

    public IReadOnlyList<Monster> Monsters => floor.Monsters;

    public IReadOnlyList<Item> Items => floor.Items;

    /// <summary>
    /// What happened during the last action, for a frontend to draw. Cleared at
    /// the start of every action, so it never describes an older turn.
    /// </summary>
    public IReadOnlyList<TurnEvent> LastTurnEvents => turnEvents;

    /// <summary>
    /// The score: coins gathered, a bonus for every floor descended, and a
    /// large one for reaching the bottom and killing what lives there.
    ///
    /// The depth bonus is paid on the deepest floor reached rather than the
    /// floor the player is standing on. Retreating is meant to be a decision
    /// about surviving, and a score that fell as the player climbed would make
    /// it a decision about points instead.
    /// </summary>
    public int Score =>
        Player.Coins + ((DeepestDepth - 1) * 10) + (HasWon ? GameRules.VictoryBonus : 0);

    public Run(int seed)
    {
        Seed = seed;
        Random = new SeededRandom(seed);
        Player = new Player(new Position(0, 0));

        EnterFloor(1, Arrival.FromAbove);
        Log.Add($"You enter the dungeon. Seed {seed}.", MessageKind.Good);
    }

    private Run(SeededRandom random, Player player, int depth, int deepestDepth, int turns)
    {
        Seed = random.Seed;
        Random = random;
        Player = player;
        Depth = depth;

        // A run cannot have been shallower than the floor it is standing on,
        // whatever it was told.
        DeepestDepth = Math.Max(depth, deepestDepth);
        Turns = turns;
    }

    /// <summary>
    /// Rebuilds a run that was saved. Everything is handed in rather than
    /// generated, because a resumed run has to be the run that was left and not
    /// a fresh one on the same seed.
    ///
    /// Every floor the run had been on is handed in, not only the one the
    /// player is standing on. A run that came back with one floor could walk
    /// up a staircase into ground it had never seen, which is the one thing
    /// keeping floors is meant to stop.
    /// </summary>
    public static Run Resume(
        SeededRandom random,
        Player player,
        IEnumerable<Floor> floors,
        int depth,
        int deepestDepth,
        int turns,
        IEnumerable<(string Text, MessageKind Kind, int Count)> log)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(floors);
        ArgumentNullException.ThrowIfNull(log);

        Run run = new(random, player, depth, deepestDepth, turns);

        foreach (Floor kept in floors) run.floors[kept.Depth] = kept;

        if (!run.floors.TryGetValue(depth, out Floor? current))
        {
            throw new ArgumentException(
                $"The run is on floor {depth} and no floor for that depth was handed in.",
                nameof(floors));
        }

        run.floor = current;

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
        floor.Monsters.FirstOrDefault(m => m.IsAlive && m.Position == position);

    /// <summary>
    /// The monster beside the player that is closest to dying, or nothing when
    /// none is beside it.
    ///
    /// Beside means sharing an edge or a corner, which is what the player can
    /// walk into and therefore what it can hit. The weakest is the one worth
    /// reporting, because it is the one worth hitting: knowing a goblin has
    /// two left is what decides between one more swing and running.
    ///
    /// The run decides which monster that is. It does not decide how, or
    /// whether, anybody draws it.
    /// </summary>
    public Monster? MonsterWithinReach
    {
        get
        {
            Monster? weakest = null;

            foreach (Position step in Directions.All)
            {
                if (MonsterAt(Player.Position + step) is not { } monster) continue;
                if (weakest is null || monster.Health < weakest.Health) weakest = monster;
            }

            return weakest;
        }
    }

    /// <summary>The items lying on a cell.</summary>
    public IEnumerable<Item> ItemsAt(Position position) => floor.Items.Where(i => i.Position == position);

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

        // Whether the step is legal is asked before anything else, so a blow
        // cannot be swung through the corner of a wall either.
        if (!Map.CanStep(Player.Position, target)) return ActionResult.Refused;

        if (MonsterAt(target) is { } monster)
        {
            BeginTurn();
            AttackMonster(monster);
            EndTurn();
            return ActionResult.Took;
        }

        BeginTurn();
        Player.Position = target;
        SpringTrapUnder(Player, isPlayer: true);

        if (Player.IsAlive)
        {
            TakeCoinsUnderfoot();
            AnnounceWhatIsUnderfoot();
        }

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
        floor.Items.Remove(item);
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

    /// <summary>Which way the player came onto a floor.</summary>
    private enum Arrival
    {
        /// <summary>Down the stairs from the floor above.</summary>
        FromAbove,

        /// <summary>Up the stairs from the floor below.</summary>
        FromBelow,
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
        EnterFloor(Depth + 1, Arrival.FromAbove);
        Log.Add($"You descend to floor {Depth}.", MessageKind.Good);
        turnEvents.Add(new TurnEvent(TurnEventKind.Descend, Player.Position));
        return ActionResult.Took;
    }

    /// <summary>
    /// Takes whichever staircase the player is standing on.
    ///
    /// A cell holds one staircase or none, so there is never a choice to make
    /// and never a second key to learn. Standing on nothing reports the same
    /// refusal as trying to go down without stairs, which is the more common
    /// mistake of the two.
    /// </summary>
    public ActionResult TakeStairs() =>
        Map[Player.Position] == TileKind.StairsUp ? Ascend() : Descend();

    /// <summary>Goes back up the stairs, if the player is standing on them.</summary>
    public ActionResult Ascend()
    {
        if (IsOver) return ActionResult.Refused;

        // The first floor has no such tile anywhere, so this one test also
        // covers the way out of the dungeon being shut.
        if (Map[Player.Position] != TileKind.StairsUp)
        {
            Log.Add("There are no stairs up here.");
            return ActionResult.Refused;
        }

        BeginTurn();
        EnterFloor(Depth - 1, Arrival.FromBelow);
        Log.Add($"You climb back to floor {Depth}.", MessageKind.Good);
        turnEvents.Add(new TurnEvent(TurnEventKind.Ascend, Player.Position));
        return ActionResult.Took;
    }

    private void EnterFloor(int depth, Arrival arrival)
    {
        Depth = depth;
        DeepestDepth = Math.Max(DeepestDepth, depth);

        if (!floors.TryGetValue(depth, out Floor? kept))
        {
            kept = BuildFloor(depth);
            floors[depth] = kept;
        }

        floor = kept;
        Player.Position = arrival == Arrival.FromAbove ? Map.Entrance : Map.StairsDown;
        UpdateVision();
    }

    private Floor BuildFloor(int depth)
    {
        // Alternate the two generators, so a run does not look the same all the
        // way down. The choice is a function of depth, so a seed still replays.
        IDungeonGenerator generator = depth % 2 == 1
            ? new BspDungeonGenerator()
            : new DrunkardWalkGenerator();

        DungeonMap map = generator.Generate(MapWidth, MapHeight, Random);

        // The bottom floor has nowhere to go on to, so its stairs are filled in.
        if (GameRules.IsFinalDepth(depth)) map[map.StairsDown] = TileKind.Floor;

        // The first floor is the way in from outside, and that way does not
        // open again. Below it, the player arrives on the stairs back up.
        if (depth > 1) map[map.Entrance] = TileKind.StairsUp;

        Floor built = new() { Depth = depth, Map = map };
        PopulateFloor(built);
        return built;
    }

    private void PopulateFloor(Floor built)
    {
        int depth = built.Depth;
        List<Position> free = [.. built.Map.WalkableCells().Where(c => c != built.Map.Entrance)];
        Random.Shuffle(free);

        int next = 0;
        Position? Take() => next < free.Count ? free[next++] : null;

        if (SpawnTable.HasBoss(depth) && Take() is { } bossCell)
        {
            built.Monsters.Add(SpawnTable.Boss(bossCell, depth));
        }

        for (int i = 0; i < SpawnTable.MonsterCount(depth); i++)
        {
            if (Take() is not { } cell) break;
            built.Monsters.Add(SpawnTable.CreateMonster(cell, depth, Random));
        }

        for (int i = 0; i < SpawnTable.CoinCount(depth); i++)
        {
            if (Take() is not { } cell) break;
            built.Items.Add(Item.Coin(cell, 1 + (depth / 3)));
        }

        for (int i = 0; i < SpawnTable.PotionCount(depth); i++)
        {
            if (Take() is not { } cell) break;
            built.Items.Add(Item.Potion(cell));
        }

        if (Take() is { } equipmentCell && SpawnTable.CreateEquipment(equipmentCell, depth, Random) is { } equipment)
        {
            built.Items.Add(equipment);
        }

        // Traps are written into the ground rather than laid on it, so they
        // are the last thing placed and they take cells nothing else wanted.
        for (int i = 0; i < SpawnTable.TrapCount(depth); i++)
        {
            if (Take() is not { } cell) break;
            if (built.Map[cell] != TileKind.Floor) continue;

            built.Map[cell] = TileKind.TrapArmed;
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

        MonsterDies(monster, toThePlayersHand: true);
    }

    /// <summary>
    /// Everything that follows a monster dying, wherever the blow came from.
    ///
    /// This is one method because the ending depends on it. A warden that died
    /// some other way and skipped this would leave a run on the bottom floor
    /// with nothing left to kill and no way to win.
    /// </summary>
    private void MonsterDies(Monster monster, bool toThePlayersHand)
    {
        turnEvents.Add(new TurnEvent(TurnEventKind.Death, monster.Position));

        if (toThePlayersHand) Player.TakeCoins(monster.CoinReward);

        floor.Monsters.Remove(monster);

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
            floor.Items.Remove(coin);
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
        else if (Map[Player.Position] == TileKind.StairsUp)
        {
            Log.Add("A staircase leads back up from here.");
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
        foreach (Monster monster in floor.Monsters.ToList())
        {
            if (!monster.IsAlive || !Player.IsAlive) continue;

            // A monster can now die during its own turn, by walking onto a
            // trap, so its second step is not a given.
            for (int step = 0; step < monster.Speed && Player.IsAlive && monster.IsAlive; step++)
            {
                TakeMonsterTurn(monster);
            }
        }
    }

    private void TakeMonsterTurn(Monster monster)
    {
        // Chebyshev, because a monster moves in eight directions now. Under
        // Manhattan a monster standing on a corner was two away, so it would
        // never swing and would spend its turn trying to walk onto the player.
        int distance = monster.Position.ChebyshevDistanceTo(Player.Position);

        // Being roused is asked about beside the distance rather than folded
        // into the radius. A radius that changed would also change how far a
        // monster shoots and how far it can be led away, and none of that is
        // what hearing a noise should do.
        if (distance > monster.AggroRadius && !monster.IsAlerted)
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
        if (next is not { } cell || IsOccupied(cell)) return;

        monster.Position = cell;
        SpringTrapUnder(monster, isPlayer: false);
    }

    /// <summary>
    /// Fires the trap under an actor, if the cell holds one still armed.
    ///
    /// A trap does not care who stood on it, which is what makes leading a
    /// monster across one worth doing. It goes off once and is then spent and
    /// visible, so the floor teaches the player where it was.
    /// </summary>
    private void SpringTrapUnder(Actor actor, bool isPlayer)
    {
        if (Map[actor.Position] != TileKind.TrapArmed) return;

        Map[actor.Position] = TileKind.TrapSprung;

        int damage = actor.TakeDamage(SpawnTable.TrapDamage(Depth));

        Log.Add(
            isPlayer
                ? $"A trap goes off under you for {damage}."
                : $"A trap goes off under {actor.Name} for {damage}.",
            isPlayer ? MessageKind.Bad : MessageKind.Good);

        turnEvents.Add(new TurnEvent(TurnEventKind.Trap, actor.Position, damage, AgainstPlayer: isPlayer));

        if (actor.IsAlive) return;

        if (isPlayer)
        {
            turnEvents.Add(new TurnEvent(TurnEventKind.Death, actor.Position, AgainstPlayer: true));
        }
        else if (actor is Monster caught)
        {
            // Nothing is paid for this one. The player did not strike the
            // blow, and paying for it would turn trap hunting into a living.
            MonsterDies(caught, toThePlayersHand: false);
        }
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
        Position target = monster.Position + Random.Pick(Directions.All);

        if (Map.CanStep(monster.Position, target) && !IsOccupied(target)) monster.Position = target;
    }

    private void UpdateVision() => FieldOfView.Compute(Map, Player.Position, VisionRadius);

    private static string Capitalise(string text) =>
        text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];
}
