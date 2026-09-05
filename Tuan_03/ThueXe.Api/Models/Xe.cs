using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ThueXe.Api.Models;

[Table("xe")]
[Index(nameof(BienSoXe), IsUnique = true)]
public class Xe
{
    [Key, Column("id")]
    public int Id { get; set; }

    [Column("id_hang_xe")]
    public int IdHangXe { get; set; }

    [Column("id_loai_xe")]
    public int IdLoaiXe { get; set; }

    [Required, MaxLength(20), Column("bien_so_xe")]
    public string BienSoXe { get; set; } = string.Empty;

    [MaxLength(50), Column("mau_xe")]
    public string? MauXe { get; set; }

    [Column("nam_san_xuat")]
    public int? NamSanXuat { get; set; }

    [Column("don_gia_ngay", TypeName = "decimal(12,2)")]
    public decimal DonGiaNgay { get; set; }

    [Required, MaxLength(30), Column("trang_thai")]
    public string TrangThai { get; set; } = "AVAILABLE";

    [Column("mo_ta")]
    public string? MoTa { get; set; }

    [ForeignKey(nameof(IdHangXe))]
    public HangXe HangXe { get; set; } = null!;

    [ForeignKey(nameof(IdLoaiXe))]
    public LoaiXe LoaiXe { get; set; } = null!;
}