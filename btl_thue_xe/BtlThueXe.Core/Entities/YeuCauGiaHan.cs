using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BtlThueXe.Core.Entities;

[Table("yeu_cau_gia_han")]
public class YeuCauGiaHan
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("id_hop_dong")]
    public int IdHopDong { get; set; }

    [Column("id_nguoi_yeu_cau")]
    public int IdNguoiYeuCau { get; set; }

    [Column("thoi_gian_tra_cu")]
    public DateTime ThoiGianTraCu { get; set; }

    [Column("thoi_gian_tra_moi")]
    public DateTime ThoiGianTraMoi { get; set; }

    [Column("so_ngay_gia_han")]
    public int SoNgayGiaHan { get; set; }

    [Column("tien_phat_sinh", TypeName = "numeric(18,2)")]
    public decimal TienPhatSinh { get; set; }

    [MaxLength(500)]
    [Column("ly_do")]
    public string? LyDo { get; set; }

    [MaxLength(20)]
    [Column("trang_thai")]
    public string TrangThai { get; set; } = "PENDING";

    [Column("id_nguoi_xu_ly")]
    public int? IdNguoiXuLy { get; set; }

    [Column("thoi_gian_tao")]
    public DateTime ThoiGianTao { get; set; } = DateTime.Now;

    [Column("thoi_gian_xu_ly")]
    public DateTime? ThoiGianXuLy { get; set; }
}