using System.Security.Claims;
using BookingService.API.Controllers;
using BookingService.Application.DTOs.Request;
using BookingService.Application.Interfaces.Services;
using BookingService.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BookingService.Tests;

public class BookingControllerEndpointTests
{
    private static BookingController BuildController(Mock<IBookingService> bookingService, Guid userId)
    {
        var controller = new BookingController(bookingService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    ], "test"))
                }
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetAllBookings_ReturnsOk()
    {
        var service = new Mock<IBookingService>();
        service.Setup(x => x.GetAllBookingsAsync()).ReturnsAsync(new List<Booking>());
        var controller = BuildController(service, Guid.NewGuid());

        var result = await controller.GetAllBookings();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetMyBookings_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var service = new Mock<IBookingService>();
        service.Setup(x => x.GetUserBookingsAsync(userId)).ReturnsAsync(new List<Booking>());
        var controller = BuildController(service, userId);

        var result = await controller.GetMyBookings();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Checkout_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var service = new Mock<IBookingService>();
        service.Setup(x => x.CheckoutAsync(userId, "SALE10")).ReturnsAsync(bookingId);
        var controller = BuildController(service, userId);

        var result = await controller.Checkout("SALE10");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetPendingBookings_ReturnsOk()
    {
        var service = new Mock<IBookingService>();
        service.Setup(x => x.GetPendingBookingsAsync()).ReturnsAsync(new List<Booking>());
        var controller = BuildController(service, Guid.NewGuid());

        var result = await controller.GetPendingBookings();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetConfirmedBookings_ReturnsOk()
    {
        var service = new Mock<IBookingService>();
        service.Setup(x => x.GetConfirmedBookingsAsync()).ReturnsAsync(new List<Booking>());
        var controller = BuildController(service, Guid.NewGuid());

        var result = await controller.GetConfirmedBookings();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateBookingStatus_WhenMissing_ReturnsNotFound()
    {
        var service = new Mock<IBookingService>();
        service.Setup(x => x.UpdateBookingStatusAsync(It.IsAny<Guid>(), BookingStatus.Confirmed)).ReturnsAsync(false);
        var controller = BuildController(service, Guid.NewGuid());

        var result = await controller.UpdateBookingStatus(Guid.NewGuid(), BookingStatus.Confirmed);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreateOrder_WhenServiceThrows_ReturnsBadRequest()
    {
        var service = new Mock<IBookingService>();
        service.Setup(x => x.CreateRazorpayOrderAsync(It.IsAny<Guid>())).ThrowsAsync(new Exception("failed"));
        var controller = BuildController(service, Guid.NewGuid());

        var result = await controller.CreateOrder(Guid.NewGuid());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task VerifyPayment_WhenInvalid_ReturnsBadRequest()
    {
        var service = new Mock<IBookingService>();
        service.Setup(x => x.VerifyRazorpayPaymentAsync(It.IsAny<RazorpayPaymentVerificationRequest>())).ReturnsAsync(false);
        var controller = BuildController(service, Guid.NewGuid());

        var result = await controller.VerifyPayment(new RazorpayPaymentVerificationRequest());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CancelBooking_WhenMissing_ReturnsNotFound()
    {
        var service = new Mock<IBookingService>();
        service.Setup(x => x.CancelBookingAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(false);
        var controller = BuildController(service, Guid.NewGuid());

        var result = await controller.CancelBooking(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task RequestRefund_WhenInvalid_ReturnsBadRequest()
    {
        var service = new Mock<IBookingService>();
        service.Setup(x => x.RequestRefundAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(false);
        var controller = BuildController(service, Guid.NewGuid());

        var result = await controller.RequestRefund(Guid.NewGuid());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ApproveRefund_WhenMissing_ReturnsNotFound()
    {
        var service = new Mock<IBookingService>();
        service.Setup(x => x.ApproveRefundAsync(It.IsAny<Guid>())).ReturnsAsync(false);
        var controller = BuildController(service, Guid.NewGuid());

        var result = await controller.ApproveRefund(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetInvoice_WhenMissing_ReturnsNotFound()
    {
        var service = new Mock<IBookingService>();
        service.Setup(x => x.GetInvoiceAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((byte[]?)null);
        var controller = BuildController(service, Guid.NewGuid());

        var result = await controller.GetInvoice(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetAdminReport_ReturnsFile()
    {
        var service = new Mock<IBookingService>();
        service.Setup(x => x.GetAdminReportAsync()).ReturnsAsync([1, 2, 3]);
        var controller = BuildController(service, Guid.NewGuid());

        var result = await controller.GetAdminReport();

        Assert.IsType<FileContentResult>(result);
    }
}

public class CartControllerEndpointTests
{
    private static CartController BuildController(Mock<ICartService> cartService, Guid userId)
    {
        var controller = new CartController(cartService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    ], "test"))
                }
            }
        };
        return controller;
    }

    [Fact]
    public async Task AddToCart_ReturnsOk()
    {
        var cartService = new Mock<ICartService>();
        var controller = BuildController(cartService, Guid.NewGuid());

        var result = await controller.AddToCart(new CreateCartItemRequest());

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetCart_ReturnsOk()
    {
        var cartService = new Mock<ICartService>();
        cartService.Setup(x => x.GetCartAsync(It.IsAny<Guid>())).ReturnsAsync(new Cart());
        var controller = BuildController(cartService, Guid.NewGuid());

        var result = await controller.GetCart();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Remove_ReturnsOk()
    {
        var cartService = new Mock<ICartService>();
        var controller = BuildController(cartService, Guid.NewGuid());

        var result = await controller.Remove(Guid.NewGuid());

        Assert.IsType<OkObjectResult>(result);
    }
}
