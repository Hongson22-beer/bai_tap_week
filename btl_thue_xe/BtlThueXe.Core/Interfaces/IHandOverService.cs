using BtlThueXe.Core.DTOs.Handovers;

namespace BtlThueXe.Core.Interfaces;

public interface IHandOverService
{
    Task<HandoverResponse> CreateAsync(
        CreateHandoverRequest request);

    Task<HandoverResponse?> GetByIdAsync(int id);

    Task<HandoverResponse?> GetByContractIdAsync(
        int idHopDong);
}