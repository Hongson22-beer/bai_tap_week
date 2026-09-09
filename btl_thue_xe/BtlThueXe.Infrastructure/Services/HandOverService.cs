using BtlThueXe.Core.DTOs.Handovers;
using BtlThueXe.Core.DTOs.AuditLogs;
using BtlThueXe.Infrastructure;
using BtlThueXe.Core.Interfaces;
using BtlThueXe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BtlThueXe.Infrastructure.Services;

public class HandOverService : IHandOverService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public HandOverService(
        ApplicationDbContext context,
        IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    public async Task<HandoverResponse> CreateAsync(
        CreateHandoverRequest request)
    {
        if (request.IdHopDong <= 0)
        {
            throw new ArgumentException(
                "ID hợp đồng không hợp lệ.");
        }

        if (request.IdXe <= 0)
        {
            throw new ArgumentException(
                "ID xe không hợp lệ.");
        }

        if (request.SoKm < 0)
        {
            throw new ArgumentException(
                "Số km không được âm.");
        }

        if (request.MucNhienLieu < 0 ||
            request.MucNhienLieu > 100)
        {
            throw new ArgumentException(
                "Mức nhiên liệu phải từ 0 đến 100.");
        }

        var handoverExists =
            await _context.BanGiaoXes
                .AnyAsync(x =>
                    x.IdHopDong == request.IdHopDong);

        if (handoverExists)
        {
            throw new ArgumentException(
                "Hợp đồng này đã được bàn giao xe.");
        }

        var handover = new BanGiaoXe
        {
            IdHopDong = request.IdHopDong,

            IdXe = request.IdXe,

            SoKm = request.SoKm,

            MucNhienLieu = request.MucNhienLieu,

            TinhTrangXe =
                string.IsNullOrWhiteSpace(
                    request.TinhTrangXe)
                    ? null
                    : request.TinhTrangXe.Trim(),

            GhiChu =
                string.IsNullOrWhiteSpace(
                    request.GhiChu)
                    ? null
                    : request.GhiChu.Trim(),

            IdNhanVien = request.IdNhanVien,

            ThoiGianGiao = DateTime.UtcNow
        };

        // =====================================================
        // LƯU BÀN GIAO XE
        // =====================================================
        _context.BanGiaoXes.Add(handover);

        await _context.SaveChangesAsync();

        // =====================================================
        // GHI AUDIT LOG TỰ ĐỘNG
        // =====================================================
        await _auditLogService.CreateAsync(
            new CreateAuditLogRequest
            {
                // Tạm thời dùng nhân viên trong request.
                // Sau khi merge JWT/RBAC sẽ lấy user từ token.
                IdNguoiDung = request.IdNhanVien,

                HanhDong = "HANDOVER_CREATED",

                LoaiDoiTuong = "HANDOVER",

                IdDoiTuong = handover.Id,

                DuLieuCu = null,

                DuLieuMoi =
                    $"{{\"idHopDong\":{handover.IdHopDong}," +
                    $"\"idXe\":{handover.IdXe}," +
                    $"\"soKm\":{handover.SoKm}," +
                    $"\"mucNhienLieu\":{handover.MucNhienLieu}}}",

                MoTa =
                    $"Bàn giao xe #{handover.IdXe} " +
                    $"cho hợp đồng #{handover.IdHopDong}",

                IpAddress = null
            });

        return MapToResponse(handover);
    }

    public async Task<HandoverResponse?>
        GetByIdAsync(int id)
    {
        if (id <= 0)
            return null;

        var handover =
            await _context.BanGiaoXes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == id);

        if (handover == null)
            return null;

        return MapToResponse(handover);
    }

    public async Task<HandoverResponse?>
        GetByContractIdAsync(int idHopDong)
    {
        if (idHopDong <= 0)
            return null;

        var handover =
            await _context.BanGiaoXes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.IdHopDong == idHopDong);

        if (handover == null)
            return null;

        return MapToResponse(handover);
    }

    private static HandoverResponse MapToResponse(
        BanGiaoXe handover)
    {
        return new HandoverResponse
        {
            Id = handover.Id,

            IdHopDong = handover.IdHopDong,

            IdXe = handover.IdXe,

            SoKm = handover.SoKm,

            MucNhienLieu =
                handover.MucNhienLieu,

            TinhTrangXe =
                handover.TinhTrangXe,

            GhiChu =
                handover.GhiChu,

            IdNhanVien =
                handover.IdNhanVien,

            ThoiGianGiao =
                handover.ThoiGianGiao
        };
    }
}