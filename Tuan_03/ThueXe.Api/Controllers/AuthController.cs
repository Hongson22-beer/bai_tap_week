using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThueXe.Api.Data;
using ThueXe.Api.Dtos;
using ThueXe.Api.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using ThueXe.Api.Models;
using System.Security.Claims;
using BCrypt.Net;
namespace ThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("login")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly TokenService _tokenService;

    public AuthController(AppDbContext context, IPasswordService passwordService, TokenService tokenService)
    {
        _context = context;
        _passwordService = passwordService;
        _tokenService = tokenService;
    }
[HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "Email và mật khẩu không được để trống." });
        }

        var emailLower = dto.Email.Trim().ToLower();
        var existingUser = await _context.Users.AnyAsync(u => u.Email == emailLower);
        if (existingUser)
        {
            return BadRequest(new { message = "Email này đã được sử dụng." });
        }

        var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
        if (customerRole == null)
        {
            return BadRequest(new { message = "Role Customer chưa được cấu hình trong hệ thống." });
        }

        var newUser = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = emailLower,
            FullName = dto.FullName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            IsActive = true
        };

        var userRole = new UserRole
        {
            UserId = newUser.Id,
            RoleId = customerRole.Id
        };

        _context.Users.Add(newUser);
        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Đăng ký tài khoản khách hàng thành công",
            userId = newUser.Id,
            email = newUser.Email,
            fullName = newUser.FullName
        });
    }
   [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user == null)
        {
            return Unauthorized(new { message = "Email hoặc mật khẩu không chính xác." });
        }

        bool isPasswordValid = false;

        // 1. Thử xác thực bằng BCrypt (dành cho tài khoản mới đăng ký)
        try
        {
            if (user.PasswordHash.StartsWith("$2"))
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            }
        }
        catch
        {
            isPasswordValid = false;
        }

        // 2. Nếu chưa hợp lệ, thử xác thực bằng Identity cũ (_passwordService)
        if (!isPasswordValid && _passwordService != null)
        {
            try
            {
                isPasswordValid = _passwordService.VerifyPassword(user, user.PasswordHash, request.Password);
            }
            catch
            {
                isPasswordValid = false;
            }
        }

        if (!isPasswordValid)
        {
            return Unauthorized(new { message = "Email hoặc mật khẩu không chính xác." });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { message = "Tài khoản của bạn đã bị khóa." });
        }

        var roles = user.UserRoles.Select(ur => ur.Role!.Name).ToList();
        var token = _tokenService.GenerateToken(user, roles);

        return Ok(new AuthResponse(token, user.Email, user.FullName, roles));
    }
}