using BookingService.Domain.Entities;

namespace BookingService.Tests;

public class CartTests
{
    [Test]
    public void NewCart_HasExpectedDefaults()
    {
        var before = DateTime.UtcNow;
        var cart = new Cart();
        var after = DateTime.UtcNow;

        Assert.That(cart.Status, Is.EqualTo("Active"));
        Assert.That(cart.CreatedAt, Is.InRange(before, after));
        Assert.That(cart.Items, Is.Null);
    }

    [Test]
    public void Cart_AllowsSettingUserId()
    {
        var userId = Guid.NewGuid();
        var cart = new Cart { UserId = userId };

        Assert.That(cart.UserId, Is.EqualTo(userId));
    }
}


