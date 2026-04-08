using BookingService.Application.DTOs.Request;
using BookingService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookingService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        private Guid GetUserId()
        {
            return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        }

        [Authorize]
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart(CreateCartItemRequest request)
        {
            await _cartService.AddToCartAsync(GetUserId(), request);
            return Ok("Added to cart");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var cart = await _cartService.GetCartAsync(GetUserId());
            return Ok(cart);
        }

        [Authorize]
        [HttpDelete("{itemId}")]
        public async Task<IActionResult> Remove(Guid itemId)
        {
            await _cartService.RemoveItemAsync(GetUserId(), itemId);
            return Ok("Item removed");
        }
    }
}
