using BtlThueXe.Core.DTOs.AuditLogs;

namespace BtlThueXe.Core.Interfaces;

public interface IAuditLogService
{
    Task<AuditLogResponse> CreateAsync(
        CreateAuditLogRequest request);

    Task<AuditLogResponse?> GetByIdAsync(long id);

    Task<List<AuditLogResponse>> GetAllAsync();

    Task<List<AuditLogResponse>> GetByUserIdAsync(
        int idNguoiDung);
}