using BtlThueXe.Core.DTOs.Cancellations;
using BtlThueXe.Core.DTOs.AuditLogs;
using BtlThueXe.Infrastructure;
using BtlThueXe.Core.Interfaces;
using BtlThueXe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BtlThueXe.Infrastructure.Services;

public class CancellationService : ICancellationService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public CancellationService(
        ApplicationDbContext context,
        IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    // =========================================================
    // TẠO YÊU CẦU HỦY HỢP ĐỒNG
    // =========================================================
    public async Task<CancellationResponse> CreateAsync(
        CreateCancellationRequest request)
    {
        if (request.IdHopDong <= 0)
        {
            throw new ArgumentException(
                "ID hợp đồng không hợp lệ.");
        }

        if (request.IdNguoiYeuCau <= 0)
        {
            throw new ArgumentException(
                "ID người yêu cầu không hợp lệ.");
        }

        if (request.SoTienHoanDuKien < 0)
        {
            throw new ArgumentException(
                "Số tiền hoàn dự kiến không được âm.");
        }

        if (request.SoTienPhat < 0)
        {
            throw new ArgumentException(
                "Số tiền phạt không được âm.");
        }

        // Không cho tạo nhiều yêu cầu PENDING
        // cho cùng một hợp đồng.
        var pendingExists =
            await _context.YeuCauHuyHopDongs
                .AnyAsync(x =>
                    x.IdHopDong == request.IdHopDong &&
                    x.TrangThai == "PENDING");

        if (pendingExists)
        {
            throw new ArgumentException(
                "Hợp đồng này đang có yêu cầu hủy chờ xử lý.");
        }

        var cancellation = new YeuCauHuyHopDong
        {
            IdHopDong =
                request.IdHopDong,

            IdNguoiYeuCau =
                request.IdNguoiYeuCau,

            LyDo =
                string.IsNullOrWhiteSpace(request.LyDo)
                    ? null
                    : request.LyDo.Trim(),

            TrangThai =
                "PENDING",

            SoTienHoanDuKien =
                request.SoTienHoanDuKien,

            SoTienPhat =
                request.SoTienPhat,

            IdNguoiXuLy =
                null,

            ThoiGianTao =
                DateTime.UtcNow,

            ThoiGianXuLy =
                null
        };

        // =====================================================
        // LƯU YÊU CẦU HỦY
        // =====================================================
        _context.YeuCauHuyHopDongs.Add(cancellation);

        await _context.SaveChangesAsync();

        // =====================================================
        // AUDIT: TẠO YÊU CẦU HỦY
        // =====================================================
        var duLieuMoi = JsonSerializer.Serialize(new
        {
            idHopDong =
                cancellation.IdHopDong,

            lyDo =
                cancellation.LyDo,

            soTienHoanDuKien =
                cancellation.SoTienHoanDuKien,

            soTienPhat =
                cancellation.SoTienPhat,

            trangThai =
                cancellation.TrangThai
        });

        await _auditLogService.CreateAsync(
            new CreateAuditLogRequest
            {
                IdNguoiDung =
                    cancellation.IdNguoiYeuCau,

                HanhDong =
                    "CANCELLATION_CREATED",

                LoaiDoiTuong =
                    "CANCELLATION",

                IdDoiTuong =
                    cancellation.Id,

                DuLieuCu =
                    null,

                DuLieuMoi =
                    duLieuMoi,

                MoTa =
                    $"Tạo yêu cầu hủy hợp đồng #{cancellation.IdHopDong}",

                IpAddress =
                    null
            });

        return MapToResponse(cancellation);
    }

    // =========================================================
    // LẤY THEO ID
    // =========================================================
    public async Task<CancellationResponse?> GetByIdAsync(int id)
    {
        if (id <= 0)
            return null;

        var cancellation =
            await _context.YeuCauHuyHopDongs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == id);

        if (cancellation == null)
            return null;

        return MapToResponse(cancellation);
    }

    // =========================================================
    // LẤY THEO HỢP ĐỒNG
    // =========================================================
    public async Task<List<CancellationResponse>>
        GetByContractIdAsync(int idHopDong)
    {
        if (idHopDong <= 0)
        {
            return new List<CancellationResponse>();
        }

        var cancellations =
            await _context.YeuCauHuyHopDongs
                .AsNoTracking()
                .Where(x =>
                    x.IdHopDong == idHopDong)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

        return cancellations
            .Select(MapToResponse)
            .ToList();
    }

    // =========================================================
    // DUYỆT / TỪ CHỐI YÊU CẦU HỦY
    // =========================================================
    public async Task<CancellationResponse> ProcessAsync(
        int id,
        ProcessCancellationRequest request)
    {
        if (id <= 0)
        {
            throw new ArgumentException(
                "ID yêu cầu hủy không hợp lệ.");
        }

        if (request.IdNguoiXuLy <= 0)
        {
            throw new ArgumentException(
                "ID người xử lý không hợp lệ.");
        }

        string status =
            request.TrangThai
                .Trim()
                .ToUpperInvariant();

        if (status != "APPROVED" &&
            status != "REJECTED")
        {
            throw new ArgumentException(
                "Trạng thái chỉ được là APPROVED hoặc REJECTED.");
        }

        var cancellation =
            await _context.YeuCauHuyHopDongs
                .FirstOrDefaultAsync(
                    x => x.Id == id);

        if (cancellation == null)
        {
            throw new ArgumentException(
                "Không tìm thấy yêu cầu hủy hợp đồng.");
        }

        if (cancellation.TrangThai != "PENDING")
        {
            throw new ArgumentException(
                "Yêu cầu hủy này đã được xử lý.");
        }

        // Lưu trạng thái cũ để ghi Audit
        string oldStatus =
            cancellation.TrangThai;

        cancellation.TrangThai =
            status;

        cancellation.IdNguoiXuLy =
            request.IdNguoiXuLy;

        cancellation.ThoiGianXuLy =
            DateTime.UtcNow;

        // =====================================================
        // LƯU KẾT QUẢ XỬ LÝ
        // =====================================================
        await _context.SaveChangesAsync();

        // =====================================================
        // AUDIT: APPROVED / REJECTED
        // =====================================================
        var duLieuCu = JsonSerializer.Serialize(new
        {
            trangThai =
                oldStatus
        });

        var duLieuMoiXuLy = JsonSerializer.Serialize(new
        {
            trangThai =
                cancellation.TrangThai,

            idNguoiXuLy =
                cancellation.IdNguoiXuLy,

            thoiGianXuLy =
                cancellation.ThoiGianXuLy,

            soTienHoanDuKien =
                cancellation.SoTienHoanDuKien,

            soTienPhat =
                cancellation.SoTienPhat
        });

        string auditAction =
            status == "APPROVED"
                ? "CANCELLATION_APPROVED"
                : "CANCELLATION_REJECTED";

        string moTa =
            status == "APPROVED"
                ? $"Duyệt yêu cầu hủy #{cancellation.Id} cho hợp đồng #{cancellation.IdHopDong}"
                : $"Từ chối yêu cầu hủy #{cancellation.Id} cho hợp đồng #{cancellation.IdHopDong}";

        await _auditLogService.CreateAsync(
            new CreateAuditLogRequest
            {
                IdNguoiDung =
                    request.IdNguoiXuLy,

                HanhDong =
                    auditAction,

                LoaiDoiTuong =
                    "CANCELLATION",

                IdDoiTuong =
                    cancellation.Id,

                DuLieuCu =
                    duLieuCu,

                DuLieuMoi =
                    duLieuMoiXuLy,

                MoTa =
                    moTa,

                IpAddress =
                    null
            });

        return MapToResponse(cancellation);
    }

    // =========================================================
    // ENTITY -> RESPONSE
    // =========================================================
    private static CancellationResponse MapToResponse(
        YeuCauHuyHopDong cancellation)
    {
        return new CancellationResponse
        {
            Id =
                cancellation.Id,

            IdHopDong =
                cancellation.IdHopDong,

            IdNguoiYeuCau =
                cancellation.IdNguoiYeuCau,

            LyDo =
                cancellation.LyDo,

            TrangThai =
                cancellation.TrangThai,

            SoTienHoanDuKien =
                cancellation.SoTienHoanDuKien,

            SoTienPhat =
                cancellation.SoTienPhat,

            IdNguoiXuLy =
                cancellation.IdNguoiXuLy,

            ThoiGianTao =
                cancellation.ThoiGianTao,

            ThoiGianXuLy =
                cancellation.ThoiGianXuLy
        };
    }
}