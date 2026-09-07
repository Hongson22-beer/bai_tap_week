using System;
using System.Collections.Generic;

namespace BtlThueXe.Infrastructure;

public partial class Xe
{
    public int Id { get; set; }

    public int IdHangXe { get; set; }

    public int IdLoaiXe { get; set; }

    public string BienSoXe { get; set; } = null!;

    public string? MauXe { get; set; }

    public int? NamSanXuat { get; set; }

    public decimal DonGiaNgay { get; set; }

    public string TrangThai { get; set; } = null!;

    public string? MoTa { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<BanGiaoXe> BanGiaoXes { get; set; } = new List<BanGiaoXe>();

    public virtual ICollection<DanhGium> DanhGia { get; set; } = new List<DanhGium>();

    public virtual HangXe IdHangXeNavigation { get; set; } = null!;

    public virtual LoaiXe IdLoaiXeNavigation { get; set; } = null!;

    public virtual ICollection<LichSuTrangThaiXe> LichSuTrangThaiXes { get; set; } = new List<LichSuTrangThaiXe>();

    public virtual ICollection<TraXe> TraXes { get; set; } = new List<TraXe>();

    public virtual ICollection<YeuCauThue> YeuCauThues { get; set; } = new List<YeuCauThue>();
}
