using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema; // <-- 1. Thêm namespace này

namespace BtlThueXe.Infrastructure;

public partial class HopDong
{
    public int Id { get; set; }

    public int IdYeuCauThue { get; set; }

    public string SoHopDong { get; set; } = null!;

    public int IdNhanVienLap { get; set; }

    public decimal DonGiaNgay { get; set; }

    public int SoNgayThue { get; set; }

    public decimal TienThue { get; set; }

    public decimal TienCoc { get; set; }

    public decimal TongTien { get; set; }

    public string? DieuKhoan { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime ThoiGianNhanDuKien { get; set; }

    public DateTime ThoiGianTraDuKien { get; set; }

    public DateTime? ThoiGianNhanThucTe { get; set; }

    public DateTime? ThoiGianTraThucTe { get; set; }

    public DateTime ThoiGianTao { get; set; }

    public DateTime ThoiGianCapNhat { get; set; }

    public virtual BanGiaoXe? BanGiaoXe { get; set; }

    public virtual DanhGia? DanhGia { get; set; }

    // 2. Bổ sung [ForeignKey] để ánh xạ đúng cột id_nhan_vien_lap
    [ForeignKey(nameof(IdNhanVienLap))]
    public virtual NguoiDung IdNhanVienLapNavigation { get; set; } = null!;

    // 3. Bổ sung [ForeignKey] để ánh xạ đúng cột id_yeu_cau_thue
    [ForeignKey(nameof(IdYeuCauThue))]
    public virtual YeuCauThue IdYeuCauThueNavigation { get; set; } = null!;

    public virtual ICollection<LichSuTrangThaiHopDong> LichSuTrangThaiHopDongs { get; set; }
        = new List<LichSuTrangThaiHopDong>();

    public virtual ICollection<ThanhToan> ThanhToans { get; set; }
        = new List<ThanhToan>();

    public virtual TraXe? TraXe { get; set; }

    public virtual ICollection<YeuCauGiaHan> YeuCauGiaHans { get; set; }
        = new List<YeuCauGiaHan>();

    public virtual ICollection<YeuCauHuyHopDong> YeuCauHuyHopDongs { get; set; }
        = new List<YeuCauHuyHopDong>();
}