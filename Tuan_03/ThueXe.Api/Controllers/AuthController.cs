using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThueXe.Api.Data;
using ThueXe.Api.Dtos;
using ThueXe.Api.Services;

namespace ThueXe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user == null || !_passwordService.VerifyPassword(user, user.PasswordHash, request.Password))
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