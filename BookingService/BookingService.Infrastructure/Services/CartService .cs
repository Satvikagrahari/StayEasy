using BookingService.Application.DTOs.Request;
using BookingService.Application.Interfaces.Services;
using BookingService.Domain.Entities;
using BookingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace BookingService.Infrastructure.Services
{
   

    public class CartService : ICartService
    {
        private readonly BookingDbContext _context;
        private readonly HttpClient _httpClient;

        public CartService(BookingDbContext context, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;

        }

        public async Task AddToCartAsync(Guid userId, CreateCartItemRequest request)
        {
            if (request.CheckInDate.Date < DateTime.UtcNow.Date)
                throw new ArgumentException("Check-in date cannot be in the past");

            if (request.CheckOutDate.Date <= request.CheckInDate.Date)
                throw new ArgumentException("Check-out date must be after check-in date");

            var cart = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == "Active");

            if (cart == null)
            {
                cart = new Cart
                {
                    CartId = Guid.NewGuid(),
                    UserId = userId
                };

                await _context.Carts.AddAsync(cart);
                await _context.SaveChangesAsync();
            }
            var room = await _httpClient.GetFromJsonAsync<RoomTypeDto>(
                $"https://localhost:7092/api/hotels/rooms/{request.RoomTypeId}");

            if (room == null)
                throw new Exception("Room not found");
            var item = new CartItem
            {
                CartItemId = Guid.NewGuid(),
                CartId = cart.CartId,
                HotelId = request.HotelId,
                RoomTypeId = request.RoomTypeId,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                Guests = request.Guests,
                PriceSnapshot = room.PricePerNight
            };

            await _context.CartItems.AddAsync(item);

            await _context.SaveChangesAsync();
        }

        public async Task<Cart> GetCartAsync(Guid userId)
        {
            var cart = await _context.Carts
    .Include(c => c.Items)
    .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == "Active");

            return cart ?? new Cart
            {
                CartId = Guid.NewGuid(),
                UserId = userId,
                Items = new List<CartItem>()
            };

        }

        public async Task RemoveItemAsync(Guid userId, Guid itemId)
        {
            var item = await _context.CartItems
                .Join(_context.Carts,
        item => item.CartId,
        cart => cart.CartId,
        (item, cart) => new { item, cart })
    .Where(x => x.item.CartItemId == itemId && x.cart.UserId == userId)
    .Select(x => x.item)
    .FirstOrDefaultAsync();

            if (item == null)
                throw new Exception("Item not found or unauthorized");

            _context.CartItems.Remove(item);

            await _context.SaveChangesAsync();
        }
    }
}
