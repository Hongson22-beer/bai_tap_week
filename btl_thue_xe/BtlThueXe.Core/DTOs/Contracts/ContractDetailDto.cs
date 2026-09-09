using System;

namespace BtlThueXe.Core.DTOs.Contracts
{
    public class ContractDetailDto
    {
        public int Id { get; set; }
        public int IdYeuCauThue { get; set; }
        public string SoHopDong { get; set; } = string.Empty;
        public int IdNhanVienLap { get; set; }
        public decimal DonGiaNgay { get; set; }
        public int SoNgayThue { get; set; }
        public decimal TienThue { get; set; }
        public decimal TienCoc { get; set; }
        public decimal TongTien { get; set; }
        public string? DieuKhoan { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public DateTime ThoiGianNhanDuKien { get; set; }
        public DateTime ThoiGianTraDuKien { get; set; }
        public DateTime? ThoiGianNhanThucTe { get; set; }
        public DateTime? ThoiGianTraThucTe { get; set; }
        public DateTime ThoiGianTao { get; set; }
        public DateTime ThoiGianCapNhat { get; set; }
    }
}