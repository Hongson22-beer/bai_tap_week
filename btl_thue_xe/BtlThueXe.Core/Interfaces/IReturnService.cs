using BtlThueXe.Core.DTOs.Returns;

namespace BtlThueXe.Core.Interfaces;

public interface IReturnService
{
    Task<ReturnResponse> CreateAsync(
        CreateReturnRequest request,
        int currentUserId);

    Task<ReturnResponse?> GetByIdAsync(int id);

    Task<ReturnResponse?> GetByContractIdAsync(
        int idHopDong);
}