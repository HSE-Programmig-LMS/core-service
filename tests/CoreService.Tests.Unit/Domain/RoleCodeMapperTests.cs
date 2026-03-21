using CoreService.Domain.Security;
using Xunit;

namespace CoreService.Tests.Unit.Domain;

public sealed class RoleCodeMapperTests
{
    [Fact]
    public void ToDb_FromDb_ShouldRoundTrip_ForAllRoleCodes()
    {
        foreach (var code in RoleCodeMapper.All)
        {
            var db = RoleCodeMapper.ToDb(code);
            var back = RoleCodeMapper.FromDb(db);

            Assert.Equal(code, back);
        }
    }

    [Fact]
    public void ToDisplay_ShouldReturnNonEmpty_ForAllRoleCodes()
    {
        foreach (var code in RoleCodeMapper.All)
        {
            var display = RoleCodeMapper.ToDisplay(code);
            Assert.False(string.IsNullOrWhiteSpace(display));
        }
    }

    [Fact]
    public void FromDb_WhenUnknown_ShouldThrow()
    {
        Assert.ThrowsAny<Exception>(() => RoleCodeMapper.FromDb("unknown_role_code"));
    }
}