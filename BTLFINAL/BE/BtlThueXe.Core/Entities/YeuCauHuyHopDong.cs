using System;
using System.Collections.Generic;


namespace BtlThueXe.Infrastructure;


public partial class YeuCauHuyHopDong
{
    public int Id { get; set; }

    public int IdHopDong { get; set; }

    public int IdNguoiYeuCau { get; set; }

    public string? LyDo { get; set; }

    public string TrangThai { get; set; } = null!;

    public decimal SoTienHoanDuKien { get; set; }

    public decimal SoTienPhat { get; set; }

    public int? IdNguoiXuLy { get; set; }

    public DateTime ThoiGianTao { get; set; }

    public DateTime? ThoiGianXuLy { get; set; }

    public virtual HopDong IdHopDongNavigation { get; set; } = null!;

    public virtual NguoiDung? IdNguoiXuLyNavigation { get; set; }

    public virtual NguoiDung IdNguoiYeuCauNavigation { get; set; } = null!;
}
