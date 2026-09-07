using System;
using System.Collections.Generic;

namespace BtlThueXe.Infrastructure;

public partial class AuditLog
{
    public long Id { get; set; }

    public int? IdNguoiDung { get; set; }

    public string HanhDong { get; set; } = null!;

    public string LoaiDoiTuong { get; set; } = null!;

    public int? IdDoiTuong { get; set; }

    public string? DuLieuCu { get; set; }

    public string? DuLieuMoi { get; set; }

    public string? MoTa { get; set; }

    public string? IpAddress { get; set; }

    public DateTime ThoiGian { get; set; }

    public virtual NguoiDung? IdNguoiDungNavigation { get; set; }
}
