using BtlThueXe.Core.DTOs.Extensions;

namespace BtlThueXe.Core.Interfaces;

public interface IExtensionService
{
    Task<ExtensionResponse> CreateAsync(
        CreateExtensionRequest request);

    Task<ExtensionResponse?> GetByIdAsync(int id);

    Task<List<ExtensionResponse>> GetByContractIdAsync(
        int idHopDong);

    Task<ExtensionResponse> ProcessAsync(
    int id,
    ProcessExtensionRequest request);
}