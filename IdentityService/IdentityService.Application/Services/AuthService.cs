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
using System.Text;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

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

        var userExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(x => x.Email == email);

        if (userExists)
            throw new ApplicationException("User already exists");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            PhoneNumber = request.PhoneNumber,
            Role = "Guest",
            IsVerified = true,
            IsActive = true
        };

        _context.Users.Add(user);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Possible concurrent signup — surface friendly error
            throw new ApplicationException("User already exists");
        }
    }

    private bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // Database-specific detection: inspect inner exception or SQL error code.
        var msg = ex.InnerException?.Message ?? string.Empty;
        return msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        // Fetch user
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == email);

        if (user == null)
            throw new ApplicationException("Invalid email or password");

        if (!user.IsActive)
            throw new ApplicationException("User is deactivated");

        // Verify password
        var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!isPasswordValid)
            throw new ApplicationException("Invalid email or password");

        // Generate JWT + refresh token
        var token = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        return new LoginResponse
        {
            Token = token,
            RefreshToken = refreshToken,
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

        return new UserProfileResponse
        {
            Id = user.Id,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            IsVerified = user.IsVerified,
            IsActive = user.IsActive
        };
    }

    public async Task UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
            throw new ApplicationException("User not found");

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var emailExists = await _context.Users
                .AsNoTracking()
                .AnyAsync(x => x.Email == normalizedEmail && x.Id != userId);

            if (emailExists)
                throw new ApplicationException("Email already in use");

            user.Email = normalizedEmail;
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            user.PhoneNumber = request.PhoneNumber.Trim();
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<UserProfileResponse>> GetAllUsersAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .Select(user => new UserProfileResponse
            {
                Id = user.Id,
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
            // Revoke refresh token when deactivating
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
        var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);
        if (user == null) throw new ApplicationException("User not found for this phone number");
        
        await _otpService.SendOtpAsync(request.PhoneNumber, request.Channel);
    }

    public async Task VerifyOtpAsync(VerifyOtpRequest request)
    {
        var isValid = await _otpService.VerifyOtpAsync(request.PhoneNumber, request.Code);
        if (!isValid) throw new ApplicationException("Invalid or expired OTP");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);
        if (user != null)
        {
            user.IsVerified = true;
            await _context.SaveChangesAsync();
        }
    }

    private string GenerateJwtToken(User user)
    {
        var jwtKey = _config["Jwt:Key"] ?? throw new ApplicationException("Jwt:Key is missing");
        var issuer = _config["Jwt:Issuer"] ?? throw new ApplicationException("Jwt:Issuer is missing");
        var audience = _config["Jwt:Audience"] ?? throw new ApplicationException("Jwt:Audience is missing");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // VERY IMPORTANT
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1), // use UTC
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
