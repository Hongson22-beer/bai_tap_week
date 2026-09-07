namespace BtlThueXe.Core.DTOs.Cancellations;

public class CancellationResponse
{
    public int Id { get; set; }

    public int IdHopDong { get; set; }

    public int IdNguoiYeuCau { get; set; }

    public string? LyDo { get; set; }

    public string TrangThai { get; set; } = string.Empty;

    public decimal SoTienHoanDuKien { get; set; }

    public decimal SoTienPhat { get; set; }

    public int? IdNguoiXuLy { get; set; }

    public DateTime ThoiGianTao { get; set; }

    public DateTime? ThoiGianXuLy { get; set; }
}