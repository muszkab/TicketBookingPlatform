using Domain.Common;
using Domain.Common.Exceptions;
using Domain.Events;
using Domain.Tests.Common.Builders;
using System;

namespace Domain.Tests.Events;

public class EventTests
{
    private static Money Price(decimal amount = 10m) => new(amount, "EUR");

    [Fact]
    public void Ctor_Should_Throw_When_TitleMissing()
    {
        Action act = () => new EventBuilder().WithTitle(" ").Build();
        act.Should().Throw<ArgumentException>().WithParameterName("title");
    }

    [Fact]
    public void Ctor_Should_Throw_When_EndsAtNotAfterStartsAt()
    {
        var now = DateTimeOffset.UtcNow;
        Action act = () => new EventBuilder().WithSchedule(now, now).Build();
        act.Should().Throw<ArgumentException>().WithParameterName("endsAt");
    }

    [Fact]
    public void Ctor_Should_Throw_When_LocationIdEmpty()
    {
        Action act = () => new EventBuilder().WithLocation(Guid.Empty).Build();
        act.Should().Throw<ArgumentException>().WithParameterName("locationId");
    }

    [Fact]
    public void Ctor_Should_StartInDraft()
    {
        new EventBuilder().Build().Status.Should().Be(EventStatus.Draft);
    }

