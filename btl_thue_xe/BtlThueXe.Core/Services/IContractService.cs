using System.Collections.Generic;
using System.Threading.Tasks;
using BtlThueXe.Core.DTOs.Contracts;

namespace BtlThueXe.Core.Services
{
    public interface IContractService
    {
        Task<ContractResponseDto> CreateContractAsync(CreateContractRequestDto request, int staffUserId, string? currentUserRole);
        Task<ContractDetailDto?> GetContractByIdAsync(int id, int currentUserId, string? currentUserRole);
        Task<ContractResponseDto> SendContractAsync(int contractId, int staffUserId, string? currentUserRole);
        Task<ContractResponseDto> ConfirmContractAsync(int contractId, int currentUserId, string? currentUserRole);
        Task<ContractResponseDto> RejectContractAsync(int contractId, RejectContractRequestDto request, int currentUserId, string? currentUserRole);
        Task<List<ContractHistoryDto>> GetContractHistoryAsync(int contractId, int currentUserId, string? currentUserRole);
    }
}