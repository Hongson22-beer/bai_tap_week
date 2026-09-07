using System;
using System.Collections.Generic;

namespace BtlThueXe.Infrastructure;

public partial class LichSuTrangThaiHopDong
{
    public int Id { get; set; }

    public int IdHopDong { get; set; }

    public string? TrangThaiCu { get; set; }

    public string TrangThaiMoi { get; set; } = null!;

    public int? IdNguoiThayDoi { get; set; }

    public string? LyDo { get; set; }

    public DateTime ThoiGianThayDoi { get; set; }

    public virtual HopDong IdHopDongNavigation { get; set; } = null!;

    public virtual NguoiDung? IdNguoiThayDoiNavigation { get; set; }
}
