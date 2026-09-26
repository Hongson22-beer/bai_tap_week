using BtlThueXe.Core.DTOs.Handovers;

namespace BtlThueXe.Core.Interfaces;

public interface IHandOverService
{
    Task<HandoverResponse> CreateAsync(CreateHandoverRequest request, int currentUserId);
    Task<HandoverResponse?> GetByIdAsync(int id);
    Task<HandoverResponse?> GetByContractIdAsync(int idHopDong);
    Task<HandoverResponse?> GetByContractForUserAsync(int idHopDong, int currentUserId, bool isStaff);
    Task<HandoverResponse> ConfirmReceivedAsync(int idHopDong, int currentUserId);
}
