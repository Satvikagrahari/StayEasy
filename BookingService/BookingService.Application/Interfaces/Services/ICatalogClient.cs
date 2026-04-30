using System;
using System.Threading.Tasks;

namespace BookingService.Application.Interfaces.Services
{
    public interface ICatalogClient
    {
        /// <summary>
        /// Fetches the hotel name for a given hotelId from the CatalogService.
        /// Returns null if the hotel is not found or the call fails.
        /// </summary>
        Task<string?> GetHotelNameAsync(Guid hotelId);
    }
}