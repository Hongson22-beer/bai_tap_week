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
    public string? AnhCccdMatTruoc { get; set; }
    public string? AnhCccdMatSau { get; set; }
    public string? SoGplx { get; set; }
    public string? AnhGplx { get; set; }
    public bool GplxDaXacMinh { get; set; }
    public string? NganHang { get; set; }
    public string? SoTaiKhoan { get; set; }
    public string? ChuTaiKhoan { get; set; }
    public string? DiaChi { get; set; }
    public DateOnly? NgaySinh { get; set; }
    public bool HoSoThueXeDayDu => !string.IsNullOrWhiteSpace(SoCccd) && !string.IsNullOrWhiteSpace(AnhCccdMatTruoc) && !string.IsNullOrWhiteSpace(AnhCccdMatSau) && !string.IsNullOrWhiteSpace(SoGplx) && !string.IsNullOrWhiteSpace(AnhGplx);
    public bool HoSoDaXacMinh => CccdDaXacMinh && GplxDaXacMinh;
    public List<string> Roles { get; set; } = new();
}

public class UpdateCustomerProfileRequestDto
{
    public string? SoDienThoai { get; set; }
    public string? SoCccd { get; set; }
    public string? SoGplx { get; set; }
    public string? DiaChi { get; set; }
    public DateOnly? NgaySinh { get; set; }
    public string? NganHang { get; set; }
    public string? SoTaiKhoan { get; set; }
    public string? ChuTaiKhoan { get; set; }
}

public class VerifyCccdRequestDto { public bool DaXacMinh { get; set; } }
public class VerifyDocumentsRequestDto { public bool CccdDaXacMinh { get; set; } public bool GplxDaXacMinh { get; set; } }
