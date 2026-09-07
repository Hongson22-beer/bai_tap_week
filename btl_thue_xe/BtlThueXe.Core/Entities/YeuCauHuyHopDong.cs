using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BtlThueXe.Core.Entities;

[Table("yeu_cau_huy_hop_dong")]
public class YeuCauHuyHopDong
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("id_hop_dong")]
    public int IdHopDong { get; set; }

    [Column("id_nguoi_yeu_cau")]
    public int IdNguoiYeuCau { get; set; }

    [MaxLength(1000)]
    [Column("ly_do")]
    public string? LyDo { get; set; }

    [MaxLength(20)]
    [Column("trang_thai")]
    public string TrangThai { get; set; } = "PENDING";

    [Column("so_tien_hoan_du_kien", TypeName = "numeric(18,2)")]
    public decimal SoTienHoanDuKien { get; set; }

    [Column("so_tien_phat", TypeName = "numeric(18,2)")]
    public decimal SoTienPhat { get; set; }

    [Column("id_nguoi_xu_ly")]
    public int? IdNguoiXuLy { get; set; }

    [Column("thoi_gian_tao")]
    public DateTime ThoiGianTao { get; set; } = DateTime.Now;

    [Column("thoi_gian_xu_ly")]
    public DateTime? ThoiGianXuLy { get; set; }
}