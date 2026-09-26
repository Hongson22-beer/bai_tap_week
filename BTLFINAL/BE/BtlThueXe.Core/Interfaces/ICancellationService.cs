using BtlThueXe.Core.DTOs.Cancellations;

namespace BtlThueXe.Core.Interfaces;

public interface ICancellationService
{
    // Khách hàng tạo yêu cầu hủy hợp đồng
    Task<CancellationResponse> CreateAsync(
        CreateCancellationRequest request,
        int currentUserId);

    // Nhân viên duyệt / từ chối yêu cầu hủy
    Task<CancellationResponse> ProcessAsync(
        int id,
        ProcessCancellationRequest request,
        int currentUserId);

    // Xem yêu cầu hủy theo ID
    Task<CancellationResponse?> GetByIdAsync(int id);

    // Xem các yêu cầu hủy của một hợp đồng
    Task<List<CancellationResponse>> GetByContractIdAsync(
        int idHopDong);

    // Khách hàng xem yêu cầu hủy của chính hợp đồng mình
    Task<List<CancellationResponse>> GetMyByContractIdAsync(
        int idHopDong, int currentUserId);
}