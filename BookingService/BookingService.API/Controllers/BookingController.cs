using BookingService.Application.DTOs.Request;
using BookingService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookingService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        //[Authorize]
        //[HttpPost]
        //public async Task<IActionResult> Create(CreateBookingRequest request)
        //{
        //    var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        //    await _bookingService.CreateBookingAsync(userId, request);

        //    return StatusCode(201);
        //}

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetMyBookings()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var bookings = await _bookingService.GetUserBookingsAsync(userId);

            return Ok(bookings);
        }

        [Authorize]
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var bookingId = await _bookingService.CheckoutAsync(userId);

            return Ok(new { bookingId });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingBookings()
        {
            var bookings = await _bookingService.GetPendingBookingsAsync();
            return Ok(bookings);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("confirmed")]
        public async Task<IActionResult> GetConfirmedBookings()
        {
            var bookings = await _bookingService.GetConfirmedBookingsAsync();
            return Ok(bookings);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{bookingId}/status")]
        public async Task<IActionResult> UpdateBookingStatus(Guid bookingId, [FromQuery] string status)
        {
            var success = await _bookingService.UpdateBookingStatusAsync(bookingId, status);
            if (!success)
                return NotFound();

            return Ok(new { message = $"Booking status updated to {status}" });
        }
    }
}
