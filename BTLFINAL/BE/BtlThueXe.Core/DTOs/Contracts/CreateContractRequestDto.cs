using System;
using System.ComponentModel.DataAnnotations;

namespace BtlThueXe.Core.DTOs.Contracts
{
    /// <summary>
    /// Request tạo hợp đồng từ yêu cầu thuê đã được APPROVED.
    /// Các thông tin trạng thái, nhân viên lập, thời gian hệ thống
    /// và các giá trị tính toán do Service xử lý.
    /// </summary>
    public class CreateContractRequestDto
    {
        [Required(ErrorMessage = "Mã yêu cầu thuê không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Mã yêu cầu thuê không hợp lệ.")]
        public int IdYeuCauThue { get; set; }

        [Required(ErrorMessage = "Số hợp đồng không được để trống.")]
        [StringLength(
            50,
            ErrorMessage = "Số hợp đồng không vượt quá 50 ký tự.")]
        public string SoHopDong { get; set; } = string.Empty;

        /// <summary>
        /// Đơn giá ngày được Staff xác lập khi lập hợp đồng.
        /// </summary>
        [Range(
            0,
            double.MaxValue,
            ErrorMessage = "Đơn giá ngày phải lớn hơn hoặc bằng 0.")]
        public decimal DonGiaNgay { get; set; }

        /// <summary>
        /// Điều khoản của hợp đồng.
        /// </summary>
        public string? DieuKhoan { get; set; }

        /// <summary>
        /// Thời gian nhận xe dự kiến.
        /// </summary>
        public DateTime ThoiGianNhanDuKien { get; set; }

        /// <summary>
        /// Thời gian trả xe dự kiến.
        /// </summary>
        public DateTime ThoiGianTraDuKien { get; set; }
    }
}