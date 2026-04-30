using IdentityService.Application.DTOs.Request;
using IdentityService.Application.DTOs.Response;
using IdentityService.Application.Interfaces.Services;
using IdentityService.Domain.Entities;
using IdentityService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IOtpService _otpService;

    public AuthService(AppDbContext context, IConfiguration config, IOtpService otpService)
    {
        _context = context;
        _config = config;
        _otpService = otpService;
    }

    public async Task SignupAsync(SignupRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var userName = request.UserName.Trim();

        var userExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(x => x.Email == email || x.UserName == userName);

        if (userExists)
            throw new ApplicationException("Email or username already exists");

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            PhoneNumber = request.PhoneNumber.Trim(),
            Role = "Guest",
            IsVerified = false,
            IsActive = true,
            EmailOtpCode = GenerateOtpCode(),
            EmailOtpExpiryTime = DateTime.UtcNow.AddMinutes(10)
        };

        _context.Users.Add(user);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new ApplicationException("Email or username already exists");
        }

        await _otpService.SendOtpAsync(user.Email, user.EmailOtpCode);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);

        if (user == null)
            throw new ApplicationException("Invalid email or password");

        if (!user.IsActive)
            throw new ApplicationException("User is deactivated");

        if (!user.IsVerified)
            throw new ApplicationException("Please verify your email before logging in");

        var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!isPasswordValid)
            throw new ApplicationException("Invalid email or password");

        var token = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        return new LoginResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            UserName = user.UserName,
            Email = user.Email,
            Role = user.Role
        };
    }

    public async Task<LoginResponse> RefreshAsync(RefreshTokenRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x =>
            x.RefreshToken == request.RefreshToken &&
            x.RefreshTokenExpiryTime != null &&
            x.RefreshTokenExpiryTime > DateTime.UtcNow);

        if (user == null)
            throw new ApplicationException("Invalid or expired refresh token");

        if (!user.IsActive)
            throw new ApplicationException("User is deactivated");

        var jwt = GenerateJwtToken(user);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        return new LoginResponse
        {
            Token = jwt,
            RefreshToken = newRefreshToken,
            UserName = user.UserName,
            Email = user.Email,
            Role = user.Role
        };
    }

    public async Task LogoutAsync(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null)
            return;

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _context.SaveChangesAsync();
    }

    public async Task<UserProfileResponse> GetProfileAsync(Guid userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
            throw new ApplicationException("User not found");

        return ToProfileResponse(user);
    }

    public async Task UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
            throw new ApplicationException("User not found");

        var shouldSendVerificationEmail = false;

        if (!string.IsNullOrWhiteSpace(request.UserName))
        {
            var userName = request.UserName.Trim();
            var userNameExists = await _context.Users
                .AsNoTracking()
                .AnyAsync(x => x.UserName == userName && x.Id != userId);

            if (userNameExists)
                throw new ApplicationException("Username already in use");

            user.UserName = userName;
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var emailExists = await _context.Users
                .AsNoTracking()
                .AnyAsync(x => x.Email == normalizedEmail && x.Id != userId);

            if (emailExists)
                throw new ApplicationException("Email already in use");

            if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = normalizedEmail;
                user.IsVerified = false;
                user.EmailOtpCode = GenerateOtpCode();
                user.EmailOtpExpiryTime = DateTime.UtcNow.AddMinutes(10);
                shouldSendVerificationEmail = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            user.PhoneNumber = request.PhoneNumber.Trim();
        }

        await _context.SaveChangesAsync();

        if (shouldSendVerificationEmail && user.EmailOtpCode != null)
        {
            await _otpService.SendOtpAsync(user.Email, user.EmailOtpCode);
        }
    }

    public async Task<List<UserProfileResponse>> GetAllUsersAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .Select(user => new UserProfileResponse
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                IsVerified = user.IsVerified,
                IsActive = user.IsActive
            })
            .ToListAsync();
    }

    public async Task UpdateUserStatusAsync(Guid userId, bool isActive)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
            throw new ApplicationException("User not found");

        user.IsActive = isActive;

        if (!isActive)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null)
            throw new ApplicationException("User not found");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }

    public async Task SendOtpAsync(SendOtpRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            throw new ApplicationException("User not found for this email");

        user.EmailOtpCode = GenerateOtpCode();
        user.EmailOtpExpiryTime = DateTime.UtcNow.AddMinutes(10);
        await _context.SaveChangesAsync();

        await _otpService.SendOtpAsync(user.Email, user.EmailOtpCode);
    }

    public async Task VerifyOtpAsync(VerifyOtpRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            throw new ApplicationException("User not found for this email");

        if (string.IsNullOrWhiteSpace(user.EmailOtpCode) ||
            user.EmailOtpExpiryTime == null ||
            user.EmailOtpExpiryTime < DateTime.UtcNow ||
            user.EmailOtpCode != request.Code.Trim())
        {
            throw new ApplicationException("Invalid or expired OTP");
        }

        user.IsVerified = true;
        user.EmailOtpCode = null;
        user.EmailOtpExpiryTime = null;
        await _context.SaveChangesAsync();
    }

    public async Task SendPasswordResetOtpAsync(PasswordResetSendOtpRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            throw new ApplicationException("User not found for this email");

        if (!user.IsActive)
            throw new ApplicationException("User is deactivated");

        user.EmailOtpCode = GenerateOtpCode();
        user.EmailOtpExpiryTime = DateTime.UtcNow.AddMinutes(10);
        await _context.SaveChangesAsync();

        await _otpService.SendOtpAsync(user.Email, user.EmailOtpCode);
    }

    public async Task VerifyPasswordResetOtpAsync(PasswordResetVerifyOtpRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            throw new ApplicationException("User not found for this email");

        ValidateOtp(user, request.Code);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            throw new ApplicationException("User not found for this email");

        ValidateOtp(user, request.Code);
        ValidatePassword(request.NewPassword);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.IsVerified = true;
        user.EmailOtpCode = null;
        user.EmailOtpExpiryTime = null;
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;

        await _context.SaveChangesAsync();
    }

    private string GenerateJwtToken(User user)
    {
        var jwtKey = _config["Jwt:Key"] ?? throw new ApplicationException("Jwt:Key is missing");
        var issuer = _config["Jwt:Issuer"] ?? throw new ApplicationException("Jwt:Issuer is missing");
        var audience = _config["Jwt:Audience"] ?? throw new ApplicationException("Jwt:Audience is missing");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresMinutes = double.Parse(_config["Jwt:ExpiresInMinutes"] ?? "15");

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserProfileResponse ToProfileResponse(User user)
    {
        return new UserProfileResponse
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            IsVerified = user.IsVerified,
            IsActive = user.IsActive
        };
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? string.Empty;
        return msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string GenerateOtpCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    private static void ValidateOtp(User user, string code)
    {
        if (string.IsNullOrWhiteSpace(user.EmailOtpCode) ||
            user.EmailOtpExpiryTime == null ||
            user.EmailOtpExpiryTime < DateTime.UtcNow ||
            user.EmailOtpCode != code.Trim())
        {
            throw new ApplicationException("Invalid or expired OTP");
        }
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) ||
            password.Length < 8 ||
            !password.Any(char.IsUpper) ||
            !password.Any(char.IsDigit))
        {
            throw new ApplicationException("Password must be at least 8 characters and include an uppercase letter and a number");
        }
    }
}
