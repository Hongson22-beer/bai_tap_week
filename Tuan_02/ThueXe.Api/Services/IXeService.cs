using ThueXe.Api.Dtos;

namespace ThueXe.Api.Services;

public interface IXeService
{
    Task<IReadOnlyList<XeResponseDto>> GetAllAsync(CancellationToken ct);
    Task<XeResponseDto?> GetByIdAsync(int id, CancellationToken ct);
    Task<XeResponseDto> CreateAsync(XeCreateDto dto, CancellationToken ct);
    Task<XeResponseDto?> UpdateAsync(int id, XeUpdateDto dto, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}