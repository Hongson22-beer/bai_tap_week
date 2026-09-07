using System.ComponentModel.DataAnnotations;

namespace BtlThueXe.Core.DTOs.Extensions;

public class ProcessExtensionRequest
{
    [Required]
    public int IdNguoiXuLy { get; set; }

    [Required]
    public string TrangThai { get; set; } = string.Empty;
}