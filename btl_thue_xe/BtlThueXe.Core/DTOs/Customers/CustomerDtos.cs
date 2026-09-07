namespace BtlThueXe.Core.DTOs.Customers;

public class CustomerProfileResponseDto
{
    public int IdKhachHang { get; set; }
    public int IdNguoiDung { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SoDienThoai { get; set; } = string.Empty;
    public string SoCccd { get; set; } = string.Empty;
    public bool CccdDaXacMinh { get; set; }
    public string? DiaChi { get; set; }
    public DateOnly? NgaySinh { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class VerifyCccdRequestDto
{
    public bool DaXacMinh { get; set; }
}