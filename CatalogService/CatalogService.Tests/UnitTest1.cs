using CatalogService.Domain.Entities;

namespace CatalogService.Tests;

public class HotelTests
{
    [Test]
    public void NewHotel_HasExpectedDefaults()
    {
        var hotel = new Hotel();

        Assert.That(hotel.IsActive, Is.True);
        Assert.That(hotel.ImageUrls, Is.Null);
        Assert.That(hotel.RoomTypes, Is.Null);
    }

    [Test]
    public void Hotel_AllowsSettingIdentityFields()
    {
        var hotelId = Guid.NewGuid();
        var hotel = new Hotel
        {
            HotelId = hotelId,
            Name = "StayEasy Deluxe"
        };

        Assert.That(hotel.HotelId, Is.EqualTo(hotelId));
        Assert.That(hotel.Name, Is.EqualTo("StayEasy Deluxe"));
    }
}


