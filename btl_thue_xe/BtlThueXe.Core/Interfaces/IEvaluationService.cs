using BtlThueXe.Core.DTOs.Evaluations;

namespace BtlThueXe.Core.Interfaces;

public interface IEvaluationService
{
    Task<EvaluationResponse> CreateAsync(
        CreateEvaluationRequest request);

    Task<EvaluationResponse?> GetByIdAsync(int id);

    Task<EvaluationResponse?> GetByContractIdAsync(
        int idHopDong);

    Task<List<EvaluationResponse>> GetAllAsync();
}