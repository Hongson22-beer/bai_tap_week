using BtlThueXe.Core.DTOs.Extensions;
using BtlThueXe.Core.Interfaces;
using BtlThueXe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BtlThueXe.Infrastructure.Services;

public class ExtensionService : IExtensionService
{
    private readonly ApplicationDbContext _context;

    public ExtensionService(ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // KHÁCH HÀNG TẠO YÊU CẦU GIA HẠN
    // =========================================================
    public async Task<ExtensionResponse> CreateAsync(
        CreateExtensionRequest request,
        int currentUserId)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (currentUserId <= 0)
            throw new UnauthorizedAccessException(
                "Không xác định được người dùng.");

        if (request.IdHopDong <= 0)
            throw new ArgumentException(
                "ID hợp đồng không hợp lệ.");


        // =====================================================
        // LẤY HỢP ĐỒNG + KHÁCH HÀNG + XE
        // =====================================================
        var hopDong = await _context.HopDongs
            .Include(x => x.IdYeuCauThueNavigation)
                .ThenInclude(x => x.IdKhachHangNavigation)
            .Include(x => x.IdYeuCauThueNavigation)
                .ThenInclude(x => x.IdXeNavigation)
            .FirstOrDefaultAsync(x =>
                x.Id == request.IdHopDong);

        if (hopDong == null)
            throw new KeyNotFoundException(
                "Không tìm thấy hợp đồng.");

        // =====================================================
        // CHỈ GIA HẠN KHI HỢP ĐỒNG ĐANG THUÊ
        // =====================================================
        if (!string.Equals(
                hopDong.TrangThai,
                "IN_PROGRESS",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Không thể gia hạn khi hợp đồng đang ở trạng thái " +
                $"{hopDong.TrangThai}. Hợp đồng phải ở IN_PROGRESS.");
        }

        var yeuCauThue =
            hopDong.IdYeuCauThueNavigation;

        if (yeuCauThue == null)
            throw new InvalidOperationException(
                "Không tìm thấy yêu cầu thuê của hợp đồng.");

        var khachHang =
            yeuCauThue.IdKhachHangNavigation;

        if (khachHang == null)
            throw new InvalidOperationException(
                "Không tìm thấy khách hàng của hợp đồng.");

        // =====================================================
        // JWT chứa ID NguoiDung.
        // IdNguoiYeuCau trong bảng gia hạn là ID NguoiDung.
        //
        // Kiểm tra hợp đồng có thuộc tài khoản hiện tại hay không.
        // =====================================================
        if (khachHang.IdNguoiDung != currentUserId)
        {
            throw new UnauthorizedAccessException(
                "Bạn không có quyền yêu cầu gia hạn hợp đồng này.");
        }

        // Không tin IdNguoiYeuCau do client gửi lên.
        // currentUserId lấy trực tiếp từ JWT.

        // Backend là nguồn sự thật cho thời gian cũ, số ngày và tiền gia hạn.
        var thoiGianTraCu = hopDong.ThoiGianTraDuKien;
        if (request.ThoiGianTraMoi <= thoiGianTraCu)
            throw new ArgumentException("Thời gian trả mới phải lớn hơn thời gian trả hiện tại.");

        var extensionHours = (request.ThoiGianTraMoi - thoiGianTraCu).TotalHours;
        var soNgayGiaHan = Math.Max(1, (int)Math.Ceiling(extensionHours / 24d));
        var tienGiaHan = soNgayGiaHan * hopDong.DonGiaNgay;

        // =====================================================
        // KHÔNG CHO TẠO NHIỀU YÊU CẦU PENDING
        // =====================================================
        bool pendingExists =
            await _context.YeuCauGiaHans.AnyAsync(x =>
                x.IdHopDong == hopDong.Id &&
                x.TrangThai == "PENDING");

        if (pendingExists)
        {
            throw new InvalidOperationException(
                "Hợp đồng đang có một yêu cầu gia hạn chờ xử lý.");
        }

        // =====================================================
        // KIỂM TRA TRÙNG LỊCH XE
        //
        // Khoảng thời gian gia hạn:
        // thời gian trả hiện tại -> thời gian trả mới
        //
        // Nếu xe đã được đặt cho yêu cầu thuê khác trong khoảng
        // này thì không cho gia hạn.
        // =====================================================
        int idXe = yeuCauThue.IdXe;

        DateTime extensionStart =
            hopDong.ThoiGianTraDuKien;

        DateTime extensionEnd =
            request.ThoiGianTraMoi;

        bool vehicleConflict =
            await _context.YeuCauThues.AnyAsync(x =>
                x.Id != yeuCauThue.Id &&
                x.IdXe == idXe &&

                (
                    x.TrangThai == "APPROVED" ||
                    x.TrangThai == "CONFIRMED"
                ) &&

                x.ThoiGianNhan < extensionEnd &&
                x.ThoiGianTraDuKien > extensionStart);

        if (vehicleConflict)
        {
            throw new InvalidOperationException(
                "Không thể gia hạn vì xe đã có lịch thuê khác trong khoảng thời gian yêu cầu.");
        }

        // =====================================================
        // TẠO YÊU CẦU GIA HẠN PENDING
        // =====================================================
        var entity = new YeuCauGiaHan
        {
            IdHopDong =
                hopDong.Id,

            IdNguoiYeuCau =
                currentUserId,

            ThoiGianTraCu =
                thoiGianTraCu,

            ThoiGianTraMoi =
                request.ThoiGianTraMoi,

            SoNgayGiaHan =
                soNgayGiaHan,

            TienPhatSinh =
                tienGiaHan,

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

        _context.YeuCauGiaHans.Add(entity);

        _context.AuditLogs.Add(new AuditLog
        {
            IdNguoiDung = currentUserId,
            HanhDong = "CREATE_EXTENSION_REQUEST",
            LoaiDoiTuong = "HOP_DONG",
            IdDoiTuong = hopDong.Id,
            DuLieuCu = $"ThoiGianTra={hopDong.ThoiGianTraDuKien:O}",
            DuLieuMoi = $"YeuCauTraMoi={request.ThoiGianTraMoi:O}; TrangThai=PENDING",
            MoTa = $"Khách hàng tạo yêu cầu gia hạn hợp đồng #{hopDong.Id}.",
            ThoiGian = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return MapToResponse(entity);
    }

    // =========================================================
    // NHÂN VIÊN DUYỆT / TỪ CHỐI GIA HẠN
    // =========================================================
    public async Task<ExtensionResponse> ProcessAsync(
        int id,
        ProcessExtensionRequest request,
        int currentUserId)
    {
        if (id <= 0)
            throw new ArgumentException(
                "ID yêu cầu gia hạn không hợp lệ.");

        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (currentUserId <= 0)
            throw new UnauthorizedAccessException(
                "Không xác định được nhân viên.");

        string newStatus =
            request.TrangThai?
                .Trim()
                .ToUpperInvariant()
            ?? string.Empty;

        if (newStatus != "APPROVED" &&
            newStatus != "REJECTED")
        {
            throw new ArgumentException(
                "Trạng thái xử lý chỉ được là APPROVED hoặc REJECTED.");
        }

        var extension =
            await _context.YeuCauGiaHans
                .FirstOrDefaultAsync(x =>
                    x.Id == id);

        if (extension == null)
            throw new KeyNotFoundException(
                "Không tìm thấy yêu cầu gia hạn.");

        if (!string.Equals(
                extension.TrangThai,
                "PENDING",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Yêu cầu gia hạn này đã được xử lý.");
        }

        var hopDong =
            await _context.HopDongs
                .Include(x => x.IdYeuCauThueNavigation)
                .FirstOrDefaultAsync(x =>
                    x.Id == extension.IdHopDong);

        if (hopDong == null)
            throw new KeyNotFoundException(
                "Không tìm thấy hợp đồng.");

        if (!string.Equals(
                hopDong.TrangThai,
                "IN_PROGRESS",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Không thể xử lý gia hạn vì hợp đồng đang ở trạng thái {hopDong.TrangThai}.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // Không tin IdNguoiXuLy từ client.
            extension.IdNguoiXuLy =
                currentUserId;

            extension.TrangThai =
                newStatus;

            extension.ThoiGianXuLy =
                DateTime.UtcNow;

            // =================================================
            // NẾU APPROVED
            // =================================================
            if (newStatus == "APPROVED")
            {
                var yeuCauThue =
                    hopDong.IdYeuCauThueNavigation;

                if (yeuCauThue == null)
                {
                    throw new InvalidOperationException(
                        "Không tìm thấy yêu cầu thuê của hợp đồng.");
                }

                // Kiểm tra lại trùng lịch ngay lúc duyệt.
                // Tránh trường hợp từ lúc khách gửi request đến lúc
                // nhân viên duyệt đã xuất hiện booking mới.
                bool vehicleConflict =
                    await _context.YeuCauThues.AnyAsync(x =>
                        x.Id != yeuCauThue.Id &&
                        x.IdXe == yeuCauThue.IdXe &&

                        (
                            x.TrangThai == "APPROVED" ||
                            x.TrangThai == "CONFIRMED"
                        ) &&

                        x.ThoiGianNhan <
                            extension.ThoiGianTraMoi &&

                        x.ThoiGianTraDuKien >
                            hopDong.ThoiGianTraDuKien);

                if (vehicleConflict)
                {
                    throw new InvalidOperationException(
                        "Không thể duyệt gia hạn vì xe đã có lịch thuê khác.");
                }

                // Cập nhật thời gian trả của hợp đồng
                hopDong.ThoiGianTraDuKien =
                    extension.ThoiGianTraMoi;

                hopDong.ThoiGianCapNhat =
                    DateTime.UtcNow;

                // Đồng bộ thời gian của yêu cầu thuê
                yeuCauThue.ThoiGianTraDuKien =
                    extension.ThoiGianTraMoi;

                yeuCauThue.ThoiGianCapNhat =
                    DateTime.UtcNow;
            }

            _context.AuditLogs.Add(new AuditLog
            {
                IdNguoiDung = currentUserId,
                HanhDong = newStatus == "APPROVED"
                    ? "APPROVE_EXTENSION_REQUEST"
                    : "REJECT_EXTENSION_REQUEST",
                LoaiDoiTuong = "YEU_CAU_GIA_HAN",
                IdDoiTuong = extension.Id,
                DuLieuCu = "TrangThai=PENDING",
                DuLieuMoi = $"TrangThai={newStatus}; ThoiGianTraMoi={extension.ThoiGianTraMoi:O}",
                MoTa = $"Nhân viên xử lý yêu cầu gia hạn #{extension.Id} của hợp đồng #{hopDong.Id}.",
                ThoiGian = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return MapToResponse(extension);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // =========================================================
    // GET BY ID
    // =========================================================
    public async Task<ExtensionResponse?> GetByIdAsync(
        int id)
    {
        if (id <= 0)
            return null;

        var entity =
            await _context.YeuCauGiaHans
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == id);

        return entity == null
            ? null
            : MapToResponse(entity);
    }

    // =========================================================
    // GET BY CONTRACT
    // =========================================================
    public async Task<List<ExtensionResponse>>
        GetByContractIdAsync(int idHopDong)
    {
        if (idHopDong <= 0)
            return new List<ExtensionResponse>();

        var entities =
            await _context.YeuCauGiaHans
                .AsNoTracking()
                .Where(x =>
                    x.IdHopDong == idHopDong)
                .OrderByDescending(x =>
                    x.ThoiGianTao)
                .ToListAsync();

        return entities
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<List<ExtensionResponse>> GetMyByContractIdAsync(
        int idHopDong, int currentUserId)
    {
        if (idHopDong <= 0) return new List<ExtensionResponse>();

        bool owned = await _context.HopDongs
            .AsNoTracking()
            .Include(x => x.IdYeuCauThueNavigation)
                .ThenInclude(x => x.IdKhachHangNavigation)
            .AnyAsync(x => x.Id == idHopDong &&
                x.IdYeuCauThueNavigation.IdKhachHangNavigation.IdNguoiDung == currentUserId);

        if (!owned)
            throw new UnauthorizedAccessException("Bạn không có quyền xem gia hạn của hợp đồng này.");

        return await GetByContractIdAsync(idHopDong);
    }

    // =========================================================
    // ENTITY -> DTO
    // =========================================================
    private static ExtensionResponse MapToResponse(
        YeuCauGiaHan entity)
    {
        return new ExtensionResponse
        {
            Id =
                entity.Id,

            IdHopDong =
                entity.IdHopDong,

            IdNguoiYeuCau =
                entity.IdNguoiYeuCau,

            ThoiGianTraCu =
                entity.ThoiGianTraCu,

            ThoiGianTraMoi =
                entity.ThoiGianTraMoi,

            SoNgayGiaHan =
                entity.SoNgayGiaHan,

            TienPhatSinh =
                entity.TienPhatSinh,

            LyDo =
                entity.LyDo,

            TrangThai =
                entity.TrangThai,

            IdNguoiXuLy =
                entity.IdNguoiXuLy,

            ThoiGianTao =
                entity.ThoiGianTao,

            ThoiGianXuLy =
                entity.ThoiGianXuLy
        };
    }
}