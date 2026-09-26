namespace BtlThueXe.Core.DTOs.Extensions;

public class ExtensionResponse
{
    public int Id { get; set; }

    public int IdHopDong { get; set; }

    public int IdNguoiYeuCau { get; set; }

    public DateTime ThoiGianTraCu { get; set; }

    public DateTime ThoiGianTraMoi { get; set; }

    public int SoNgayGiaHan { get; set; }

    public decimal TienPhatSinh { get; set; }

    public string? LyDo { get; set; }

    public string TrangThai { get; set; } = string.Empty;

    public int? IdNguoiXuLy { get; set; }

    public DateTime ThoiGianTao { get; set; }

    public DateTime? ThoiGianXuLy { get; set; }
}