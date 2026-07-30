using System;

namespace Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();

    public override bool Equals(object? obj)
        => obj is Entity other && other.GetType() == GetType() && other.Id == Id;

    public override int GetHashCode() => Id.GetHashCode();
}
