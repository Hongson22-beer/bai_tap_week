using System;
using System.Collections.Generic;

namespace BtlThueXe.Infrastructure;

public partial class YeuCauGiaHan
{
    public int Id { get; set; }

    public int IdHopDong { get; set; }

    public int IdNguoiYeuCau { get; set; }

    public DateTime ThoiGianTraCu { get; set; }

    public DateTime ThoiGianTraMoi { get; set; }

    public int SoNgayGiaHan { get; set; }

    public decimal TienPhatSinh { get; set; }

    public string? LyDo { get; set; }

    public string TrangThai { get; set; } = null!;

    public int? IdNguoiXuLy { get; set; }

    public DateTime ThoiGianTao { get; set; }

    public DateTime? ThoiGianXuLy { get; set; }

    public virtual HopDong IdHopDongNavigation { get; set; } = null!;

    public virtual NguoiDung? IdNguoiXuLyNavigation { get; set; }

    public virtual NguoiDung IdNguoiYeuCauNavigation { get; set; } = null!;
}
