using BtlThueXe.Core.DTOs.Cancellations;

namespace BtlThueXe.Core.Interfaces;

public interface ICancellationService
{
    Task<CancellationResponse> CreateAsync(
        CreateCancellationRequest request);

    Task<CancellationResponse?> GetByIdAsync(int id);

    Task<List<CancellationResponse>> GetByContractIdAsync(
        int idHopDong);

    Task<CancellationResponse> ProcessAsync(
        int id,
        ProcessCancellationRequest request);
}