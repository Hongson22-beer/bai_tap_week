using System;
using System.Collections.Generic;

namespace BtlThueXe.Infrastructure;

public partial class ThanhToan
{
    public int Id { get; set; }

    public int IdHopDong { get; set; }

    public string LoaiThanhToan { get; set; } = null!;

    public decimal SoTien { get; set; }

    public string PhuongThuc { get; set; } = null!;

    public string? MaGiaoDich { get; set; }

    public string TrangThai { get; set; } = null!;

    public string? GhiChu { get; set; }

    public DateTime? ThoiGianThanhToan { get; set; }

    public virtual HopDong IdHopDongNavigation { get; set; } = null!;
}
