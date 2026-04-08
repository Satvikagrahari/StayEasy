using BookingService.Application.DTOs.Request;
using BookingService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Application.Interfaces.Services
{
    public interface ICartService
    {
        Task AddToCartAsync(Guid userId, CreateCartItemRequest request);

        Task<Cart> GetCartAsync(Guid userId);

        Task RemoveItemAsync(Guid userId, Guid itemId);
    }
}
