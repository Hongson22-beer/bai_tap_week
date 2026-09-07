using BtlThueXe.Core.DTOs.Payments;

namespace BtlThueXe.Core.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponse> CreateAsync(CreatePaymentRequest request);

    Task<List<PaymentResponse>> GetByContractIdAsync(int idHopDong);

    Task<PaymentResponse?> GetByIdAsync(int id);
}