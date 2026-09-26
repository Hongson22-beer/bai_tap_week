using System.ComponentModel.DataAnnotations;

namespace BtlThueXe.Core.DTOs.Admin;

public class AdminUserResponse
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public bool DangHoatDong { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class UpdateUserStatusRequest
{
    public bool DangHoatDong { get; set; }
}

public class UpdateUserRolesRequest
{
    [Required]
    [MinLength(1)]
    public List<string> Roles { get; set; } = new();
}
