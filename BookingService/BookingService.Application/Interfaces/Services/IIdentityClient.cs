using System;
using System.Threading.Tasks;

namespace BookingService.Application.Interfaces.Services
{
    public interface IIdentityClient
    {
        /// <summary>
        /// Fetches the email address for a given userId from the IdentityService.
        /// Returns null if the user is not found or the call fails.
        /// </summary>
        Task<string?> GetUserEmailAsync(Guid userId);
    }
}
