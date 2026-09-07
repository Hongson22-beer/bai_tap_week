using System;
using System.Collections.Generic;

namespace BtlThueXe.Infrastructure;

public partial class BanGiaoXe
{
    public int Id { get; set; }

    public int IdHopDong { get; set; }

    public int IdXe { get; set; }

    public int SoKm { get; set; }

    public decimal MucNhienLieu { get; set; }

    public string? TinhTrangXe { get; set; }

    public string? GhiChu { get; set; }

    public int? IdNhanVien { get; set; }

    public DateTime ThoiGianGiao { get; set; }

    public virtual HopDong IdHopDongNavigation { get; set; } = null!;

    public virtual NguoiDung? IdNhanVienNavigation { get; set; }

    public virtual Xe IdXeNavigation { get; set; } = null!;
}
