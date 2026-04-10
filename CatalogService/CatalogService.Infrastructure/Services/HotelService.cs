using CatalogService.Application.DTOs.Request;
using CatalogService.Application.DTOs.Response;
using CatalogService.Application.Interfaces.Services;
using CatalogService.Domain.Entities;
using CatalogService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
                HotelId = Guid.NewGuid(),
                Name = request.Name,
                City = request.City,
                Country = request.Country,
                Address = request.Address,
                Description = request.Description,
                StarRating = request.StarRating
            };

            await _context.Hotels.AddAsync(hotel);
            await _context.SaveChangesAsync();
        }

        //public async Task<List<Hotel>> GetAllHotelsAsync()
        //{
        //    return await _context.Hotels
        //        .AsNoTracking()
        //        .ToListAsync();
        //}
        public async Task<List<HotelResponseDto>> GetHotelsAsync(string? roomType)
        {
            var query = _context.Hotels
                .Include(h => h.RoomTypes)
                .AsQueryable();

            if (!string.IsNullOrEmpty(roomType))
            {
                query = query.Where(h =>
                    h.RoomTypes.Any(r => r.Type == roomType));
            }

            var hotels = await query.ToListAsync();

            return hotels.Select(h => new HotelResponseDto
            {
                HotelId = h.HotelId,
                Name = h.Name,
                City = h.City,

                RoomTypes = h.RoomTypes.Select(r => new RoomTypeDto
                {
                    RoomTypeId = r.RoomTypeId,
                    Name = r.Type,
                    PricePerNight = r.PricePerNight
                }).ToList()
            }).ToList();
        }

        public async Task AddRoomTypeAsync(CreateRoomTypeRequest request)
        {
            var room = new RoomType
            {
                RoomTypeId = Guid.NewGuid(),
                HotelId = request.HotelId,
                Type = request.Type,
                Description = request.Description,
                MaxGuests = request.MaxGuests,
                PricePerNight = request.PricePerNight,
                TotalRooms = request.TotalRooms
            };

            await _context.RoomTypes.AddAsync(room);
            await _context.SaveChangesAsync();
        }


        public async Task<Hotel> GetHotelByIdAsync(Guid id)
        {
            var hotel = await _context.Hotels.FindAsync(id);

            if (hotel == null)
                throw new ApplicationException("Hotel not found");

            return hotel;
        }

        public async Task<RoomTypeDto> GetRoomByIdAsync(Guid roomTypeId)
        {
            var room = await _context.RoomTypes
                .FirstOrDefaultAsync(r => r.RoomTypeId == roomTypeId);

            if (room == null)
                throw new Exception("Room not found");

            return new RoomTypeDto
            {
                RoomTypeId = room.RoomTypeId,
                Name = room.Type, // or Name depending on your entity
                PricePerNight = room.PricePerNight
            };
        }
    }
}


