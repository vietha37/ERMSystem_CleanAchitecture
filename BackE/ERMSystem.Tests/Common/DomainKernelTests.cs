using System;
using System.Collections.Generic;
using ERMSystem.Domain.Common;
using Xunit;

namespace ERMSystem.Tests.Common;

public class DomainKernelTests
{
    private record TestDomainEvent(string Message) : IDomainEvent;

    private class SampleEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public SampleEntity() : base() { }
        public SampleEntity(Guid id) : base(id) { }
    }

    private class SampleAggregate : AggregateRoot
    {
        public string Title { get; set; } = string.Empty;

        public void DoSomething(string msg)
        {
            RaiseDomainEvent(new TestDomainEvent(msg));
        }
    }

    private class AddressVo : ValueObject
    {
        public string Street { get; }
        public string City { get; }

        public AddressVo(string street, string city)
        {
            Street = street;
            City = city;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
        }
    }

    [Fact]
    public void BaseEntity_WhenSameId_AreEqual()
    {
        var id = Guid.NewGuid();
        var entity1 = new SampleEntity(id) { Name = "A" };
        var entity2 = new SampleEntity(id) { Name = "B" };

        Assert.Equal(entity1, entity2);
        Assert.True(entity1 == entity2);
        Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
    }

    [Fact]
    public void BaseEntity_WhenDifferentId_AreNotEqual()
    {
        var entity1 = new SampleEntity(Guid.NewGuid());
        var entity2 = new SampleEntity(Guid.NewGuid());

        Assert.NotEqual(entity1, entity2);
        Assert.True(entity1 != entity2);
    }

    [Fact]
    public void BaseEntity_AuditAndSoftDelete_WorksCorrectly()
    {
        var entity = new SampleEntity();
        Assert.False(entity.IsDeleted);

        entity.SetAuditCreated("admin");
        Assert.Equal("admin", entity.CreatedBy);

        entity.SoftDelete("admin");
        Assert.True(entity.IsDeleted);
        Assert.NotNull(entity.DeletedAtUtc);

        entity.Restore("admin");
        Assert.False(entity.IsDeleted);
        Assert.Null(entity.DeletedAtUtc);
    }

    [Fact]
    public void ValueObject_WithIdenticalValues_AreEqual()
    {
        var addr1 = new AddressVo("123 Main St", "Hanoi");
        var addr2 = new AddressVo("123 Main St", "Hanoi");

        Assert.Equal(addr1, addr2);
        Assert.True(addr1 == addr2);
        Assert.Equal(addr1.GetHashCode(), addr2.GetHashCode());
    }

    [Fact]
    public void ValueObject_WithDifferentValues_AreNotEqual()
    {
        var addr1 = new AddressVo("123 Main St", "Hanoi");
        var addr2 = new AddressVo("456 Other St", "Hanoi");

        Assert.NotEqual(addr1, addr2);
        Assert.True(addr1 != addr2);
    }

    [Fact]
    public void AggregateRoot_DomainEvents_CanBeRaisedAndCleared()
    {
        var aggregate = new SampleAggregate();
        Assert.Empty(aggregate.DomainEvents);

        aggregate.DoSomething("Action 1");
        aggregate.DoSomething("Action 2");

        Assert.Equal(2, aggregate.DomainEvents.Count);

        aggregate.ClearDomainEvents();
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void Result_SuccessAndFailure_BehaveCorrectly()
    {
        var success = Result.Success("OK");
        Assert.True(success.IsSuccess);
        Assert.False(success.IsFailure);
        Assert.Equal("OK", success.Value);

        var failure = Result<string>.Failure("Error message");
        Assert.True(failure.IsFailure);
        Assert.False(failure.IsSuccess);
        Assert.Equal("Error message", failure.Error);
        Assert.Throws<InvalidOperationException>(() => failure.Value);
    }
}
