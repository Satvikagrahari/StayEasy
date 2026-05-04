using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using AdminService.API.Controller;
using AdminService.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AdminService.Tests;

public class AdminControllerEndpointTests
{
    private const string BookingAllUrl = "https://localhost:7071/api/booking/all";

    [Test]
    public async Task GetDashboardSummary_ReturnsOk()
    {
        var controller = BuildControllerWithBookings(SampleBookings());

        var result = await controller.GetDashboardSummary();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetDashboardStats_ReturnsOk()
    {
        var controller = BuildControllerWithBookings(SampleBookings());

        var result = await controller.GetDashboardStats();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetAllBookings_WithFilter_ReturnsOk()
    {
        var controller = BuildControllerWithBookings(SampleBookings());

        var result = await controller.GetAllBookings(status: "Confirmed");

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetRecentBookings_ReturnsOk()
    {
        var controller = BuildControllerWithBookings(SampleBookings());

        var result = await controller.GetRecentBookings(5);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetBookingTrends_ReturnsOk()
    {
        var controller = BuildControllerWithBookings(SampleBookings());

        var result = await controller.GetBookingTrends(7);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetRevenueTrends_ReturnsOk()
    {
        var controller = BuildControllerWithBookings(SampleBookings());

        var result = await controller.GetRevenueTrends(6);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetBookingStatusDistribution_ReturnsOk()
    {
        var controller = BuildControllerWithBookings(SampleBookings());

        var result = await controller.GetBookingStatusDistribution();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task UpdateBookingStatus_WhenStatusMissing_ReturnsBadRequest()
    {
        var controller = BuildControllerWithBookings(SampleBookings());

        var result = await controller.UpdateBookingStatus(Guid.NewGuid(), string.Empty);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetOccupancyReport_InvalidDates_ReturnsBadRequest()
    {
        var controller = BuildControllerWithBookings(SampleBookings());

        var result = await controller.GetOccupancyReport(DateTime.UtcNow, DateTime.UtcNow.AddDays(-1));

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetRevenueReport_ReturnsOk()
    {
        var controller = BuildControllerWithBookings(SampleBookings());

        var result = await controller.GetRevenueReport();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetCancellationReport_ReturnsOk()
    {
        var controller = BuildControllerWithBookings(SampleBookings());

        var result = await controller.GetCancellationReport();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task ApproveRefund_ForwardsStatusCode()
    {
        var handler = new StubHttpMessageHandler(req =>
        {
            if (req.Method == HttpMethod.Post && req.RequestUri!.ToString().Contains("/approve-refund", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            return BookingListResponse(SampleBookings());
        });

        var controller = BuildController(handler);

        var result = await controller.ApproveRefund(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<StatusCodeResult>());
        var status = (StatusCodeResult)result;
        Assert.That(status.StatusCode, Is.EqualTo(StatusCodes.Status204NoContent));
    }

    private static AdminController BuildControllerWithBookings(List<BookingDto> bookings)
    {
        var handler = new StubHttpMessageHandler(req =>
        {
            if (req.RequestUri!.ToString().Equals(BookingAllUrl, StringComparison.OrdinalIgnoreCase))
            {
                return BookingListResponse(bookings);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        return BuildController(handler);
    }

    private static AdminController BuildController(HttpMessageHandler handler)
    {
        var controller = new AdminController(new HttpClient(handler))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        controller.Request.Headers["Authorization"] = "Bearer fake";
        return controller;
    }

    private static HttpResponseMessage BookingListResponse(List<BookingDto> bookings)
    {
        var json = JsonSerializer.Serialize(bookings);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static List<BookingDto> SampleBookings()
    {
        var today = DateTime.UtcNow.Date;
        return
        [
            new BookingDto
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Status = "Confirmed",
                TotalAmount = 1500,
                BookingDate = today,
                BookingItems =
                [
                    new BookingItemDto
                    {
                        BookingItemId = Guid.NewGuid(),
                        HotelId = Guid.NewGuid(),
                        RoomTypeId = Guid.NewGuid(),
                        CheckInDate = today,
                        CheckOutDate = today.AddDays(2)
                    }
                ]
            },
            new BookingDto
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Status = "Cancelled",
                TotalAmount = 800,
                BookingDate = today.AddDays(-1)
            }
        ];
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responder(request));
        }
    }
}


