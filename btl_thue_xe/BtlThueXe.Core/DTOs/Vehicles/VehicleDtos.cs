namespace BtlThueXe.Core.DTOs.Vehicles;

public class VehicleResponseDto
{
    public int Id { get; set; }
    public string BienSoXe { get; set; } = string.Empty;
    public string? MauXe { get; set; }
    public int? NamSanXuat { get; set; }
    public decimal DonGiaNgay { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public string HangXe { get; set; } = string.Empty;
    public string LoaiXe { get; set; } = string.Empty;
    public int SoCho { get; set; }
    public string? MoTa { get; set; }
}

public class CheckAvailableQueryDto
{
    public DateTime ThoiGianNhan { get; set; }
    public DateTime ThoiGianTra { get; set; }
    public int? IdLoaiXe { get; set; }
    public int? IdHangXe { get; set; }
}

public class CreateVehicleDto
{
    public int IdHangXe { get; set; }
    public int IdLoaiXe { get; set; }
    public string BienSoXe { get; set; } = string.Empty;
    public string? MauXe { get; set; }
    public int? NamSanXuat { get; set; }
    public decimal DonGiaNgay { get; set; }
    public string? MoTa { get; set; }
}

public class UpdateVehicleDto
{
    public int IdHangXe { get; set; }
    public int IdLoaiXe { get; set; }
    public string BienSoXe { get; set; } = string.Empty;
    public string? MauXe { get; set; }
    public int? NamSanXuat { get; set; }
    public decimal DonGiaNgay { get; set; }
    public string? MoTa { get; set; }
}

public class UpdateVehicleStatusDto
{
    public string TrangThai { get; set; } = string.Empty;
    public string? GhiChu { get; set; }
}

public class VehicleStatusHistoryDto
{
    public int Id { get; set; }
    public string? TrangThaiCu { get; set; }
    public string TrangThaiMoi { get; set; } = string.Empty;
    public string? LyDo { get; set; }
    public string? NguoiThucHien { get; set; }
    public DateTime CreatedAt { get; set; }
}