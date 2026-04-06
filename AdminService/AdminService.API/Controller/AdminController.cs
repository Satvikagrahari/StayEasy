using AdminService.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AdminService.API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public AdminController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost("hotels")]
        public async Task<IActionResult> CreateHotel([FromBody] CreateHotelRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "https://localhost:7092/api/hotels", request);

            return StatusCode((int)response.StatusCode);
        }
    }
}
