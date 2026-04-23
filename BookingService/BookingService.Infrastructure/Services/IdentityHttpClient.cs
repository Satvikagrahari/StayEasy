using BookingService.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BookingService.Infrastructure.Services
{
    public class IdentityHttpClient : IIdentityClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IdentityHttpClient> _logger;

        public IdentityHttpClient(HttpClient httpClient, IConfiguration config, ILogger<IdentityHttpClient> logger)
        {
            _logger = logger;
            var baseUrl = config["IdentityService:BaseUrl"]
                ?? throw new InvalidOperationException("IdentityService:BaseUrl is not configured.");
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        }

        public async Task<string?> GetUserEmailAsync(Guid userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/internal/users/{userId}/email");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("IdentityService returned {StatusCode} for userId {UserId}.",
                        response.StatusCode, userId);
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<EmailResponse>();
                return result?.Email;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch email for userId {UserId} from IdentityService.", userId);
                return null;
            }
        }

        private sealed record EmailResponse(string Email);
    }
}
