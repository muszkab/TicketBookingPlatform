using System;

namespace Domain.Common;

public abstract class Entity
{
    protected Entity() { Id = Guid.Empty; }

    protected Entity(Guid id) => Id = id;

    protected static Guid CreateId() => Guid.CreateVersion7();

    public Guid Id { get; protected set; }

    public bool IsTransient => Id == Guid.Empty;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        if (IsTransient || other.IsTransient)
            return false;

        return Id == other.Id;
    }

    public override int GetHashCode()
        => IsTransient ? base.GetHashCode() : Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity? left, Entity? right)
        => !(left == right);
}
