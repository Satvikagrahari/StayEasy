using System.Security.Claims;
using IdentityService.API.Controller;
using IdentityService.Application.DTOs.Request;
using IdentityService.Application.DTOs.Response;
using IdentityService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdentityService.Tests;

public class AuthControllerEndpointTests
{
    private static AuthController BuildAuthController(Mock<IAuthService> authService, Guid? userId = null)
    {
        var controller = new AuthController(authService.Object);
        if (userId.HasValue)
        {
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext(userId.Value)
            };
        }
        return controller;
    }

    private static DefaultHttpContext CreateHttpContext(Guid userId)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        ], "test"));
        return context;
    }

    [Fact]
    public async Task Signup_ReturnsCreated()
    {
        var authService = new Mock<IAuthService>();
        var controller = BuildAuthController(authService);

        var result = await controller.Signup(new SignupRequest());

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status201Created, status.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsOk()
    {
        var authService = new Mock<IAuthService>();
        authService.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(new LoginResponse { Token = "t", RefreshToken = "r", UserName = "u", Email = "e", Role = "Guest" });
        var controller = BuildAuthController(authService);

        var result = await controller.Login(new LoginRequest());

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Refresh_ReturnsOk()
    {
        var authService = new Mock<IAuthService>();
        authService.Setup(x => x.RefreshAsync(It.IsAny<RefreshTokenRequest>()))
            .ReturnsAsync(new LoginResponse { Token = "t", RefreshToken = "r", UserName = "u", Email = "e", Role = "Guest" });
        var controller = BuildAuthController(authService);

        var result = await controller.Refresh(new RefreshTokenRequest());

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Logout_ReturnsNoContent()
    {
        var userId = Guid.NewGuid();
        var authService = new Mock<IAuthService>();
        var controller = BuildAuthController(authService, userId);

        var result = await controller.Logout();

        Assert.IsType<NoContentResult>(result);
        authService.Verify(x => x.LogoutAsync(userId), Times.Once);
    }

    [Fact]
    public void GetMe_ReturnsOk()
    {
        var authService = new Mock<IAuthService>();
        var controller = BuildAuthController(authService, Guid.NewGuid());

        var result = controller.GetMe();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task SendOtp_ReturnsOk()
    {
        var authService = new Mock<IAuthService>();
        var controller = BuildAuthController(authService);

        var result = await controller.SendOtp(new SendOtpRequest());

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task VerifyOtp_ReturnsOk()
    {
        var authService = new Mock<IAuthService>();
        var controller = BuildAuthController(authService);

        var result = await controller.VerifyOtp(new VerifyOtpRequest());

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task SendPasswordResetOtp_ReturnsOk()
    {
        var authService = new Mock<IAuthService>();
        var controller = BuildAuthController(authService);

        var result = await controller.SendPasswordResetOtp(new PasswordResetSendOtpRequest());

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task VerifyPasswordResetOtp_ReturnsOk()
    {
        var authService = new Mock<IAuthService>();
        var controller = BuildAuthController(authService);

        var result = await controller.VerifyPasswordResetOtp(new PasswordResetVerifyOtpRequest());

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ResetPassword_ReturnsOk()
    {
        var authService = new Mock<IAuthService>();
        var controller = BuildAuthController(authService);

        var result = await controller.ResetPassword(new ResetPasswordRequest());

        Assert.IsType<OkObjectResult>(result);
    }
}

public class UsersControllerEndpointTests
{
    private static UsersController BuildController(Mock<IAuthService> authService, Guid userId)
    {
        var controller = new UsersController(authService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    ], "test"))
                }
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetProfile_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var authService = new Mock<IAuthService>();
        authService.Setup(x => x.GetProfileAsync(userId))
            .ReturnsAsync(new UserProfileResponse { Id = userId, UserName = "u", Email = "e", PhoneNumber = "p", Role = "Guest" });
        var controller = BuildController(authService, userId);

        var result = await controller.GetProfile();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateProfile_ReturnsNoContent()
    {
        var userId = Guid.NewGuid();
        var authService = new Mock<IAuthService>();
        var controller = BuildController(authService, userId);

        var result = await controller.UpdateProfile(new UpdateProfileRequest());

        Assert.IsType<NoContentResult>(result);
        authService.Verify(x => x.UpdateProfileAsync(userId, It.IsAny<UpdateProfileRequest>()), Times.Once);
    }
}

public class AdminUsersControllerEndpointTests
{
    [Fact]
    public async Task GetAllUsers_ReturnsOk()
    {
        var authService = new Mock<IAuthService>();
        authService.Setup(x => x.GetAllUsersAsync()).ReturnsAsync(new List<UserProfileResponse>());
        var controller = new AdminUsersController(authService.Object);

        var result = await controller.GetAllUsers();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsNoContent()
    {
        var authService = new Mock<IAuthService>();
        var controller = new AdminUsersController(authService.Object);

        var result = await controller.UpdateStatus(Guid.NewGuid(), new UpdateUserStatusRequest { IsActive = true });

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteUser_ReturnsNoContent()
    {
        var authService = new Mock<IAuthService>();
        var controller = new AdminUsersController(authService.Object);

        var result = await controller.DeleteUser(Guid.NewGuid());

        Assert.IsType<NoContentResult>(result);
    }
}

public class InternalControllerEndpointTests
{
    [Fact]
    public async Task GetUserEmail_WhenProfileMissing_ReturnsNotFound()
    {
        var authService = new Mock<IAuthService>();
        authService.Setup(x => x.GetProfileAsync(It.IsAny<Guid>()))
            .ReturnsAsync((UserProfileResponse)null!);
        var controller = new InternalController(authService.Object);

        var result = await controller.GetUserEmail(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetUserName_WhenProfileExists_ReturnsOk()
    {
        var authService = new Mock<IAuthService>();
        authService.Setup(x => x.GetProfileAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new UserProfileResponse { UserName = "satvik", Email = "s@example.com", PhoneNumber = "1", Role = "Guest" });
        var controller = new InternalController(authService.Object);

        var result = await controller.GetUserName(Guid.NewGuid());

        Assert.IsType<OkObjectResult>(result);
    }
}
