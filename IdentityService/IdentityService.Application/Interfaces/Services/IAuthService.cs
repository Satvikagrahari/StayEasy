using IdentityService.Application.DTOs.Request;
using IdentityService.Application.DTOs.Response;
using System.Threading.Tasks;

namespace IdentityService.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task SignupAsync(SignupRequest request);
        Task<LoginResponse> LoginAsync(LoginRequest request);

        Task<LoginResponse> RefreshAsync(RefreshTokenRequest request);
        Task LogoutAsync(Guid userId);

        Task<UserProfileResponse> GetProfileAsync(Guid userId);
        Task UpdateProfileAsync(Guid userId, UpdateProfileRequest request);

        Task<List<UserProfileResponse>> GetAllUsersAsync();
        Task UpdateUserStatusAsync(Guid userId, bool isActive);
        Task DeleteUserAsync(Guid userId);
        Task SendOtpAsync(SendOtpRequest request);
        Task VerifyOtpAsync(VerifyOtpRequest request);
        Task SendPasswordResetOtpAsync(PasswordResetSendOtpRequest request);
        Task VerifyPasswordResetOtpAsync(PasswordResetVerifyOtpRequest request);
        Task ResetPasswordAsync(ResetPasswordRequest request);
    }
}
