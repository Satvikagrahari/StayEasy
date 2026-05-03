using CatalogService.Domain.Entities;

namespace CatalogService.Tests;

public class HotelTests
{
    [Fact]
    public void NewHotel_HasExpectedDefaults()
    {
        var hotel = new Hotel();

        Assert.True(hotel.IsActive);
        Assert.Null(hotel.ImageUrls);
        Assert.Null(hotel.RoomTypes);
    }

    [Fact]
    public void Hotel_AllowsSettingIdentityFields()
    {
        var hotelId = Guid.NewGuid();
        var hotel = new Hotel
        {
            HotelId = hotelId,
            Name = "StayEasy Deluxe"
        };

        Assert.Equal(hotelId, hotel.HotelId);
        Assert.Equal("StayEasy Deluxe", hotel.Name);
    }
}