    [Fact]
    public void UpdateDetails_Should_Throw_When_NotDraft()
    {
        var ev = new EventBuilder().Build();
        ev.PutOnSale();

        Action act = () => ev.UpdateDetails("t", "d", EventCategory.Concert,
            DateTimeOffset.UtcNow.AddDays(2), DateTimeOffset.UtcNow.AddDays(3));
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void UpdateDetails_Should_Validate_TitleAndSchedule()
    {
        var ev = new EventBuilder().Build();
        var t = DateTimeOffset.UtcNow.AddDays(1);
        FluentActions.Invoking(() => ev.UpdateDetails("", "d", EventCategory.Concert, t, t.AddHours(1)))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => ev.UpdateDetails("ok", "d", EventCategory.Concert, t, t))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateDetails_Should_UpdateFields_When_Draft()
    {
        var ev = new EventBuilder().Build();
        var start = DateTimeOffset.UtcNow.AddDays(5);
        var end = start.AddHours(3);
        ev.UpdateDetails("New", "NewDesc", EventCategory.Theater, start, end);

        ev.Title.Should().Be("New");
        ev.Description.Should().Be("NewDesc");
        ev.Category.Should().Be(EventCategory.Theater);
        ev.StartsAt.Should().Be(start);
        ev.EndsAt.Should().Be(end);
    }

    [Theory]
    [InlineData(EventStatus.OnSale)]
    [InlineData(EventStatus.SoldOut)]
    [InlineData(EventStatus.Cancelled)]
    [InlineData(EventStatus.Completed)]
    public void PutOnSale_Should_Throw_When_NotDraft(EventStatus from)
    {
        var ev = MoveTo(from);
        Action act = () => ev.PutOnSale();
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void PutOnSale_Should_TransitionFromDraft()
    {
        var ev = new EventBuilder().Build();
        ev.PutOnSale();
        ev.Status.Should().Be(EventStatus.OnSale);
    }

    [Theory]
    [InlineData(EventStatus.Draft)]
    [InlineData(EventStatus.SoldOut)]
    [InlineData(EventStatus.Cancelled)]
    [InlineData(EventStatus.Completed)]
    public void MarkAsSoldOut_Should_Throw_When_NotOnSale(EventStatus from)
    {
        var ev = MoveTo(from);
        Action act = () => ev.MarkAsSoldOut();
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void MarkAsSoldOut_Should_TransitionFromOnSale()
    {
        var ev = MoveTo(EventStatus.OnSale);
        ev.MarkAsSoldOut();
        ev.Status.Should().Be(EventStatus.SoldOut);
    }

    [Theory]
    [InlineData(EventStatus.Cancelled)]
    [InlineData(EventStatus.Completed)]
    public void Cancel_Should_Throw_When_TerminalState(EventStatus from)
    {
        var ev = MoveTo(from);
        Action act = () => ev.Cancel();
        act.Should().Throw<BusinessRuleException>();
    }

    [Theory]
    [InlineData(EventStatus.Draft)]
    [InlineData(EventStatus.OnSale)]
    [InlineData(EventStatus.SoldOut)]
    public void Cancel_Should_Transition(EventStatus from)
    {
        var ev = MoveTo(from);
        ev.Cancel();
        ev.Status.Should().Be(EventStatus.Cancelled);
    }

    [Fact]
    public void Complete_Should_Throw_When_Cancelled()
    {
        var ev = MoveTo(EventStatus.Cancelled);
        Action act = () => ev.Complete();
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Complete_Should_TransitionFromAnyNonCancelled()
    {
        var ev = MoveTo(EventStatus.OnSale);
        ev.Complete();
        ev.Status.Should().Be(EventStatus.Completed);
    }

    [Fact]
    public void AddTicketCategory_Should_Throw_When_NotDraft()
    {
        var ev = MoveTo(EventStatus.OnSale);
        Action act = () => ev.AddTicketCategory("VIP", Price(), 5, 100);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void AddTicketCategory_Should_Throw_When_DuplicateNameCaseInsensitive()
    {
        var ev = new EventBuilder().Build();
        ev.AddTicketCategory("Std", Price(), 5, 100);
        Action act = () => ev.AddTicketCategory("std", Price(), 5, 100);
        act.Should().Throw<BusinessRuleException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddTicketCategory_Should_Throw_When_LocationCapacityNotPositive(int capacity)
    {
        var ev = new EventBuilder().Build();
        Action act = () => ev.AddTicketCategory("Std", Price(), 5, capacity);
        act.Should().Throw<ArgumentException>().WithParameterName("locationCapacity");
    }

    [Fact]
    public void AddTicketCategory_Should_Throw_When_ExceedsLocationCapacity()
    {
        var ev = new EventBuilder().Build();
        ev.AddTicketCategory("Std", Price(), 60, 100);
        Action act = () => ev.AddTicketCategory("VIP", Price(), 60, 100);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void AddTicketCategory_Should_AppendCategory()
    {
        var ev = new EventBuilder().Build();
        var cat = ev.AddTicketCategory("Std", Price(20), 5, 100);
        ev.TicketCategories.Should().ContainSingle().Which.Should().BeSameAs(cat);
        cat.EventId.Should().Be(ev.Id);
        cat.TotalQuantity.Should().Be(5);
    }

    [Fact]
    public void RemoveTicketCategory_Should_Throw_When_NotDraft()
    {
        var ev = new EventBuilder().Build();
        var cat = ev.AddTicketCategory("Std", Price(), 5, 100);
        ev.PutOnSale();
        Action act = () => ev.RemoveTicketCategory(cat.Id);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void RemoveTicketCategory_Should_Throw_When_Unknown()
    {
        var ev = new EventBuilder().Build();
        Action act = () => ev.RemoveTicketCategory(Guid.NewGuid());
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void RemoveTicketCategory_Should_RemoveFromCollection()
    {
        var ev = new EventBuilder().Build();
        var cat = ev.AddTicketCategory("Std", Price(), 5, 100);
        ev.RemoveTicketCategory(cat.Id);
        ev.TicketCategories.Should().BeEmpty();
    }

    private static Event MoveTo(EventStatus status)
    {
        var ev = new EventBuilder().Build();
        switch (status)
        {
            case EventStatus.Draft: return ev;
            case EventStatus.OnSale: ev.PutOnSale(); return ev;
            case EventStatus.SoldOut: ev.PutOnSale(); ev.MarkAsSoldOut(); return ev;
            case EventStatus.Cancelled: ev.Cancel(); return ev;
            case EventStatus.Completed: ev.Complete(); return ev;
            default: throw new ArgumentOutOfRangeException(nameof(status));
        }
    }
}
