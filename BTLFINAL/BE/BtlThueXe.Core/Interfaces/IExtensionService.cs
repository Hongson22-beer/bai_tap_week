using BtlThueXe.Core.DTOs.Extensions;

namespace BtlThueXe.Core.Interfaces;

public interface IExtensionService
{
    // Khách hàng tạo yêu cầu gia hạn
    Task<ExtensionResponse> CreateAsync(
        CreateExtensionRequest request,
        int currentUserId);

    // Nhân viên duyệt / từ chối yêu cầu
    Task<ExtensionResponse> ProcessAsync(
        int id,
        ProcessExtensionRequest request,
        int currentUserId);

    // Xem yêu cầu theo ID
    Task<ExtensionResponse?> GetByIdAsync(int id);

    // Xem các yêu cầu gia hạn của một hợp đồng
    Task<List<ExtensionResponse>> GetByContractIdAsync(
        int idHopDong);

    // Khách hàng xem lịch sử gia hạn của chính hợp đồng mình
    Task<List<ExtensionResponse>> GetMyByContractIdAsync(
        int idHopDong, int currentUserId);
}