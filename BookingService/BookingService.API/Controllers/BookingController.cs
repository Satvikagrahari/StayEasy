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
        public async Task<IActionResult> Checkout([FromQuery] string? promoCode = null)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var bookingId = await _bookingService.CheckoutAsync(userId, promoCode);

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

        [Authorize]
        [HttpPost("{bookingId}/razorpay-order")]
        public async Task<IActionResult> CreateOrder(Guid bookingId)
        {
            try
            {
                var orderId = await _bookingService.CreateRazorpayOrderAsync(bookingId);
                return Ok(new { orderId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("razorpay-verify")]
        public async Task<IActionResult> VerifyPayment([FromBody] RazorpayPaymentVerificationRequest request)
        {
            var success = await _bookingService.VerifyRazorpayPaymentAsync(request);
            if (!success)
                return BadRequest(new { message = "Payment verification failed." });

            return Ok(new { message = "Payment verified successfully." });
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

        [Authorize]
        [HttpGet("{id}/invoice")]
        public async Task<IActionResult> GetInvoice(Guid id)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var pdfBytes = await _bookingService.GetInvoiceAsync(id, userId);
            if (pdfBytes == null)
                return NotFound(new { message = "Booking not found or not owned by user." });

            return File(pdfBytes, "application/pdf", $"Invoice_{id.ToString().Substring(0, 8)}.pdf");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/report")]
        public async Task<IActionResult> GetAdminReport()
        {
            var pdfBytes = await _bookingService.GetAdminReportAsync();
            return File(pdfBytes, "application/pdf", $"StayEasy_Business_Report_{DateTime.Now:yyyyMMdd}.pdf");
        }
    }
}
