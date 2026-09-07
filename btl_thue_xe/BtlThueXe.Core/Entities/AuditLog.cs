using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BtlThueXe.Core.Entities;

[Table("audit_log")]
public class AuditLog
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("id_nguoi_dung")]
    public int? IdNguoiDung { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("hanh_dong")]
    public string HanhDong { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("loai_doi_tuong")]
    public string LoaiDoiTuong { get; set; } = string.Empty;

    [Column("id_doi_tuong")]
    public int? IdDoiTuong { get; set; }

    [Column("du_lieu_cu")]
    public string? DuLieuCu { get; set; }

    [Column("du_lieu_moi")]
    public string? DuLieuMoi { get; set; }

    [MaxLength(1000)]
    [Column("mo_ta")]
    public string? MoTa { get; set; }

    [MaxLength(45)]
    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [Column("thoi_gian")]
    public DateTime ThoiGian { get; set; } = DateTime.Now;
}