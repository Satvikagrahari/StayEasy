using System.Threading.Tasks;

namespace IdentityService.Application.Interfaces.Services
{
    public interface IOtpService
    {
        Task SendOtpAsync(string email, string code);
    }
}
