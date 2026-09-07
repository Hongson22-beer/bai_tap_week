using System.ComponentModel.DataAnnotations;

namespace BtlThueXe.Core.DTOs.AuditLogs;

public class CreateAuditLogRequest
{
    public int? IdNguoiDung { get; set; }

    [Required]
    [MaxLength(50)]
    public string HanhDong { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LoaiDoiTuong { get; set; } = string.Empty;

    public int? IdDoiTuong { get; set; }

    public string? DuLieuCu { get; set; }

    public string? DuLieuMoi { get; set; }

    [MaxLength(1000)]
    public string? MoTa { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }
}