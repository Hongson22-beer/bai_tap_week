using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BtlThueXe.Core.Entities;

[Table("thanh_toan")]
public class ThanhToan
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("id_hop_dong")]
    public int IdHopDong { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("loai_thanh_toan")]
    public string LoaiThanhToan { get; set; } = string.Empty;

    [Column("so_tien", TypeName = "numeric(18,2)")]
    public decimal SoTien { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("phuong_thuc")]
    public string PhuongThuc { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("ma_giao_dich")]
    public string? MaGiaoDich { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("trang_thai")]
    public string TrangThai { get; set; } = "PENDING";

    [MaxLength(500)]
    [Column("ghi_chu")]
    public string? GhiChu { get; set; }

    [Column("thoi_gian_thanh_toan")]
    public DateTime? ThoiGianThanhToan { get; set; }
}