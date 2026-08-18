namespace RogueBit.Core.Map;

/// <summary>
/// Carves rooms and corridors by splitting the floor in two over and over.
///
/// The space is cut into a tree of smaller and smaller blocks. Each leaf gets
/// one room, and each split joins the rooms of its two halves with a corridor.
/// Joining at every split, rather than joining the rooms in a list, is what
/// makes the result connected by construction.
/// </summary>
public sealed class BspDungeonGenerator : IDungeonGenerator
{
    private readonly int minimumLeafSize;
    private readonly int minimumRoomSize;

    public string Name => "rooms";

    public BspDungeonGenerator(int minimumLeafSize = 9, int minimumRoomSize = 4)
    {
        if (minimumRoomSize < 2) throw new ArgumentOutOfRangeException(nameof(minimumRoomSize));
        if (minimumLeafSize < minimumRoomSize + 2) throw new ArgumentOutOfRangeException(nameof(minimumLeafSize));

        this.minimumLeafSize = minimumLeafSize;
        this.minimumRoomSize = minimumRoomSize;
    }

    public DungeonMap Generate(int width, int height, SeededRandom random)
    {
        DungeonMap map = new(width, height);

        // Inset by one so the floor always keeps a wall around its edge.
        Rectangle whole = new(1, 1, width - 2, height - 2);
        List<Rectangle> rooms = [];

        Split(map, whole, rooms, random);

        // A map too small to split still needs somewhere to stand.
        if (rooms.Count == 0)
        {
            Rectangle fallback = new(1, 1, Math.Max(1, width - 2), Math.Max(1, height - 2));
            CarveRoom(map, fallback);
            rooms.Add(fallback);
        }

        MapRegions.ConnectAll(map);

        map.Rooms = rooms;
        map.Entrance = rooms[0].Centre;
        map.StairsDown = rooms[^1].Centre;
        map[map.StairsDown] = TileKind.StairsDown;

        PlaceDoors(map, rooms);

        return map;
    }

    /// <summary>
    /// Puts a door where a corridor arrives at a room.
    ///
    /// A doorway is a walkable cell that belongs to no room, has exactly one
    /// walkable neighbour that does belong to one, and exactly one walkable
    /// neighbour that does not. That is the throat of a corridor at the point
    /// it reaches a wall: the only place narrow enough for a door to be worth
    /// anything, and the only place one can stand without walling off an open
    /// space from itself.
    ///
    /// The entrance and the stairs are left alone. A player cannot see the
    /// ground it is standing on through a door it is standing in.
    /// </summary>
    private static void PlaceDoors(DungeonMap map, List<Rectangle> rooms)
    {
        bool InARoom(Position cell) => rooms.Any(room => room.Contains(cell));

        foreach (Position cell in map.WalkableCells().ToList())
        {
            if (cell == map.Entrance || cell == map.StairsDown) continue;
            if (InARoom(cell)) continue;

            int intoARoom = 0;
            int alongTheCorridor = 0;

            foreach (Position step in Directions.Cardinal)
            {
                Position neighbour = cell + step;
                if (!map.IsWalkable(neighbour)) continue;

                if (InARoom(neighbour)) intoARoom++;
                else alongTheCorridor++;
            }

            if (intoARoom == 1 && alongTheCorridor == 1) map[cell] = TileKind.Door;
        }
    }

    /// <summary>Splits a block, or carves a room when it cannot be split again.</summary>
    private Rectangle Split(DungeonMap map, Rectangle area, List<Rectangle> rooms, SeededRandom random)
    {
        bool canSplitHorizontally = area.Height >= minimumLeafSize * 2;
        bool canSplitVertically = area.Width >= minimumLeafSize * 2;

        if (!canSplitHorizontally && !canSplitVertically)
        {
            Rectangle room = CarveRoomIn(map, area, random);
            rooms.Add(room);
            return room;
        }

        bool splitVertically = canSplitVertically && (!canSplitHorizontally || random.Chance(0.5));

        Rectangle first;
        Rectangle second;

        if (splitVertically)
        {
            int cut = random.Between(minimumLeafSize, area.Width - minimumLeafSize);
            first = new Rectangle(area.X, area.Y, cut, area.Height);
            second = new Rectangle(area.X + cut, area.Y, area.Width - cut, area.Height);
        }
        else
        {
            int cut = random.Between(minimumLeafSize, area.Height - minimumLeafSize);
            first = new Rectangle(area.X, area.Y, area.Width, cut);
            second = new Rectangle(area.X, area.Y + cut, area.Width, area.Height - cut);
        }

        Rectangle left = Split(map, first, rooms, random);
        Rectangle right = Split(map, second, rooms, random);

        // Join the two halves before returning, so every split is joined once.
        MapRegions.CarveCorridor(map, left.Centre, right.Centre);

        return random.Chance(0.5) ? left : right;
    }

    private Rectangle CarveRoomIn(DungeonMap map, Rectangle area, SeededRandom random)
    {
        int roomWidth = random.Between(Math.Min(minimumRoomSize, area.Width), Math.Max(1, area.Width - 1));
        int roomHeight = random.Between(Math.Min(minimumRoomSize, area.Height), Math.Max(1, area.Height - 1));

        roomWidth = Math.Clamp(roomWidth, 1, area.Width);
        roomHeight = Math.Clamp(roomHeight, 1, area.Height);

        int x = random.Between(area.X, area.X + area.Width - roomWidth);
        int y = random.Between(area.Y, area.Y + area.Height - roomHeight);

        Rectangle room = new(x, y, roomWidth, roomHeight);
        CarveRoom(map, room);
        return room;
    }

    private static void CarveRoom(DungeonMap map, Rectangle room)
    {
        foreach (Position cell in room.Cells())
        {
            if (map.Contains(cell)) map[cell] = TileKind.Floor;
        }
    }
}
