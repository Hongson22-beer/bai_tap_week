namespace BtlThueXe.Core.DTOs.Auth;

public class RegisterRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string MatKhau { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public string SoCccd { get; set; } = string.Empty;
    public string? DiaChi { get; set; }
    public DateOnly? NgaySinh { get; set; }
}

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string MatKhau { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}