using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BtlThueXe.Core.Entities;

[Table("ban_giao_xe")]
public class BanGiaoXe
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("id_hop_dong")]
    public int IdHopDong { get; set; }

    [Column("id_xe")]
    public int IdXe { get; set; }

    [Column("so_km")]
    public int SoKm { get; set; }

    [Column("muc_nhien_lieu", TypeName = "numeric(5,2)")]
    public decimal MucNhienLieu { get; set; }

    [MaxLength(1000)]
    [Column("tinh_trang_xe")]
    public string? TinhTrangXe { get; set; }

    [MaxLength(1000)]
    [Column("ghi_chu")]
    public string? GhiChu { get; set; }

    [Column("id_nhan_vien")]
    public int? IdNhanVien { get; set; }

    [Column("thoi_gian_giao")]
    public DateTime ThoiGianGiao { get; set; } = DateTime.Now;
}