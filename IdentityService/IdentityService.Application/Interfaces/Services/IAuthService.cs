using IdentityService.Application.DTOs.Request;
using IdentityService.Application.DTOs.Response;
using System.Threading.Tasks;

namespace IdentityService.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task SignupAsync(SignupRequest request);
        Task<LoginResponse> LoginAsync(LoginRequest request);
        //Task GenerateOtpAsync(string phoneNumber);
        //Task<bool> VerifyOtpAsync(string phoneNumber, string code);
    }
}