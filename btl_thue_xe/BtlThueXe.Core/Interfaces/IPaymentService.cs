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

    // Lấy thanh toán theo ID
    Task<PaymentResponse?> GetByIdAsync(int id);

    // Lấy danh sách thanh toán của một hợp đồng
    Task<List<PaymentResponse>> GetByContractIdAsync(
        int idHopDong);
}