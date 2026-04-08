using CatalogService.Application.DTOs.Request;
using CatalogService.Application.DTOs.Response;
using CatalogService.Application.Interfaces.Services;
using CatalogService.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HotelsController : ControllerBase
    {
        private readonly IHotelService _hotelService;

        public HotelsController(IHotelService hotelService)
        {
            _hotelService = hotelService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateHotelRequest request)
        {
            await _hotelService.CreateHotelAsync(request);
            return StatusCode(201);
        }

        //[Authorize]
        //[HttpGet]
        //public async Task<IActionResult> GetAll()
        //{
        //    var hotels = await _hotelService.GetAllHotelsAsync();
        //    return Ok(hotels);
        //}

        [Authorize(Roles = "Admin")]
        [HttpPost("rooms")]
        public async Task<IActionResult> AddRoom(CreateRoomTypeRequest request)
        {
            await _hotelService.AddRoomTypeAsync(request);

            return Ok("Room type added");
        }

        [HttpGet]
        public async Task<IActionResult> GetHotels([FromQuery] string? roomType)
        {
            var result = await _hotelService.GetHotelsAsync(roomType);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var hotel = await _hotelService.GetHotelByIdAsync(id);
            return Ok(hotel);
        }
    }
}
