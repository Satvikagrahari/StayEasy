using AdminService.Application.DTOs;

namespace AdminService.Tests;

public class BookingDtoTests
{
    [Test]
    public void NewBookingDto_HasExpectedDefaults()
    {
        var dto = new BookingDto();

        Assert.That(dto.Status, Is.EqualTo(string.Empty));
        Assert.That(dto.BookingItems, Is.Not.Null);
        Assert.That(dto.BookingItems, Is.Empty);
    }

    [Test]
    public void BookingTrendDto_AllowsSettingValues()
    {
        var trend = new BookingTrendDto
        {
            Date = "2026-05-01",
            Count = 7
        };

        Assert.That(trend.Date, Is.EqualTo("2026-05-01"));
        Assert.That(trend.Count, Is.EqualTo(7));
    }
}


