using System;
using System.Collections.Generic;

namespace BtlThueXe.Infrastructure;

public partial class DanhGium
{
    public int Id { get; set; }

    public int IdHopDong { get; set; }

    public int IdKhachHang { get; set; }

    public int? IdXe { get; set; }

    public int DiemDanhGia { get; set; }

    public string? NhanXet { get; set; }

    public DateTime ThoiGianTao { get; set; }

    public virtual HopDong IdHopDongNavigation { get; set; } = null!;

    public virtual KhachHang IdKhachHangNavigation { get; set; } = null!;

    public virtual Xe? IdXeNavigation { get; set; }
}
