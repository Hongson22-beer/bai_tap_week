using System;
using System.Collections.Generic;

namespace BtlThueXe.Infrastructure;

public partial class KhachHang
{
    public int Id { get; set; }
    public int IdNguoiDung { get; set; }
    public string SoCccd { get; set; } = null!;
    public bool CccdDaXacMinh { get; set; }
    public string? AnhCccdMatTruoc { get; set; }
    public string? AnhCccdMatSau { get; set; }
    public string? SoGplx { get; set; }
    public string? AnhGplx { get; set; }
    public bool GplxDaXacMinh { get; set; }
    public string? NganHang { get; set; }
    public string? SoTaiKhoan { get; set; }
    public string? ChuTaiKhoan { get; set; }
    public string? DiaChi { get; set; }
    public DateOnly? NgaySinh { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public virtual ICollection<DanhGia> DanhGia { get; set; } = new List<DanhGia>();
    public virtual NguoiDung IdNguoiDungNavigation { get; set; } = null!;
    public virtual ICollection<YeuCauThue> YeuCauThues { get; set; } = new List<YeuCauThue>();
}
