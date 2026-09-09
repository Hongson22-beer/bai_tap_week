using System;
using System.ComponentModel.DataAnnotations;

namespace BtlThueXe.Core.DTOs.Rentals
{
    // =========================================================================
    // 1. CreateRentalRequestDto - Khách hàng tạo yêu cầu thuê ONLINE
    // =========================================================================
    public class CreateRentalRequestDto
    {
        [Required(ErrorMessage = "Mã xe không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Mã xe không hợp lệ.")]
        public int IdXe { get; set; }

        [Required(ErrorMessage = "Thời gian nhận không được để trống.")]
        public DateTime ThoiGianNhan { get; set; }

        [Required(ErrorMessage = "Thời gian trả dự kiến không được để trống.")]
        public DateTime ThoiGianTraDuKien { get; set; }
    }

    // =========================================================================
    // 2. CreateOfflineRentalRequestDto - Nhân viên tạo yêu cầu thuê OFFLINE
    // =========================================================================
    public class CreateOfflineRentalRequestDto
    {
        [Required(ErrorMessage = "Mã khách hàng không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Mã khách hàng không hợp lệ.")]
        public int IdKhachHang { get; set; }

        [Required(ErrorMessage = "Mã xe không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Mã xe không hợp lệ.")]
        public int IdXe { get; set; }

        [Required(ErrorMessage = "Thời gian nhận không được để trống.")]
        public DateTime ThoiGianNhan { get; set; }

        [Required(ErrorMessage = "Thời gian trả dự kiến không được để trống.")]
        public DateTime ThoiGianTraDuKien { get; set; }
    }

    // =========================================================================
    // 3. RentalResponseDto - Phản hồi dữ liệu tổng quan của Yêu cầu thuê
    // =========================================================================
    public class RentalResponseDto
    {
        public int Id { get; set; }
        public int IdKhachHang { get; set; }
        public int IdXe { get; set; }
        public string Nguon { get; set; } = string.Empty;
        public DateTime ThoiGianNhan { get; set; }
        public DateTime ThoiGianTraDuKien { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public decimal TienUocTinh { get; set; }
        public decimal TienCoc { get; set; }
        public string? LyDoTuChoi { get; set; }
        public string? LyDoHuy { get; set; }
        public int? IdNhanVienXuLy { get; set; }
        public DateTime ThoiGianTao { get; set; }
        public DateTime ThoiGianCapNhat { get; set; }
    }

    // =========================================================================
    // 4. RentalDetailDto - Dùng cho API xem chi tiết Yêu cầu thuê
    // =========================================================================
    public class RentalDetailDto
    {
        public int Id { get; set; }
        public int IdKhachHang { get; set; }
        public int IdXe { get; set; }
        public string Nguon { get; set; } = string.Empty;
        public DateTime ThoiGianNhan { get; set; }
        public DateTime ThoiGianTraDuKien { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public decimal TienUocTinh { get; set; }
        public decimal TienCoc { get; set; }
        public string? LyDoTuChoi { get; set; }
        public string? LyDoHuy { get; set; }
        public int? IdNhanVienXuLy { get; set; }
        public DateTime ThoiGianTao { get; set; }
        public DateTime ThoiGianCapNhat { get; set; }
    }

    // =========================================================================
    // 5. RejectRentalDto - Nhân viên nhập lý do từ chối yêu cầu thuê
    // =========================================================================
    public class RejectRentalDto
    {
        [Required(ErrorMessage = "Lý do từ chối không được để trống.")]
        [StringLength(500, ErrorMessage = "Lý do từ chối không được vượt quá 500 ký tự.")]
        public string LyDoTuChoi { get; set; } = string.Empty;
    }

    // =========================================================================
    // 6. CancelRentalDto - DTO nhận thông tin lý do hủy yêu cầu thuê
    // Note: Quyền kiểm tra ai được phép hủy sẽ do Service/Authorization xử lý.
    // =========================================================================
    public class CancelRentalDto
    {
        [Required(ErrorMessage = "Lý do hủy không được để trống.")]
        [StringLength(500, ErrorMessage = "Lý do hủy không được vượt quá 500 ký tự.")]
        public string LyDoHuy { get; set; } = string.Empty;
    }

    // =========================================================================
    // 7. RentalFilterDto - Dùng lọc và phân trang danh sách Yêu cầu thuê
    // =========================================================================
    public class RentalFilterDto
    {
        public string? TrangThai { get; set; }
        public string? Nguon { get; set; }
        public int? IdKhachHang { get; set; }
        public int? IdXe { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Trang phải lớn hơn 0.")]
        public int Page { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "Kích thước trang từ 1 đến 100.")]
        public int PageSize { get; set; } = 20;
    }
}