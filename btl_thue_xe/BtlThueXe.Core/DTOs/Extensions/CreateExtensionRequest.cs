using System.ComponentModel.DataAnnotations;

namespace BtlThueXe.Core.DTOs.Extensions;

public class CreateExtensionRequest
{
    [Required]
    public int IdHopDong { get; set; }

    [Required]
    public int IdNguoiYeuCau { get; set; }

    [Required]
    public DateTime ThoiGianTraCu { get; set; }

    [Required]
    public DateTime ThoiGianTraMoi { get; set; }

    [Range(1, int.MaxValue)]
    public int SoNgayGiaHan { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TienPhatSinh { get; set; }

    public string? LyDo { get; set; }
}