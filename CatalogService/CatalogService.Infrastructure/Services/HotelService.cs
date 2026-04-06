using CatalogService.Application.Interfaces.Services;
using CatalogService.Application.DTOs.Request;
using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using CatalogService.Infrastructure.Data;

namespace CatalogService.Application.Services
{

    public class HotelService : IHotelService
    {
        private readonly CatalogDbContext _context;

        public HotelService(CatalogDbContext context)
        {
            _context = context;
        }

        public async Task CreateHotelAsync(CreateHotelRequest request)
        {
            var hotel = new Hotel
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                City = request.City,
                Address = request.Address,
                PricePerNight = request.PricePerNight
            };

            await _context.Hotels.AddAsync(hotel);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Hotel>> GetAllHotelsAsync()
        {
            return await _context.Hotels
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Hotel> GetHotelByIdAsync(Guid id)
        {
            var hotel = await _context.Hotels.FindAsync(id);

            if (hotel == null)
                throw new ApplicationException("Hotel not found");

            return hotel;
        }
    }
}


