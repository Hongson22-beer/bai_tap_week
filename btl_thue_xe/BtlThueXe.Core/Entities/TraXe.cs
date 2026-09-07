using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BtlThueXe.Core.Entities;

[Table("tra_xe")]
public class TraXe
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("id_hop_dong")]
    public int IdHopDong { get; set; }

    [Column("id_xe")]
    public int IdXe { get; set; }

    [Column("thoi_gian_tra_du_kien")]
    public DateTime ThoiGianTraDuKien { get; set; }

    [Column("thoi_gian_tra_thuc_te")]
    public DateTime ThoiGianTraThucTe { get; set; }

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

    [Column("phi_tra_muon", TypeName = "numeric(18,2)")]
    public decimal PhiTraMuon { get; set; }

    [Column("phi_phat_sinh", TypeName = "numeric(18,2)")]
    public decimal PhiPhatSinh { get; set; }

    [Column("tong_phi_phat_sinh", TypeName = "numeric(18,2)")]
    public decimal TongPhiPhatSinh { get; set; }

    [Column("id_nhan_vien")]
    public int? IdNhanVien { get; set; }

    [MaxLength(30)]
    [Column("trang_thai")]
    public string TrangThai { get; set; } = "COMPLETED";

    [Column("thoi_gian_tao")]
    public DateTime ThoiGianTao { get; set; } = DateTime.Now;
}