using BtlThueXe.Core.DTOs.Returns;

namespace BtlThueXe.Core.Interfaces;

public interface IReturnService
{
    Task<ReturnResponse> CreateAsync(
        CreateReturnRequest request);

    Task<ReturnResponse?> GetByIdAsync(int id);

    Task<ReturnResponse?> GetByContractIdAsync(
        int idHopDong);
}