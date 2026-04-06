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
using System.Text;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
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
            IsVerified = true
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


    //public async Task SignupAsync(SignupRequest request)
    //{
    //    // Normalize email (important for consistency)
    //    var email = request.Email.Trim().ToLower();

    //    // Check existing user (optimized query)
    //    var userExists = await _context.Users
    //        .AsNoTracking()
    //        .AnyAsync(x => x.Email == email);

    //    if (userExists)
    //        throw new ApplicationException("User already exists");

    //    // Create user
    //    var user = new User
    //    {
    //        Id = Guid.NewGuid(),
    //        Email = email,
    //        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
    //        PhoneNumber = request.PhoneNumber,
    //        Role = "Guest",
    //        IsVerified = true
    //    };

    //    await _context.Users.AddAsync(user);
    //    await _context.SaveChangesAsync();
    //}

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLower();

        // Fetch user
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == email);

        if (user == null)
            throw new ApplicationException("Invalid email or password");

        // Verify password
        var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!isPasswordValid)
            throw new ApplicationException("Invalid email or password");

        // Generate JWT
        var token = GenerateJwtToken(user);

        return new LoginResponse
        {
            Token = token,
            Email = user.Email,
            Role = user.Role
        };
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // VERY IMPORTANT
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"])
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1), // use UTC
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
//public async Task GenerateOtpAsync(string phoneNumber)
//    {
//        var code = new Random().Next(100000, 999999).ToString();

//        var otp = new Otp
//        {
//            Id = Guid.NewGuid(),
//            PhoneNumber = phoneNumber,
//            Code = code,
//            ExpiryTime = DateTime.UtcNow.AddMinutes(5)
//        };

//        await _context.Otps.AddAsync(otp);
//        await _context.SaveChangesAsync();

//        var accountSid = _configuration["Twilio:AccountSid"];
//        var authToken = _configuration["Twilio:AuthToken"];
//        var fromPhone = _configuration["Twilio:FromPhone"];

//        if (!string.IsNullOrEmpty(accountSid) && !string.IsNullOrEmpty(authToken) && !string.IsNullOrEmpty(fromPhone))
//        {
//            TwilioClient.Init(accountSid, authToken);
//            MessageResource.Create(
//                to: new PhoneNumber(phoneNumber),
//                from: new PhoneNumber(fromPhone),
//                body: $"Your OTP is: {code} (valid 5 minutes)");
//        }
//    }

//    public async Task<bool> VerifyOtpAsync(string phoneNumber, string code)
//    {
//        var otp = await _context.Otps
//            .Where(o => o.PhoneNumber == phoneNumber && o.Code == code)
//            .OrderByDescending(o => o.ExpiryTime)
//            .FirstOrDefaultAsync();

//        if (otp == null || otp.ExpiryTime < DateTime.UtcNow)
//            return false;

//        _context.Otps.Remove(otp);

//        var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
//        if (user != null)
//        {
//            user.IsVerified = true;
//        }

//        await _context.SaveChangesAsync();
//        return true;
//    }
