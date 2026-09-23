using BtlThueXe.Core.DTOs.Evaluations;

namespace BtlThueXe.Core.Interfaces;

public interface IEvaluationService
{
    // Khách hàng tạo đánh giá
    Task<EvaluationResponse> CreateAsync(
        CreateEvaluationRequest request,
        int currentUserId);

    // Xem đánh giá theo ID
    Task<EvaluationResponse?> GetByIdAsync(int id);

    // Xem đánh giá của một hợp đồng
    Task<EvaluationResponse?> GetByContractIdAsync(
        int idHopDong);

    // Danh sách toàn bộ đánh giá
    Task<List<EvaluationResponse>> GetAllAsync();
}