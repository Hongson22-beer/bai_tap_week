using System;
using System.Collections.Generic;

namespace BtlThueXe.Infrastructure;

public partial class YeuCauThue
{
    public int Id { get; set; }

    public int IdKhachHang { get; set; }

    public int IdXe { get; set; }

    public string Nguon { get; set; } = null!;

    public DateTime ThoiGianNhan { get; set; }

    public DateTime ThoiGianTraDuKien { get; set; }

    public string TrangThai { get; set; } = null!;

    public decimal TienUocTinh { get; set; }

    public decimal TienCoc { get; set; }

    public string? LyDoTuChoi { get; set; }

    public string? LyDoHuy { get; set; }

    public int? IdNhanVienXuLy { get; set; }

    public DateTime ThoiGianTao { get; set; }

    public DateTime ThoiGianCapNhat { get; set; }

    public virtual HopDong? HopDong { get; set; }

    public virtual KhachHang IdKhachHangNavigation { get; set; } = null!;

    public virtual NguoiDung? IdNhanVienXuLyNavigation { get; set; }

    public virtual Xe IdXeNavigation { get; set; } = null!;
}
