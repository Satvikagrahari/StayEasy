using AdminService.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace AdminService.API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        private const string CatalogBaseUrl = "https://localhost:7092";
        private const string BookingBaseUrl = "https://localhost:7071";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AdminController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("dashboard/summary")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var bookings = await FetchBookingsAsync();

            var pendingCount = bookings.Count(x => x.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase));
            var confirmedCount = bookings.Count(x => x.Status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase));
            var cancelledCount = bookings.Count(x => x.Status.Contains("cancel", StringComparison.OrdinalIgnoreCase));
            var totalRevenue = bookings
                .Where(x => x.Status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase))
                .Sum(x => x.TotalAmount);

            var todayUtc = DateTime.UtcNow.Date;
            var todayBookings = bookings.Count(x => x.BookingDate.Date == todayUtc);

            return Ok(new
            {
                totalBookings = bookings.Count,
                pendingBookings = pendingCount,
                confirmedBookings = confirmedCount,
                cancelledBookings = cancelledCount,
                totalRevenue,
                todayBookings
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("dashboard/stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            return await GetDashboardSummary();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("bookings")]
        public async Task<IActionResult> GetAllBookings(
            [FromQuery] string? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var bookings = await FetchBookingsAsync();

            var query = bookings.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;
                query = query.Where(x => x.BookingDate.Date >= start);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date;
                query = query.Where(x => x.BookingDate.Date <= end);
            }

            var result = query
                .OrderByDescending(x => x.BookingDate)
                .ToList();

            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("bookings/recent")]
        public async Task<IActionResult> GetRecentBookings([FromQuery] int take = 10)
        {
            if (take <= 0) take = 10;

            var bookings = await FetchBookingsAsync();

            var result = bookings
                .OrderByDescending(x => x.BookingDate)
                .Take(take)
                .ToList();

            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("bookings/trends")]
        public async Task<IActionResult> GetBookingTrends([FromQuery] int days = 7)
        {
            if (days <= 0) days = 7;

            var from = DateTime.UtcNow.Date.AddDays(-(days - 1));
            var bookings = await FetchBookingsAsync();

            var grouped = bookings
                .Where(x => x.BookingDate.Date >= from)
                .GroupBy(x => x.BookingDate.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var result = Enumerable.Range(0, days)
                .Select(offset => from.AddDays(offset))
                .Select(date => new BookingTrendDto
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    Count = grouped.GetValueOrDefault(date, 0)
                })
                .ToList();

            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("revenue/trends")]
        public async Task<IActionResult> GetRevenueTrends([FromQuery] int months = 12)
        {
            if (months <= 0) months = 12;

            var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var from = currentMonth.AddMonths(-(months - 1));
            var bookings = await FetchBookingsAsync();

            var grouped = bookings
                .Where(x =>
                    x.Status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase) &&
                    x.BookingDate.Date >= from)
                .GroupBy(x => new DateTime(x.BookingDate.Year, x.BookingDate.Month, 1))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalAmount));

            var result = Enumerable.Range(0, months)
                .Select(offset => from.AddMonths(offset))
                .Select(month => new RevenueTrendDto
                {
                    Month = month.ToString("MMM", CultureInfo.InvariantCulture),
                    Revenue = grouped.GetValueOrDefault(month, 0)
                })
                .ToList();

            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("bookings/status-distribution")]
        public async Task<IActionResult> GetBookingStatusDistribution()
        {
            var bookings = await FetchBookingsAsync();

            var grouped = bookings
                .GroupBy(x => x.Status)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var statuses = new[] { "Pending", "Confirmed", "Cancelled", "Failed" };

            var result = statuses
                .Select(status => new BookingStatusDistributionDto
                {
                    Status = status,
                    Count = grouped.GetValueOrDefault(status, 0)
                })
                .ToList();

            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("bookings/{id:guid}/status")]
        public async Task<IActionResult> UpdateBookingStatus(Guid id, [FromQuery] string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return BadRequest("Query parameter 'status' is required.");
            }

            using var downstreamRequest = CreateAuthorizedRequest(
                HttpMethod.Put,
                $"{BookingBaseUrl}/api/booking/{id}/status?status={Uri.EscapeDataString(status)}");

            var response = await _httpClient.SendAsync(downstreamRequest);
            return StatusCode((int)response.StatusCode);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("reports/occupancy")]
        public async Task<IActionResult> GetOccupancyReport([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            var from = (startDate ?? DateTime.UtcNow.Date.AddDays(-30)).Date;
            var to = (endDate ?? DateTime.UtcNow.Date).Date;

            if (to < from)
            {
                return BadRequest("'endDate' must be greater than or equal to 'startDate'.");
            }

            var bookings = await FetchBookingsAsync();

            // Flatten: one occupancy span per BookingItem (each item has its own hotel/room + dates)
            var confirmedItems = bookings
                .Where(x => x.Status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase))
                .SelectMany(x => x.BookingItems)
                .Where(i => i.CheckInDate > DateTime.MinValue && i.CheckOutDate > i.CheckInDate)
                .ToList();

            var report = new List<object>();
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                var occupied = confirmedItems.Count(i => i.CheckInDate.Date <= date && i.CheckOutDate.Date > date);
                report.Add(new
                {
                    date = date.ToString("yyyy-MM-dd"),
                    occupiedBookings = occupied
                });
            }

            return Ok(report);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("reports/revenue")]
        public async Task<IActionResult> GetRevenueReport([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            var from = (startDate ?? DateTime.UtcNow.Date.AddDays(-30)).Date;
            var to = (endDate ?? DateTime.UtcNow.Date).Date;

            if (to < from)
            {
                return BadRequest("'endDate' must be greater than or equal to 'startDate'.");
            }

            var bookings = await FetchBookingsAsync();

            var revenue = bookings
                .Where(x =>
                    x.Status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase) &&
                    x.BookingDate.Date >= from &&
                    x.BookingDate.Date <= to)
                .GroupBy(x => x.BookingDate.Date)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    revenue = g.Sum(x => x.TotalAmount),
                    bookingCount = g.Count()
                })
                .OrderBy(x => x.date)
                .ToList();

            return Ok(revenue);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("reports/cancellations")]
        public async Task<IActionResult> GetCancellationReport([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            var from = (startDate ?? DateTime.UtcNow.Date.AddDays(-30)).Date;
            var to = (endDate ?? DateTime.UtcNow.Date).Date;

            if (to < from)
            {
                return BadRequest("'endDate' must be greater than or equal to 'startDate'.");
            }

            var bookings = await FetchBookingsAsync();

            var cancellations = bookings
                .Where(x =>
                    x.Status.Contains("cancel", StringComparison.OrdinalIgnoreCase) &&
                    x.BookingDate.Date >= from &&
                    x.BookingDate.Date <= to)
                .GroupBy(x => x.BookingDate.Date)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    cancellations = g.Count()
                })
                .OrderBy(x => x.date)
                .ToList();

            return Ok(cancellations);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("bookings/{id:guid}/refund")]
        public async Task<IActionResult> ApproveRefund(Guid id)
        {
            using var downstreamRequest = CreateAuthorizedRequest(
                HttpMethod.Post,
                $"{BookingBaseUrl}/api/booking/{id}/approve-refund");

            var response = await _httpClient.SendAsync(downstreamRequest);
            return StatusCode((int)response.StatusCode);
        }

        private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, url);

            if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                request.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());
            }

            return request;
        }

        private async Task<List<BookingDto>> FetchBookingsAsync()
        {
            using var request = CreateAuthorizedRequest(HttpMethod.Get, $"{BookingBaseUrl}/api/booking/all");
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return new List<BookingDto>();
            }

            return await response.Content.ReadFromJsonAsync<List<BookingDto>>(JsonOptions) ?? new List<BookingDto>();
        }

        
    }
}
