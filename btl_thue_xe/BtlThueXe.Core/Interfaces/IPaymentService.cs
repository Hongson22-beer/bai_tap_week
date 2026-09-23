using BtlThueXe.Core.DTOs.Payments;

namespace BtlThueXe.Core.Interfaces;

public interface IPaymentService
{
    // Tạo thanh toán
    Task<PaymentResponse> CreateAsync(
        CreatePaymentRequest request,
        int currentUserId,
        string? currentUserRole);

    // Xử lý kết quả thanh toán chuyển khoản / mô phỏng webhook
    Task<PaymentResponse> ProcessWebhookAsync(
        PaymentWebhookRequest request);

    // Nhân viên xử lý hoàn tiền cho giao dịch REFUND_PENDING
    Task<PaymentResponse> ProcessRefundAsync(int paymentId, int currentUserId);

    // Lấy thanh toán theo ID
    Task<PaymentResponse?> GetByIdAsync(int id);

    // Lấy danh sách thanh toán của một hợp đồng
    Task<List<PaymentResponse>> GetByContractIdAsync(
        int idHopDong);
}