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

    // Khách hàng xem đánh giá của chính hợp đồng mình
    Task<EvaluationResponse?> GetMyByContractIdAsync(int idHopDong, int currentUserId);

    // Danh sách toàn bộ đánh giá
    Task<List<EvaluationResponse>> GetAllAsync();

    // Công khai: chỉ lấy đánh giá đang hiển thị theo xe
    Task<List<EvaluationResponse>> GetVisibleByVehicleIdAsync(int idXe);

    // Admin ẩn/hiện đánh giá
    Task<EvaluationResponse> SetVisibilityAsync(int id, bool hienThi, int currentUserId);
}