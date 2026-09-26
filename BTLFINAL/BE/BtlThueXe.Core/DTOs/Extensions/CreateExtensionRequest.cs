using System.ComponentModel.DataAnnotations;

namespace BtlThueXe.Core.DTOs.Extensions;

public class CreateExtensionRequest
{
    [Required]
    public int IdHopDong { get; set; }

    [Required]
    public DateTime ThoiGianTraMoi { get; set; }

    [MaxLength(500)]
    public string? LyDo { get; set; }

    // Các trường cũ giữ lại để tương thích FE cũ nhưng backend KHÔNG tin/calculate từ client.
    public int IdNguoiYeuCau { get; set; }
    public DateTime ThoiGianTraCu { get; set; }
    public int SoNgayGiaHan { get; set; }
    public decimal TienPhatSinh { get; set; }
}
