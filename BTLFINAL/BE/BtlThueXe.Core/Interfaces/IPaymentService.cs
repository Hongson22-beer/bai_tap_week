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

    // Khách báo đã chuyển khoản: PENDING -> WAITING_CONFIRMATION
    Task<PaymentResponse> CustomerMarkPaidAsync(int paymentId, int currentUserId);

    // Nhân viên mô phỏng giao dịch tiền vào để demo đối soát.
    Task<PaymentResponse> SimulateBankReceivedAsync(int paymentId, int currentUserId);

    // Nhân viên xác nhận/từ chối sau đối soát.
    Task<PaymentResponse> VerifyTransferAsync(int paymentId, bool confirmed, int currentUserId);

    // Khách được xem trạng thái payment của chính hợp đồng mình.
    Task<PaymentResponse> GetStatusForUserAsync(int paymentId, int currentUserId, string? currentUserRole);

    // Nhân viên xử lý hoàn tiền cho giao dịch REFUND_PENDING
    Task<PaymentResponse> ProcessRefundAsync(int paymentId, int currentUserId);

    // Lấy thanh toán theo ID
    Task<PaymentResponse?> GetByIdAsync(int id);

    // Lấy danh sách thanh toán của một hợp đồng
    Task<List<PaymentResponse>> GetByContractIdAsync(
        int idHopDong);
}