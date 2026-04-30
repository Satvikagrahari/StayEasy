using BookingService.Application.DTOs.Request;
using BookingService.Application.Interfaces.Services;
using BookingService.Domain.Entities;
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
        [HttpGet("all")]
        public async Task<IActionResult> GetAllBookings()
        {
            var bookings = await _bookingService.GetAllBookingsAsync();
            return Ok(bookings);
        }

        [Authorize]
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
        public async Task<IActionResult> UpdateBookingStatus(Guid bookingId, [FromQuery] BookingStatus status)
        {
            var success = await _bookingService.UpdateBookingStatusAsync(bookingId, status);
            if (!success)
                return NotFound();

            return Ok(new { message = $"Booking status updated to {status}" });
        }

        [HttpPost("{bookingId}/simulate-payment")]
        public async Task<IActionResult> SimulatePayment(Guid bookingId, [FromQuery] bool isSuccess = true)
        {
            await _bookingService.SimulatePaymentAsync(bookingId, isSuccess);
            return Ok(new { message = $"Payment simulated for booking {bookingId}. Success: {isSuccess}" });
        }

        [Authorize]
        [HttpDelete("{id}/cancel")]
        public async Task<IActionResult> CancelBooking(Guid id)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var success = await _bookingService.CancelBookingAsync(id, userId);
            if (!success)
                return NotFound(new { message = "Booking not found or not owned by user." });

            return Ok(new { message = "Booking cancelled successfully." });
        }

        [Authorize]
        [HttpPost("{id}/request-refund")]
        public async Task<IActionResult> RequestRefund(Guid id)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            try
            {
                var success = await _bookingService.RequestRefundAsync(id, userId);
                if (!success)
                    return BadRequest(new { message = "Refund request failed. Ensure booking is cancelled." });

                return Ok(new { message = "Refund requested successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/approve-refund")]
        public async Task<IActionResult> ApproveRefund(Guid id)
        {
            var success = await _bookingService.ApproveRefundAsync(id);
            if (!success)
                return NotFound(new { message = "Booking not found or not in refund requested status." });

            return Ok(new { message = "Refund approved and processed." });
        }
    }
}
