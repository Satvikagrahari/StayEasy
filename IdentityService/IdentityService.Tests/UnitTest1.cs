using IdentityService.Domain.Entities;

namespace IdentityService.Tests;

public class UserTests
{
    [Test]
    public void NewUser_HasExpectedDefaults()
    {
        var user = new User();

        Assert.That(user.Role, Is.EqualTo("Guest"));
        Assert.That(user.IsActive, Is.True);
        Assert.That(user.IsVerified, Is.False);
    }

    [Test]
    public void User_AllowsSettingCoreFields()
    {
        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id,
            UserName = "satvik",
            Email = "satvik@example.com"
        };

        Assert.That(user.Id, Is.EqualTo(id));
        Assert.That(user.UserName, Is.EqualTo("satvik"));
        Assert.That(user.Email, Is.EqualTo("satvik@example.com"));
    }
}


