using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BtlThueXe.Core.Entities;

[Table("danh_gia")]
public class DanhGia
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("id_hop_dong")]
    public int IdHopDong { get; set; }

    [Column("id_khach_hang")]
    public int IdKhachHang { get; set; }

    [Column("id_xe")]
    public int? IdXe { get; set; }

    [Column("diem_danh_gia")]
    public int DiemDanhGia { get; set; }

    [MaxLength(2000)]
    [Column("nhan_xet")]
    public string? NhanXet { get; set; }

    [Column("thoi_gian_tao")]
    public DateTime ThoiGianTao { get; set; } = DateTime.Now;
}