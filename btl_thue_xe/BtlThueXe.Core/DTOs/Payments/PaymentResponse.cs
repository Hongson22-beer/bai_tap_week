namespace BtlThueXe.Core.DTOs.Payments;

public class PaymentResponse
{
    public int Id { get; set; }

    public int IdHopDong { get; set; }

    public string LoaiThanhToan { get; set; } = string.Empty;

    public decimal SoTien { get; set; }

    public string PhuongThuc { get; set; } = string.Empty;

    public string? MaGiaoDich { get; set; }

    public string TrangThai { get; set; } = string.Empty;

    public string? GhiChu { get; set; }

    public DateTime? ThoiGianThanhToan { get; set; }
}