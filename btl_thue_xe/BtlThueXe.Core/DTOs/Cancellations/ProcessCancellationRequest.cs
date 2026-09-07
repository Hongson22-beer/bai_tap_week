using System.ComponentModel.DataAnnotations;

namespace BtlThueXe.Core.DTOs.Cancellations;

public class ProcessCancellationRequest
{
    [Required]
    public int IdNguoiXuLy { get; set; }

    [Required]
    public string TrangThai { get; set; } = string.Empty;
}