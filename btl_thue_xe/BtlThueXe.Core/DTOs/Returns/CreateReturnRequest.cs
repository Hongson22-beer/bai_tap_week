using System.ComponentModel.DataAnnotations;

namespace BtlThueXe.Core.DTOs.Returns;

public class CreateReturnRequest
{
    [Required]
    public int IdHopDong { get; set; }

    [Required]
    public int IdXe { get; set; }

    [Required]
    public DateTime ThoiGianTraDuKien { get; set; }

    public DateTime? ThoiGianTraThucTe { get; set; }

    [Range(0, int.MaxValue)]
    public int SoKm { get; set; }

    [Range(0, 100)]
    public decimal MucNhienLieu { get; set; }

    public string? TinhTrangXe { get; set; }

    public string? GhiChu { get; set; }

    [Range(0, double.MaxValue)]
    public decimal PhiTraMuon { get; set; }

    [Range(0, double.MaxValue)]
    public decimal PhiPhatSinh { get; set; }

    public int? IdNhanVien { get; set; }
}