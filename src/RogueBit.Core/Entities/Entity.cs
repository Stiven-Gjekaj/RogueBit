namespace RogueBit.Core.Entities;

/// <summary>Anything that stands on a cell of the map.</summary>
public abstract class Entity
{
    public Position Position { get; set; }

    public required char Glyph { get; init; }

    public required string Name { get; init; }

    /// <summary>True when another entity cannot walk onto this cell.</summary>
    public virtual bool BlocksMovement => false;

    protected Entity(Position position) => Position = position;
}
