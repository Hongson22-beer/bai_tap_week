using System.ComponentModel.DataAnnotations;

namespace BtlThueXe.Core.DTOs.Evaluations;

public class CreateEvaluationRequest
{
    [Required]
    public int IdHopDong { get; set; }

    [Required]
    public int IdKhachHang { get; set; }

    public int? IdXe { get; set; }

    [Range(1, 5)]
    public int DiemDanhGia { get; set; }

    [MaxLength(2000)]
    public string? NhanXet { get; set; }
}