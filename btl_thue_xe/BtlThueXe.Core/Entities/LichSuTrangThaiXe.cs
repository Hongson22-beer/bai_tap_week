using System;
using System.Collections.Generic;

namespace BtlThueXe.Infrastructure;

public partial class LichSuTrangThaiXe
{
    public int Id { get; set; }

    public int IdXe { get; set; }

    public string? TrangThaiCu { get; set; }

    public string TrangThaiMoi { get; set; } = null!;

    public string? LyDo { get; set; }

    public int? IdNguoiThayDoi { get; set; }

    public DateTime ThoiGianThayDoi { get; set; }

    public virtual NguoiDung? IdNguoiThayDoiNavigation { get; set; }

    public virtual Xe IdXeNavigation { get; set; } = null!;
}
