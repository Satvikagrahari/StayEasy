using CatalogService.Application.DTOs.Request;
using CatalogService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CatalogService.Application.Interfaces.Services
{
    public interface IHotelService
    {
        Task CreateHotelAsync(CreateHotelRequest request);

        Task<List<Hotel>> GetAllHotelsAsync();

        Task<Hotel> GetHotelByIdAsync(Guid id);
    }
}
