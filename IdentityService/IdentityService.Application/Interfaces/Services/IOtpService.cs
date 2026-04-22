using System.Threading.Tasks;

namespace IdentityService.Application.Interfaces.Services
{
    public interface IOtpService
    {
        Task SendOtpAsync(string phoneNumber, string channel);
        Task<bool> VerifyOtpAsync(string phoneNumber, string code);
    }
}
