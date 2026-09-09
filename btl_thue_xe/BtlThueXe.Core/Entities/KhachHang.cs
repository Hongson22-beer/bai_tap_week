using System;
using System.Collections.Generic;


namespace BtlThueXe.Infrastructure;

public partial class KhachHang
{
    public int Id { get; set; }

    public int IdNguoiDung { get; set; }

    public string SoCccd { get; set; } = null!;

    public bool CccdDaXacMinh { get; set; }

    public string? DiaChi { get; set; }

    public DateOnly? NgaySinh { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<DanhGia> DanhGia { get; set; } = new List<DanhGia>();

    public virtual NguoiDung IdNguoiDungNavigation { get; set; } = null!;

    public virtual ICollection<YeuCauThue> YeuCauThues { get; set; } = new List<YeuCauThue>();
}
