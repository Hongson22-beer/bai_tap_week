using System.ComponentModel.DataAnnotations;

namespace BtlThueXe.Core.DTOs.Cancellations;

public class CreateCancellationRequest
{
    [Required]
    public int IdHopDong { get; set; }

    [Required]
    public int IdNguoiYeuCau { get; set; }

    public string? LyDo { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SoTienHoanDuKien { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SoTienPhat { get; set; }
}