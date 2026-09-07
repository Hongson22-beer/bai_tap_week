using System.ComponentModel.DataAnnotations;

namespace BtlThueXe.Core.DTOs.Payments;

public class CreatePaymentRequest
{
    [Required]
    public int IdHopDong { get; set; }

    [Required]
    public string LoaiThanhToan { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal SoTien { get; set; }

    [Required]
    public string PhuongThuc { get; set; } = string.Empty;

    public string? MaGiaoDich { get; set; }

    public string? GhiChu { get; set; }
}