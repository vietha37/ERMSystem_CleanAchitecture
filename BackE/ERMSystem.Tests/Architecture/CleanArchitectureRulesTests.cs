using System.Reflection;
using ERMSystem.Domain.Common;
using ERMSystem.Domain.Entities;
using NetArchTest.Rules;
using Xunit;

namespace ERMSystem.Tests.Architecture;

public class CleanArchitectureRulesTests
{
    private static readonly Assembly DomainAssembly = typeof(Patient).Assembly;

    [Fact]
    public void Domain_ShouldNotHaveDependencyOn_Application()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("ERMSystem.Application")
            .GetResult();

        Assert.True(result.IsSuccessful, "Domain layer must not depend on Application layer.");
    }

    [Fact]
    public void Domain_ShouldNotHaveDependencyOn_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("ERMSystem.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, "Domain layer must not depend on Infrastructure layer.");
    }

    [Fact]
    public void Domain_ShouldNotHaveDependencyOn_API()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("ERMSystem.API")
            .GetResult();

        Assert.True(result.IsSuccessful, "Domain layer must not depend on Presentation / API layer.");
    }

    [Fact]
    public void ValueObjects_ShouldInheritFrom_ValueObject()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("ERMSystem.Domain.ValueObjects")
            .Should()
            .Inherit(typeof(ValueObject))
            .GetResult();

        Assert.True(result.IsSuccessful, "All Value Objects in Domain must inherit from ValueObject base class.");
    }

    [Fact]
    public void AggregateRoots_ShouldInheritFrom_AggregateRoot()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .HaveName("Patient")
            .Or()
            .HaveName("Appointment")
            .Or()
            .HaveName("Prescription")
            .Should()
            .Inherit(typeof(AggregateRoot<Guid>))
            .GetResult();

        Assert.True(result.IsSuccessful, "Core aggregates (Patient, Appointment, Prescription) must inherit from AggregateRoot<Guid>.");
    }
}
