using CatalogService.API.Controllers;
using CatalogService.Application.DTOs.Request;
using CatalogService.Application.DTOs.Response;
using CatalogService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CatalogService.Tests;

public class HotelsControllerEndpointTests
{
    [Test]
    public async Task Create_ReturnsCreated()
    {
        var service = new Mock<IHotelService>();
        var controller = new HotelsController(service.Object);

        var result = await controller.Create(new CreateHotelRequest());

        Assert.That(result, Is.InstanceOf<StatusCodeResult>());
        var status = (StatusCodeResult)result;
        Assert.That(status.StatusCode, Is.EqualTo(201));
    }

    [Test]
    public async Task UpdateHotel_WhenMissing_ReturnsNotFound()
    {
        var service = new Mock<IHotelService>();
        service.Setup(x => x.UpdateHotelAsync(It.IsAny<Guid>(), It.IsAny<UpdateHotelRequest>())).ReturnsAsync(false);
        var controller = new HotelsController(service.Object);

        var result = await controller.UpdateHotel(Guid.NewGuid(), new UpdateHotelRequest());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task DeactivateHotel_WhenMissing_ReturnsNotFound()
    {
        var service = new Mock<IHotelService>();
        service.Setup(x => x.DeactivateHotelAsync(It.IsAny<Guid>())).ReturnsAsync(false);
        var controller = new HotelsController(service.Object);

        var result = await controller.DeactivateHotel(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task DeleteHotel_WhenMissing_ReturnsNotFound()
    {
        var service = new Mock<IHotelService>();
        service.Setup(x => x.DeactivateHotelAsync(It.IsAny<Guid>())).ReturnsAsync(false);
        var controller = new HotelsController(service.Object);

        var result = await controller.DeleteHotel(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task AddRoom_ReturnsOk()
    {
        var service = new Mock<IHotelService>();
        var controller = new HotelsController(service.Object);

        var result = await controller.AddRoom(new CreateRoomTypeRequest());

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task UpdateRoom_WhenMissing_ReturnsNotFound()
    {
        var service = new Mock<IHotelService>();
        service.Setup(x => x.UpdateRoomTypeAsync(It.IsAny<Guid>(), It.IsAny<UpdateRoomTypeRequest>())).ReturnsAsync(false);
        var controller = new HotelsController(service.Object);

        var result = await controller.UpdateRoom(Guid.NewGuid(), new UpdateRoomTypeRequest());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task DeleteRoomType_WhenMissing_ReturnsNotFound()
    {
        var service = new Mock<IHotelService>();
        service.Setup(x => x.DeleteRoomTypeAsync(It.IsAny<Guid>())).ReturnsAsync(false);
        var controller = new HotelsController(service.Object);

        var result = await controller.DeleteRoomType(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetAllHotels_ReturnsOk()
    {
        var service = new Mock<IHotelService>();
        service.Setup(x => x.GetHotelsAsync(null)).ReturnsAsync(new List<HotelResponseDto>());
        var controller = new HotelsController(service.Object);

        var result = await controller.GetAllHotels();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetHotelsByRoomType_ReturnsOk()
    {
        var service = new Mock<IHotelService>();
        service.Setup(x => x.GetHotelsAsync("Deluxe")).ReturnsAsync(new List<HotelResponseDto>());
        var controller = new HotelsController(service.Object);

        var result = await controller.GetHotelsByRoomType("Deluxe");

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_ReturnsOk()
    {
        var service = new Mock<IHotelService>();
        service.Setup(x => x.GetHotelByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new HotelResponseDto());
        var controller = new HotelsController(service.Object);

        var result = await controller.GetById(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetRoomById_WhenMissing_ReturnsNotFound()
    {
        var service = new Mock<IHotelService>();
        service.Setup(x => x.GetRoomByIdAsync(It.IsAny<Guid>())).ReturnsAsync((RoomTypeDto)null!);
        var controller = new HotelsController(service.Object);

        var result = await controller.GetRoomById(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }
}


