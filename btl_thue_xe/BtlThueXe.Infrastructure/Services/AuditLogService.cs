using BtlThueXe.Core.DTOs.AuditLogs;
using BtlThueXe.Infrastructure;
using BtlThueXe.Core.Interfaces;
using BtlThueXe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BtlThueXe.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;

    public AuditLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLogResponse> CreateAsync(
        CreateAuditLogRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.HanhDong))
        {
            throw new ArgumentException(
                "Hành động không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(request.LoaiDoiTuong))
        {
            throw new ArgumentException(
                "Loại đối tượng không được để trống.");
        }

        if (request.IdNguoiDung.HasValue &&
            request.IdNguoiDung.Value <= 0)
        {
            throw new ArgumentException(
                "ID người dùng không hợp lệ.");
        }

        if (request.IdDoiTuong.HasValue &&
            request.IdDoiTuong.Value <= 0)
        {
            throw new ArgumentException(
                "ID đối tượng không hợp lệ.");
        }

        var auditLog = new AuditLog
        {
            IdNguoiDung = request.IdNguoiDung,

            HanhDong = request.HanhDong.Trim(),

            LoaiDoiTuong = request.LoaiDoiTuong.Trim(),

            IdDoiTuong = request.IdDoiTuong,

            DuLieuCu = request.DuLieuCu,

            DuLieuMoi = request.DuLieuMoi,

            MoTa = string.IsNullOrWhiteSpace(request.MoTa)
                ? null
                : request.MoTa.Trim(),

            IpAddress = string.IsNullOrWhiteSpace(request.IpAddress)
                ? null
                : request.IpAddress.Trim(),

            ThoiGian = DateTime.UtcNow
        };

        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync();

        return MapToResponse(auditLog);
    }

    public async Task<AuditLogResponse?> GetByIdAsync(long id)
    {
        if (id <= 0)
            return null;

        var auditLog = await _context.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        return auditLog == null
            ? null
            : MapToResponse(auditLog);
    }

    public async Task<List<AuditLogResponse>> GetAllAsync()
    {
        var auditLogs = await _context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.ThoiGian)
            .ToListAsync();

        return auditLogs
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<List<AuditLogResponse>> GetByUserIdAsync(
        int idNguoiDung)
    {
        var auditLogs = await _context.AuditLogs
            .AsNoTracking()
            .Where(x => x.IdNguoiDung == idNguoiDung)
            .OrderByDescending(x => x.ThoiGian)
            .ToListAsync();

        return auditLogs
            .Select(MapToResponse)
            .ToList();
    }

    private static AuditLogResponse MapToResponse(
        AuditLog auditLog)
    {
        return new AuditLogResponse
        {
            Id = auditLog.Id,
            IdNguoiDung = auditLog.IdNguoiDung,
            HanhDong = auditLog.HanhDong,
            LoaiDoiTuong = auditLog.LoaiDoiTuong,
            IdDoiTuong = auditLog.IdDoiTuong,
            DuLieuCu = auditLog.DuLieuCu,
            DuLieuMoi = auditLog.DuLieuMoi,
            MoTa = auditLog.MoTa,
            IpAddress = auditLog.IpAddress,
            ThoiGian = auditLog.ThoiGian
        };
    }
}