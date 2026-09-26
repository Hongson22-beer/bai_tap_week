namespace BtlThueXe.Core.DTOs.Returns;

public class ReturnResponse
{
    public int Id { get; set; }

    public int IdHopDong { get; set; }

    public int IdXe { get; set; }

    public DateTime ThoiGianTraDuKien { get; set; }

    public DateTime ThoiGianTraThucTe { get; set; }

    public int SoKm { get; set; }

    public decimal MucNhienLieu { get; set; }

    public string? TinhTrangXe { get; set; }

    public string? GhiChu { get; set; }

    public decimal PhiTraMuon { get; set; }

    public decimal PhiPhatSinh { get; set; }

    public decimal TongPhiPhatSinh { get; set; }

    public int? IdNhanVien { get; set; }

    public string TrangThai { get; set; } = string.Empty;

    public DateTime ThoiGianTao { get; set; }
}