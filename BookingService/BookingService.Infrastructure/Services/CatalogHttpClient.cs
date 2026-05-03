using BookingService.Application.Interfaces.Services;
using BookingService.Application.DTOs.Request;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BookingService.Infrastructure.Services
{
    public class CatalogHttpClient : ICatalogClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CatalogHttpClient> _logger;

        public CatalogHttpClient(HttpClient httpClient, IConfiguration config, ILogger<CatalogHttpClient> logger)
        {
            _logger = logger;

            var baseUrl = config["CatalogService:BaseUrl"]
                ?? throw new InvalidOperationException("CatalogService:BaseUrl is not configured.");

            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        }

        public async Task<string?> GetHotelNameAsync(Guid hotelId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/hotels/{hotelId}");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CatalogService returned {StatusCode} for hotelId {HotelId}.", response.StatusCode, hotelId);
                    return null;
                }

                var hotel = await response.Content.ReadFromJsonAsync<HotelResponse>();
                return string.IsNullOrWhiteSpace(hotel?.Name) ? null : hotel.Name;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch hotel name for hotelId {HotelId} from CatalogService.", hotelId);
                return null;
            }
        }

        public async Task<RoomTypeDto?> GetRoomTypeAsync(Guid roomTypeId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/hotels/rooms/{roomTypeId}");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("CatalogService returned {StatusCode} for roomTypeId {RoomTypeId}.", response.StatusCode, roomTypeId);
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<RoomTypeDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch room type for roomTypeId {RoomTypeId} from CatalogService.", roomTypeId);
                return null;
            }
        }

        private sealed class HotelResponse
        {
            public Guid HotelId { get; set; }
            public string? Name { get; set; }
        }
    }
}