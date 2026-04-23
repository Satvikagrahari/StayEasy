using IdentityService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.API.Controller
{
    /// <summary>
    /// Internal endpoints — only called by other services within the same network.
    /// NOT exposed through the API Gateway.
    /// </summary>
    [ApiController]
    [Route("api/internal")]
    public class InternalController : ControllerBase
    {
        private readonly IAuthService _authService;

        public InternalController(IAuthService authService)
        {
            _authService = authService;
        }

        // GET api/internal/users/{userId}/email
        [HttpGet("users/{userId:guid}/email")]
        public async Task<IActionResult> GetUserEmail(Guid userId)
        {
            var profile = await _authService.GetProfileAsync(userId);
            if (profile == null)
                return NotFound();

            return Ok(new { Email = profile.Email });
        }
    }
}
