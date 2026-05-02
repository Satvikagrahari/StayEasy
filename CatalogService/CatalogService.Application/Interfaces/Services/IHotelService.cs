using CatalogService.Application.DTOs.Request;
using CatalogService.Application.DTOs.Response;
using CatalogService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CatalogService.Application.Interfaces.Services
{
    public interface IHotelService
    {
        Task CreateHotelAsync(CreateHotelRequest request);

        //Task<List<Hotel>> GetAllHotelsAsync();
        Task<List<HotelResponseDto>> GetHotelsAsync(string? roomType);

        Task AddRoomTypeAsync(CreateRoomTypeRequest request);

        Task<HotelResponseDto> GetHotelByIdAsync(Guid id);
        Task<RoomTypeDto> GetRoomByIdAsync(Guid roomTypeId);

        Task<bool> UpdateHotelAsync(Guid id, UpdateHotelRequest request);
        Task<bool> DeactivateHotelAsync(Guid id);
        Task<bool> UpdateRoomTypeAsync(Guid roomTypeId, UpdateRoomTypeRequest request);
        Task<bool> DeleteRoomTypeAsync(Guid roomTypeId);
    }
}
