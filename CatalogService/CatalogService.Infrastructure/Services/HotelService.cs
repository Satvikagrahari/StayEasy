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
                StarRating = request.StarRating,
                ImageUrls = request.ImageUrls?.Where(url => !string.IsNullOrWhiteSpace(url)).ToList() ?? new List<string>(),
                IsActive = true
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
                .Where(h => h.IsActive)
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
                Country = h.Country,
                Address = h.Address,
                Description = h.Description,
                StarRating = h.StarRating,
                ImageUrls = h.ImageUrls ?? new List<string>(),

                RoomTypes = h.RoomTypes.Select(r => new RoomTypeDto
                {
                    RoomTypeId = r.RoomTypeId,
                    Name = r.Type,
                    Description = r.Description,
                    MaxGuests = r.MaxGuests,
                    PricePerNight = r.PricePerNight,
                    TotalRooms = r.TotalRooms,
                    AvailableRooms = r.AvailableRooms,
                    Status = r.Status
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
                TotalRooms = request.TotalRooms,
                AvailableRooms = request.AvailableRooms ?? request.TotalRooms
            };

            await _context.RoomTypes.AddAsync(room);
            await _context.SaveChangesAsync();
        }


        public async Task<HotelResponseDto> GetHotelByIdAsync(Guid id)
        {
            var hotel = await _context.Hotels
                .Include(h => h.RoomTypes)
                .FirstOrDefaultAsync(h => h.HotelId == id && h.IsActive);

            if (hotel == null)
                throw new ApplicationException("Hotel not found");

            return new HotelResponseDto
            {
                HotelId = hotel.HotelId,
                Name = hotel.Name,
                City = hotel.City,
                Country = hotel.Country,
                Address = hotel.Address,
                Description = hotel.Description,
                StarRating = hotel.StarRating,
                ImageUrls = hotel.ImageUrls ?? new List<string>(),
                RoomTypes = hotel.RoomTypes.Select(r => new RoomTypeDto
                {
                    RoomTypeId = r.RoomTypeId,
                    Name = r.Type,
                    Description = r.Description,
                    MaxGuests = r.MaxGuests,
                    PricePerNight = r.PricePerNight,
                    TotalRooms = r.TotalRooms,
                    AvailableRooms = r.AvailableRooms,
                    Status = r.Status
                }).ToList()
            };
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
                Description = room.Description,
                MaxGuests = room.MaxGuests,
                PricePerNight = room.PricePerNight,
                TotalRooms = room.TotalRooms,
                AvailableRooms = room.AvailableRooms,
                Status = room.Status
            };
        }

        public async Task<bool> UpdateHotelAsync(Guid id, UpdateHotelRequest request)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.HotelId == id && h.IsActive);
            if (hotel == null)
                return false;

            hotel.Name = request.Name;
            hotel.City = request.City;
            hotel.Country = request.Country;
            hotel.Address = request.Address;
            hotel.Description = request.Description;
            hotel.StarRating = request.StarRating;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateHotelAsync(Guid id)
        {
            var hotel = await _context.Hotels
                .Include(h => h.RoomTypes)
                .FirstOrDefaultAsync(h => h.HotelId == id && h.IsActive);

            if (hotel == null)
                return false;

            hotel.IsActive = false;

            foreach (var room in hotel.RoomTypes)
            {
                room.Status = "Inactive";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateRoomTypeAsync(Guid roomTypeId, UpdateRoomTypeRequest request)
        {
            var room = await _context.RoomTypes
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r => r.RoomTypeId == roomTypeId && r.Hotel.IsActive);

            if (room == null)
                return false;

            room.Type = request.Type;
            room.Description = request.Description;
            room.MaxGuests = request.MaxGuests;
            room.PricePerNight = request.PricePerNight;
            room.TotalRooms = request.TotalRooms;
            
            if (request.AvailableRooms.HasValue)
            {
                room.AvailableRooms = request.AvailableRooms.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                room.Status = request.Status;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteRoomTypeAsync(Guid roomTypeId)
        {
            var room = await _context.RoomTypes
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r => r.RoomTypeId == roomTypeId && r.Hotel.IsActive);

            if (room == null)
                return false;

            _context.RoomTypes.Remove(room);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}


