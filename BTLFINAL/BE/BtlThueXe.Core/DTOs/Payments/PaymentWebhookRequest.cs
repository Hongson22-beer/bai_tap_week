using System.ComponentModel.DataAnnotations;

namespace BtlThueXe.Core.DTOs.Payments;

public class PaymentWebhookRequest
{
    [Required(ErrorMessage = "Mã giao dịch không được để trống.")]
    public string MaGiaoDich { get; set; } = string.Empty;

    [Required(ErrorMessage = "Trạng thái thanh toán không được để trống.")]
    public string TrangThai { get; set; } = string.Empty;
}