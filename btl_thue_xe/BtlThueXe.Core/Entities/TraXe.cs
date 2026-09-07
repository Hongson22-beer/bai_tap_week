using System;
using System.Collections.Generic;

namespace BtlThueXe.Infrastructure;

public partial class TraXe
{
    public int Id { get; set; }

    public int IdHopDong { get; set; }

    public int IdXe { get; set; }

    public DateTime ThoiGianTraDuKien { get; set; }

    public DateTime ThoiGianTraThucTe { get; set; }

    public int SoKm { get; set; }

    public decimal MucNhienLieu { get; set; }

    public string? TinhTrangXe { get; set; }

    public string? GhiChu { get; set; }

    public decimal PhiTraMuon { get; set; }

    public decimal PhiPhatSinh { get; set; }

    public decimal TongPhiPhatSinh { get; set; }

    public int? IdNhanVien { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime ThoiGianTao { get; set; }

    public virtual HopDong IdHopDongNavigation { get; set; } = null!;

    public virtual NguoiDung? IdNhanVienNavigation { get; set; }

    public virtual Xe IdXeNavigation { get; set; } = null!;
}
