using BookingService.Application.DTOs.Request;
using BookingService.Application.Interfaces.Services;
using BookingService.Domain.Entities;
using BookingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Infrastructure.Services
{
   

    public class CartService : ICartService
    {
        private readonly BookingDbContext _context;

        public CartService(BookingDbContext context)
        {
            _context = context;
        }

        public async Task AddToCartAsync(Guid userId, CreateCartItemRequest request)
        {
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

            var item = new CartItem
            {
                CartItemId = Guid.NewGuid(),
                CartId = cart.CartId,
                HotelId = request.HotelId,
                RoomTypeId = request.RoomTypeId,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                Guests = request.Guests,
                PriceSnapshot = 1000
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
