using IdentityService.Domain.Entities;

namespace IdentityService.Tests;

public class UserTests
{
    [Fact]
    public void NewUser_HasExpectedDefaults()
    {
        var user = new User();

        Assert.Equal("Guest", user.Role);
        Assert.True(user.IsActive);
        Assert.False(user.IsVerified);
    }

    [Fact]
    public void User_AllowsSettingCoreFields()
    {
        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id,
            UserName = "satvik",
            Email = "satvik@example.com"
        };

        Assert.Equal(id, user.Id);
        Assert.Equal("satvik", user.UserName);
        Assert.Equal("satvik@example.com", user.Email);
    }
}
