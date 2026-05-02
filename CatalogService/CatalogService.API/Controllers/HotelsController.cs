using CatalogService.Application.DTOs.Request;
using CatalogService.Application.DTOs.Response;
using CatalogService.Application.Interfaces.Services;
using CatalogService.Domain.Entities;
using CatalogService.Infrastructure.Data;
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
        //private readonly CatalogDbContext _context;

        public HotelsController(IHotelService hotelService)
        {
            _hotelService = hotelService;
        }
        [Authorize(Roles = "Admin")]
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
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateHotel(Guid id, [FromBody] UpdateHotelRequest request)
        {
            var updated = await _hotelService.UpdateHotelAsync(id, request);
            if (!updated)
                return NotFound();

            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeactivateHotel(Guid id)
        {
            var deactivated = await _hotelService.DeactivateHotelAsync(id);
            if (!deactivated)
                return NotFound();

            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("delete/hotel/{id:guid}")]
        public async Task<IActionResult> DeleteHotel(Guid id)
        {
            var deactivated = await _hotelService.DeactivateHotelAsync(id);
            if (!deactivated)
                return NotFound();

            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("rooms")]
        public async Task<IActionResult> AddRoom(CreateRoomTypeRequest request)
        {
            await _hotelService.AddRoomTypeAsync(request);

            return Ok("Room type added");
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("rooms/{id:guid}")]
        public async Task<IActionResult> UpdateRoom(Guid id, [FromBody] UpdateRoomTypeRequest request)
        {
            var updated = await _hotelService.UpdateRoomTypeAsync(id, request);
            if (!updated)
                return NotFound();

            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("delete/roomtype/{id:guid}")]
        public async Task<IActionResult> DeleteRoomType(Guid id)
        {
            var deleted = await _hotelService.DeleteRoomTypeAsync(id);
            if (!deleted)
                return NotFound();

            return NoContent();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllHotels()
        {
            var result = await _hotelService.GetHotelsAsync(null);
            return Ok(result);
        }


        [HttpGet("filter")]
        public async Task<IActionResult> GetHotelsByRoomType([FromQuery] string roomType)
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

        [HttpGet("rooms/{roomTypeId}")]
        public async Task<IActionResult> GetRoomById(Guid roomTypeId)
        {
            var room = await _hotelService.GetRoomByIdAsync(roomTypeId);

            if (room == null)
                return NotFound();

            return Ok(new
            {
                room.RoomTypeId,
                room.Name,
                room.PricePerNight,
                room.TotalRooms,
                room.AvailableRooms
            });
        }
    }
}
