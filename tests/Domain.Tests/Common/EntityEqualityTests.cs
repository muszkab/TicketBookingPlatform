using Domain.Common;
using System;

namespace Domain.Tests.Common;

public class EntityEqualityTests
{
    private sealed class FakeEntityA : Entity { public FakeEntityA(Guid id) { Id = id; } }
    private sealed class FakeEntityB : Entity { public FakeEntityB(Guid id) { Id = id; } }

    [Fact]
    public void Equals_Should_BeTrue_When_SameTypeAndId()
    {
        var id = Guid.NewGuid();
        new FakeEntityA(id).Equals(new FakeEntityA(id)).Should().BeTrue();
    }

    [Fact]
    public void Equals_Should_BeFalse_When_DifferentIds()
    {
        new FakeEntityA(Guid.NewGuid()).Equals(new FakeEntityA(Guid.NewGuid())).Should().BeFalse();
    }

    [Fact]
    public void Equals_Should_BeFalse_When_DifferentRuntimeType()
    {
        var id = Guid.NewGuid();
        new FakeEntityA(id).Equals(new FakeEntityB(id)).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_Should_MatchId()
    {
        var id = Guid.NewGuid();
        new FakeEntityA(id).GetHashCode().Should().Be(id.GetHashCode());
    }
}
