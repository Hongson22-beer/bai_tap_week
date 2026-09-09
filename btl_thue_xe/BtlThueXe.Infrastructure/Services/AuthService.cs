using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BtlThueXe.Core.DTOs.Auth;
using BtlThueXe.Core.Interfaces;
using BtlThueXe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;


namespace BtlThueXe.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;

    public AuthService(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<AuthResponseDto> RegisterCustomerAsync(RegisterRequestDto dto)
    {
        // 1. Kiểm tra BR-01: Email duy nhất
        if (await _context.NguoiDungs.AnyAsync(u => u.Email == dto.Email))
            throw new Exception("Email đã được sử dụng trong hệ thống.");

        // 2. Kiểm tra BR-09: CCCD duy nhất
        if (await _context.KhachHangs.AnyAsync(k => k.SoCccd == dto.SoCccd))
            throw new Exception("Số CCCD đã được đăng ký trong hệ thống.");

        // 3. Hash mật khẩu (NFR-SEC-01)
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau);

        // Chuẩn hóa thời gian không kèm timezone để khớp với kiểu TIMESTAMP(0) trong xe2.sql
        var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

        var nguoiDung = new NguoiDung
        {
            Email = dto.Email,
            MatKhau = passwordHash,
            HoTen = dto.HoTen,
            SoDienThoai = dto.SoDienThoai,
            DangHoatDong = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        // 4. Gán vai trò KHACH_HANG
        var role = await _context.VaiTros.FirstOrDefaultAsync(r => r.Ten == "KHACH_HANG");
        if (role != null)
        {
            nguoiDung.IdVaiTros.Add(role);
        }

        // Lưu NguoiDung trước để CSDL sinh Id tự tăng
        _context.NguoiDungs.Add(nguoiDung);
        await _context.SaveChangesAsync();

        // 5. Tạo hồ sơ Khách hàng và gán IdNguoiDung từ bản ghi vừa lưu
        var khachHang = new KhachHang
        {
            IdNguoiDung = nguoiDung.Id,
            SoCccd = dto.SoCccd,
            CccdDaXacMinh = false,
            DiaChi = dto.DiaChi,
            NgaySinh = dto.NgaySinh,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.KhachHangs.Add(khachHang);
        await _context.SaveChangesAsync();

        var roles = new List<string> { "KHACH_HANG" };
        var token = GenerateJwtToken(nguoiDung, roles);

        return new AuthResponseDto
        {
            Token = token,
            Id = nguoiDung.Id,
            Email = nguoiDung.Email,
            HoTen = nguoiDung.HoTen,
            Roles = roles
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var nguoiDung = await _context.NguoiDungs
            .Include(u => u.IdVaiTros)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (nguoiDung == null || !BCrypt.Net.BCrypt.Verify(dto.MatKhau, nguoiDung.MatKhau))
            throw new Exception("Email hoặc mật khẩu không chính xác.");

        if (!nguoiDung.DangHoatDong)
            throw new Exception("Tài khoản của bạn đã bị khóa.");

        var roles = nguoiDung.IdVaiTros.Select(r => r.Ten).ToList();
        var token = GenerateJwtToken(nguoiDung, roles);

        return new AuthResponseDto
        {
            Token = token,
            Id = nguoiDung.Id,
            Email = nguoiDung.Email,
            HoTen = nguoiDung.HoTen,
            Roles = roles
        };
    }

    private string GenerateJwtToken(NguoiDung user, List<string> roles)
    {
        var jwtKey = _config["Jwt:Key"] ?? "DefaultSuperSecretKeyForDevelopmentOnly2026";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.HoTen)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "BtlThueXeApi",
            audience: _config["Jwt:Audience"] ?? "BtlThueXeClient",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:DurationInMinutes"] ?? "120")),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}