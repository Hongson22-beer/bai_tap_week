using BtlThueXe.Core.DTOs.Returns;
using BtlThueXe.Core.DTOs.AuditLogs;
using BtlThueXe.Core.Entities;
using BtlThueXe.Core.Interfaces;
using BtlThueXe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BtlThueXe.Infrastructure.Services;

public class ReturnService : IReturnService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public ReturnService(
        ApplicationDbContext context,
        IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    // =========================================================
    // TẠO PHIẾU TRẢ XE
    // =========================================================
    public async Task<ReturnResponse> CreateAsync(
        CreateReturnRequest request)
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

        if (request.PhiTraMuon < 0)
        {
            throw new ArgumentException(
                "Phí trả muộn không được âm.");
        }

        if (request.PhiPhatSinh < 0)
        {
            throw new ArgumentException(
                "Phí phát sinh không được âm.");
        }

        // Một hợp đồng chỉ được trả xe một lần
        var returnExists = await _context.TraXes
            .AnyAsync(x =>
                x.IdHopDong == request.IdHopDong);

        if (returnExists)
        {
            throw new ArgumentException(
                "Hợp đồng này đã được trả xe.");
        }

        // Kiểm tra hợp đồng đã bàn giao xe
        var handover = await _context.BanGiaoXes
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.IdHopDong == request.IdHopDong);

        if (handover == null)
        {
            throw new ArgumentException(
                "Hợp đồng chưa được bàn giao xe.");
        }

        // Xe trả phải đúng xe đã bàn giao
        if (handover.IdXe != request.IdXe)
        {
            throw new ArgumentException(
                "Xe trả không khớp với xe đã bàn giao.");
        }

        // Số km khi trả >= số km lúc giao
        if (request.SoKm < handover.SoKm)
        {
            throw new ArgumentException(
                $"Số km khi trả không được nhỏ hơn số km lúc bàn giao ({handover.SoKm} km).");
        }

        DateTime thoiGianTraDuKien =
            ToUtc(request.ThoiGianTraDuKien);

        DateTime thoiGianTraThucTe =
            request.ThoiGianTraThucTe.HasValue
                ? ToUtc(request.ThoiGianTraThucTe.Value)
                : DateTime.UtcNow;

        decimal tongPhi =
            request.PhiTraMuon +
            request.PhiPhatSinh;

        var returnEntity = new TraXe
        {
            IdHopDong = request.IdHopDong,

            IdXe = request.IdXe,

            ThoiGianTraDuKien =
                thoiGianTraDuKien,

            ThoiGianTraThucTe =
                thoiGianTraThucTe,

            SoKm = request.SoKm,

            MucNhienLieu =
                request.MucNhienLieu,

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

            PhiTraMuon =
                request.PhiTraMuon,

            PhiPhatSinh =
                request.PhiPhatSinh,

            TongPhiPhatSinh =
                tongPhi,

            IdNhanVien =
                request.IdNhanVien,

            TrangThai =
                "COMPLETED",

            ThoiGianTao =
                DateTime.UtcNow
        };

        // =====================================================
        // LƯU PHIẾU TRẢ XE
        // =====================================================
        _context.TraXes.Add(returnEntity);

        await _context.SaveChangesAsync();

        // =====================================================
        // GHI AUDIT LOG TỰ ĐỘNG
        // =====================================================
        var duLieuMoi = JsonSerializer.Serialize(new
        {
            idHopDong = returnEntity.IdHopDong,
            idXe = returnEntity.IdXe,
            soKm = returnEntity.SoKm,
            mucNhienLieu = returnEntity.MucNhienLieu,
            phiTraMuon = returnEntity.PhiTraMuon,
            phiPhatSinh = returnEntity.PhiPhatSinh,
            tongPhiPhatSinh = returnEntity.TongPhiPhatSinh,
            trangThai = returnEntity.TrangThai
        });

        await _auditLogService.CreateAsync(
            new CreateAuditLogRequest
            {
                IdNguoiDung =
                    request.IdNhanVien,

                HanhDong =
                    "RETURN_CREATED",

                LoaiDoiTuong =
                    "RETURN",

                IdDoiTuong =
                    returnEntity.Id,

                DuLieuCu =
                    null,

                DuLieuMoi =
                    duLieuMoi,

                MoTa =
                    $"Trả xe #{returnEntity.IdXe} " +
                    $"cho hợp đồng #{returnEntity.IdHopDong}",

                IpAddress =
                    null
            });

        return MapToResponse(returnEntity);
    }

    // =========================================================
    // LẤY PHIẾU TRẢ XE THEO ID
    // =========================================================
    public async Task<ReturnResponse?>
        GetByIdAsync(int id)
    {
        if (id <= 0)
            return null;

        var returnEntity =
            await _context.TraXes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == id);

        if (returnEntity == null)
            return null;

        return MapToResponse(returnEntity);
    }

    // =========================================================
    // LẤY PHIẾU TRẢ XE THEO HỢP ĐỒNG
    // =========================================================
    public async Task<ReturnResponse?>
        GetByContractIdAsync(int idHopDong)
    {
        if (idHopDong <= 0)
            return null;

        var returnEntity =
            await _context.TraXes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.IdHopDong == idHopDong);

        if (returnEntity == null)
            return null;

        return MapToResponse(returnEntity);
    }

    // =========================================================
    // CHUYỂN DATETIME SANG UTC
    // =========================================================
    private static DateTime ToUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            return value;
        }

        if (value.Kind == DateTimeKind.Local)
        {
            return value.ToUniversalTime();
        }

        return DateTime.SpecifyKind(
            value,
            DateTimeKind.Utc);
    }

    // =========================================================
    // ENTITY -> RESPONSE DTO
    // =========================================================
    private static ReturnResponse MapToResponse(
        TraXe returnEntity)
    {
        return new ReturnResponse
        {
            Id = returnEntity.Id,

            IdHopDong =
                returnEntity.IdHopDong,

            IdXe =
                returnEntity.IdXe,

            ThoiGianTraDuKien =
                returnEntity.ThoiGianTraDuKien,

            ThoiGianTraThucTe =
                returnEntity.ThoiGianTraThucTe,

            SoKm =
                returnEntity.SoKm,

            MucNhienLieu =
                returnEntity.MucNhienLieu,

            TinhTrangXe =
                returnEntity.TinhTrangXe,

            GhiChu =
                returnEntity.GhiChu,

            PhiTraMuon =
                returnEntity.PhiTraMuon,

            PhiPhatSinh =
                returnEntity.PhiPhatSinh,

            TongPhiPhatSinh =
                returnEntity.TongPhiPhatSinh,

            IdNhanVien =
                returnEntity.IdNhanVien,

            TrangThai =
                returnEntity.TrangThai,

            ThoiGianTao =
                returnEntity.ThoiGianTao
        };
    }
}