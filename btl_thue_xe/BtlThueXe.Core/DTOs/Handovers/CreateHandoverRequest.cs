using System.ComponentModel.DataAnnotations;

namespace BtlThueXe.Core.DTOs.Handovers;

public class CreateHandoverRequest
{
    [Required]
    public int IdHopDong { get; set; }

    [Required]
    public int IdXe { get; set; }

    [Range(0, int.MaxValue)]
    public int SoKm { get; set; }

    [Range(0, 100)]
    public decimal MucNhienLieu { get; set; }

    public string? TinhTrangXe { get; set; }

    public string? GhiChu { get; set; }

    public int? IdNhanVien { get; set; }
}