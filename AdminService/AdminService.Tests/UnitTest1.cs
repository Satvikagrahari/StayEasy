using AdminService.Application.DTOs;

namespace AdminService.Tests;

public class BookingDtoTests
{
    [Fact]
    public void NewBookingDto_HasExpectedDefaults()
    {
        var dto = new BookingDto();

        Assert.Equal(string.Empty, dto.Status);
        Assert.NotNull(dto.BookingItems);
        Assert.Empty(dto.BookingItems);
    }

    [Fact]
    public void BookingTrendDto_AllowsSettingValues()
    {
        var trend = new BookingTrendDto
        {
            Date = "2026-05-01",
            Count = 7
        };

        Assert.Equal("2026-05-01", trend.Date);
        Assert.Equal(7, trend.Count);
    }
}
