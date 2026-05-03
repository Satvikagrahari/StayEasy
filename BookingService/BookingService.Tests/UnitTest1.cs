using BookingService.Domain.Entities;

namespace BookingService.Tests;

public class CartTests
{
    [Fact]
    public void NewCart_HasExpectedDefaults()
    {
        var before = DateTime.UtcNow;
        var cart = new Cart();
        var after = DateTime.UtcNow;

        Assert.Equal("Active", cart.Status);
        Assert.InRange(cart.CreatedAt, before, after);
        Assert.Null(cart.Items);
    }

    [Fact]
    public void Cart_AllowsSettingUserId()
    {
        var userId = Guid.NewGuid();
        var cart = new Cart { UserId = userId };

        Assert.Equal(userId, cart.UserId);
    }
}
