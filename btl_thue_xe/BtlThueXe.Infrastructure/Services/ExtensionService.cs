using BtlThueXe.Core.DTOs.Extensions;
using BtlThueXe.Core.DTOs.AuditLogs;
using BtlThueXe.Core.Entities;
using BtlThueXe.Core.Interfaces;
using BtlThueXe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BtlThueXe.Infrastructure.Services;

public class ExtensionService : IExtensionService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public ExtensionService(
        ApplicationDbContext context,
        IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    // =========================================================
    // TẠO YÊU CẦU GIA HẠN
    // =========================================================
    public async Task<ExtensionResponse> CreateAsync(
        CreateExtensionRequest request)
    {
        if (request.IdHopDong <= 0)
            throw new ArgumentException(
                "ID hợp đồng không hợp lệ.");

        if (request.IdNguoiYeuCau <= 0)
            throw new ArgumentException(
                "ID người yêu cầu không hợp lệ.");

        if (request.SoNgayGiaHan <= 0)
            throw new ArgumentException(
                "Số ngày gia hạn phải lớn hơn 0.");

        if (request.TienPhatSinh < 0)
            throw new ArgumentException(
                "Tiền phát sinh không được âm.");

        DateTime thoiGianTraCu =
            ToUtc(request.ThoiGianTraCu);

        DateTime thoiGianTraMoi =
            ToUtc(request.ThoiGianTraMoi);

        if (thoiGianTraMoi <= thoiGianTraCu)
        {
            throw new ArgumentException(
                "Thời gian trả mới phải sau thời gian trả cũ.");
        }

        var extension = new YeuCauGiaHan
        {
            IdHopDong =
                request.IdHopDong,

            IdNguoiYeuCau =
                request.IdNguoiYeuCau,

            ThoiGianTraCu =
                thoiGianTraCu,

            ThoiGianTraMoi =
                thoiGianTraMoi,

            SoNgayGiaHan =
                request.SoNgayGiaHan,

            TienPhatSinh =
                request.TienPhatSinh,

            LyDo =
                string.IsNullOrWhiteSpace(request.LyDo)
                    ? null
                    : request.LyDo.Trim(),

            TrangThai =
                "PENDING",

            IdNguoiXuLy =
                null,

            ThoiGianTao =
                DateTime.UtcNow,

            ThoiGianXuLy =
                null
        };

        // =====================================================
        // LƯU YÊU CẦU GIA HẠN
        // =====================================================
        _context.YeuCauGiaHans.Add(extension);

        await _context.SaveChangesAsync();

        // =====================================================
        // AUDIT: TẠO YÊU CẦU GIA HẠN
        // =====================================================
        var duLieuMoi = JsonSerializer.Serialize(new
        {
            idHopDong =
                extension.IdHopDong,

            thoiGianTraCu =
                extension.ThoiGianTraCu,

            thoiGianTraMoi =
                extension.ThoiGianTraMoi,

            soNgayGiaHan =
                extension.SoNgayGiaHan,

            tienPhatSinh =
                extension.TienPhatSinh,

            trangThai =
                extension.TrangThai
        });

        await _auditLogService.CreateAsync(
            new CreateAuditLogRequest
            {
                IdNguoiDung =
                    extension.IdNguoiYeuCau,

                HanhDong =
                    "EXTENSION_CREATED",

                LoaiDoiTuong =
                    "EXTENSION",

                IdDoiTuong =
                    extension.Id,

                DuLieuCu =
                    null,

                DuLieuMoi =
                    duLieuMoi,

                MoTa =
                    $"Tạo yêu cầu gia hạn cho hợp đồng #{extension.IdHopDong}",

                IpAddress =
                    null
            });

        return MapToResponse(extension);
    }

    // =========================================================
    // LẤY YÊU CẦU THEO ID
    // =========================================================
    public async Task<ExtensionResponse?> GetByIdAsync(int id)
    {
        if (id <= 0)
            return null;

        var extension =
            await _context.YeuCauGiaHans
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == id);

        if (extension == null)
            return null;

        return MapToResponse(extension);
    }

    // =========================================================
    // LẤY DANH SÁCH GIA HẠN THEO HỢP ĐỒNG
    // =========================================================
    public async Task<List<ExtensionResponse>>
        GetByContractIdAsync(int idHopDong)
    {
        if (idHopDong <= 0)
            return new List<ExtensionResponse>();

        var extensions =
            await _context.YeuCauGiaHans
                .AsNoTracking()
                .Where(x =>
                    x.IdHopDong == idHopDong)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

        return extensions
            .Select(MapToResponse)
            .ToList();
    }

    // =========================================================
    // DUYỆT / TỪ CHỐI YÊU CẦU GIA HẠN
    // =========================================================
    public async Task<ExtensionResponse> ProcessAsync(
        int id,
        ProcessExtensionRequest request)
    {
        if (id <= 0)
        {
            throw new ArgumentException(
                "ID yêu cầu gia hạn không hợp lệ.");
        }

        if (request.IdNguoiXuLy <= 0)
        {
            throw new ArgumentException(
                "ID người xử lý không hợp lệ.");
        }

        string status = request.TrangThai
            .Trim()
            .ToUpperInvariant();

        if (status != "APPROVED" &&
            status != "REJECTED")
        {
            throw new ArgumentException(
                "Trạng thái chỉ được là APPROVED hoặc REJECTED.");
        }

        var extension =
            await _context.YeuCauGiaHans
                .FirstOrDefaultAsync(
                    x => x.Id == id);

        if (extension == null)
        {
            throw new ArgumentException(
                "Không tìm thấy yêu cầu gia hạn.");
        }

        if (extension.TrangThai != "PENDING")
        {
            throw new ArgumentException(
                "Yêu cầu gia hạn này đã được xử lý.");
        }

        // Lưu trạng thái cũ để Audit
        string oldStatus =
            extension.TrangThai;

        extension.TrangThai =
            status;

        extension.IdNguoiXuLy =
            request.IdNguoiXuLy;

        extension.ThoiGianXuLy =
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
                extension.TrangThai,

            idNguoiXuLy =
                extension.IdNguoiXuLy,

            thoiGianXuLy =
                extension.ThoiGianXuLy
        });

        string auditAction =
            status == "APPROVED"
                ? "EXTENSION_APPROVED"
                : "EXTENSION_REJECTED";

        string moTa =
            status == "APPROVED"
                ? $"Duyệt yêu cầu gia hạn #{extension.Id} cho hợp đồng #{extension.IdHopDong}"
                : $"Từ chối yêu cầu gia hạn #{extension.Id} cho hợp đồng #{extension.IdHopDong}";

        await _auditLogService.CreateAsync(
            new CreateAuditLogRequest
            {
                IdNguoiDung =
                    request.IdNguoiXuLy,

                HanhDong =
                    auditAction,

                LoaiDoiTuong =
                    "EXTENSION",

                IdDoiTuong =
                    extension.Id,

                DuLieuCu =
                    duLieuCu,

                DuLieuMoi =
                    duLieuMoiXuLy,

                MoTa =
                    moTa,

                IpAddress =
                    null
            });

        return MapToResponse(extension);
    }

    // =========================================================
    // CHUYỂN DATETIME SANG UTC
    // =========================================================
    private static DateTime ToUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
            return value;

        if (value.Kind == DateTimeKind.Local)
            return value.ToUniversalTime();

        return DateTime.SpecifyKind(
            value,
            DateTimeKind.Utc);
    }

    // =========================================================
    // ENTITY -> RESPONSE DTO
    // =========================================================
    private static ExtensionResponse MapToResponse(
        YeuCauGiaHan extension)
    {
        return new ExtensionResponse
        {
            Id =
                extension.Id,

            IdHopDong =
                extension.IdHopDong,

            IdNguoiYeuCau =
                extension.IdNguoiYeuCau,

            ThoiGianTraCu =
                extension.ThoiGianTraCu,

            ThoiGianTraMoi =
                extension.ThoiGianTraMoi,

            SoNgayGiaHan =
                extension.SoNgayGiaHan,

            TienPhatSinh =
                extension.TienPhatSinh,

            LyDo =
                extension.LyDo,

            TrangThai =
                extension.TrangThai,

            IdNguoiXuLy =
                extension.IdNguoiXuLy,

            ThoiGianTao =
                extension.ThoiGianTao,

            ThoiGianXuLy =
                extension.ThoiGianXuLy
        };
    }
}