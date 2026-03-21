using System.Reflection;
using CoreService.Domain.Audit;
using CoreService.Domain.Auth;
using Xunit;

namespace CoreService.Tests.Unit.Domain;

public sealed class DomainStringConstantsTests
{
    [Theory]
    [InlineData(typeof(JwtClaimNames))]
    [InlineData(typeof(AuditEventTypes))]
    [InlineData(typeof(AuditEntityTypes))]
    public void ConstStrings_ShouldBeNonEmpty_AndUnique(Type type)
    {
        var values = type
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        Assert.NotEmpty(values);
        Assert.All(values, v => Assert.False(string.IsNullOrWhiteSpace(v)));

        var distinct = values.Distinct(StringComparer.Ordinal).Count();
        Assert.Equal(values.Count, distinct);
    }
}