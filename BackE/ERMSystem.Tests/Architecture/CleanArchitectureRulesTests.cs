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
        var valueObjectTypes = DomainAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace == "ERMSystem.Domain.ValueObjects" && !t.IsNested);

        foreach (var type in valueObjectTypes)
        {
            Assert.True(
                typeof(ValueObject).IsAssignableFrom(type),
                $"{type.Name} must inherit from ValueObject base class.");
        }
    }

    [Fact]
    public void AggregateRoots_ShouldInheritFrom_AggregateRoot()
    {
        var aggregateTypes = new[] { typeof(Patient), typeof(Appointment), typeof(Prescription) };
        foreach (var type in aggregateTypes)
        {
            var isAggregate = type.BaseType != null &&
                              type.BaseType.IsGenericType &&
                              type.BaseType.GetGenericTypeDefinition() == typeof(AggregateRoot<>);
            Assert.True(isAggregate, $"{type.Name} must inherit from AggregateRoot<Guid>.");
        }
    }
}
