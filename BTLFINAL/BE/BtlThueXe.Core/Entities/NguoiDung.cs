using System;
using System.Collections.Generic;


namespace BtlThueXe.Infrastructure;

public partial class NguoiDung
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string MatKhau { get; set; } = null!;

    public string HoTen { get; set; } = null!;

    public string? SoDienThoai { get; set; }

    public bool DangHoatDong { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<BanGiaoXe> BanGiaoXes { get; set; } = new List<BanGiaoXe>();

    public virtual ICollection<HopDong> HopDongs { get; set; } = new List<HopDong>();

    public virtual KhachHang? KhachHang { get; set; }

    public virtual ICollection<LichSuTrangThaiHopDong> LichSuTrangThaiHopDongs { get; set; } = new List<LichSuTrangThaiHopDong>();

    public virtual ICollection<LichSuTrangThaiXe> LichSuTrangThaiXes { get; set; } = new List<LichSuTrangThaiXe>();

    public virtual ICollection<TraXe> TraXes { get; set; } = new List<TraXe>();

    public virtual ICollection<YeuCauGiaHan> YeuCauGiaHanIdNguoiXuLyNavigations { get; set; } = new List<YeuCauGiaHan>();

    public virtual ICollection<YeuCauGiaHan> YeuCauGiaHanIdNguoiYeuCauNavigations { get; set; } = new List<YeuCauGiaHan>();

    public virtual ICollection<YeuCauHuyHopDong> YeuCauHuyHopDongIdNguoiXuLyNavigations { get; set; } = new List<YeuCauHuyHopDong>();

    public virtual ICollection<YeuCauHuyHopDong> YeuCauHuyHopDongIdNguoiYeuCauNavigations { get; set; } = new List<YeuCauHuyHopDong>();

    public virtual ICollection<YeuCauThue> YeuCauThues { get; set; } = new List<YeuCauThue>();

    public virtual ICollection<VaiTro> IdVaiTros { get; set; } = new List<VaiTro>();
}
