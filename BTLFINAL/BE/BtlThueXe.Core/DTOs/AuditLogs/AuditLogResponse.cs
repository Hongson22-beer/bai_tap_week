namespace BtlThueXe.Core.DTOs.AuditLogs;

public class AuditLogResponse
{
    public long Id { get; set; }

    public int? IdNguoiDung { get; set; }

    public string HanhDong { get; set; } = string.Empty;

    public string LoaiDoiTuong { get; set; } = string.Empty;

    public int? IdDoiTuong { get; set; }

    public string? DuLieuCu { get; set; }

    public string? DuLieuMoi { get; set; }

    public string? MoTa { get; set; }

    public string? IpAddress { get; set; }

    public DateTime ThoiGian { get; set; }
}