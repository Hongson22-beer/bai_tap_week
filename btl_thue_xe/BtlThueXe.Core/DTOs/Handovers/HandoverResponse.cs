namespace BtlThueXe.Core.DTOs.Handovers;

public class HandoverResponse
{
    public int Id { get; set; }

    public int IdHopDong { get; set; }

    public int IdXe { get; set; }

    public int SoKm { get; set; }

    public decimal MucNhienLieu { get; set; }

    public string? TinhTrangXe { get; set; }

    public string? GhiChu { get; set; }

    public int? IdNhanVien { get; set; }

    public DateTime ThoiGianGiao { get; set; }
}