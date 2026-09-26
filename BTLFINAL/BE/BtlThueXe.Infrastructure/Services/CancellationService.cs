using BtlThueXe.Core.DTOs.Cancellations;
using BtlThueXe.Core.Interfaces;
using BtlThueXe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BtlThueXe.Infrastructure.Services;

public class CancellationService : ICancellationService
{
    private readonly ApplicationDbContext _context;

    public CancellationService(ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // KHÁCH HÀNG TẠO YÊU CẦU HỦY
    // =========================================================
    public async Task<CancellationResponse> CreateAsync(
        CreateCancellationRequest request,
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
        // KIỂM TRA QUYỀN SỞ HỮU
        // JWT = NguoiDung.Id
        // =====================================================
        if (khachHang.IdNguoiDung != currentUserId)
        {
            throw new UnauthorizedAccessException(
                "Bạn không có quyền yêu cầu hủy hợp đồng này.");
        }

        // =====================================================
        // KHÔNG CHO HỦY HỢP ĐỒNG ĐÃ KẾT THÚC
        // =====================================================
        string contractStatus =
            hopDong.TrangThai?.ToUpperInvariant()
            ?? string.Empty;

        if (contractStatus == "CANCELLED")
        {
            throw new InvalidOperationException(
                "Hợp đồng đã bị hủy.");
        }

        if (contractStatus == "COMPLETED" ||
            contractStatus == "RETURNED")
        {
            throw new InvalidOperationException(
                "Không thể hủy hợp đồng đã hoàn tất hoặc đã trả xe.");
        }

        if (contractStatus == "IN_PROGRESS")
        {
            throw new InvalidOperationException(
                "Xe đã được bàn giao. Không thể sử dụng chức năng hủy hợp đồng.");
        }

        // =====================================================
        // KHÔNG CHO TẠO NHIỀU REQUEST PENDING
        // =====================================================
        bool pendingExists =
            await _context.YeuCauHuyHopDongs.AnyAsync(x =>
                x.IdHopDong == hopDong.Id &&
                x.TrangThai == "PENDING");

        if (pendingExists)
        {
            throw new InvalidOperationException(
                "Hợp đồng đang có một yêu cầu hủy chờ xử lý.");
        }

        // =====================================================
        // TẠO REQUEST
        // IdNguoiYeuCau lấy từ JWT
        // =====================================================
        var entity = new YeuCauHuyHopDong
        {
            IdHopDong =
                hopDong.Id,

            IdNguoiYeuCau =
                currentUserId,

            LyDo =
                string.IsNullOrWhiteSpace(request.LyDo)
                    ? null
                    : request.LyDo.Trim(),

            TrangThai =
                "PENDING",

            // Tiền hoàn do backend tính, không tin số tiền từ client.
            SoTienHoanDuKien = 0m,
            SoTienPhat = 0m,

            IdNguoiXuLy =
                null,

            ThoiGianTao =
                DateTime.UtcNow,

            ThoiGianXuLy =
                null
        };

        // =====================================================
        // CHÍNH SÁCH HỦY / HOÀN CỌC
        //
        // Khóa kết quả NGAY TẠI THỜI ĐIỂM khách gửi yêu cầu hủy.
        // Nhân viên duyệt sau không tính lại, tránh khách bị thiệt do xử lý chậm.
        //
        // - Trước giờ nhận xe > 48 giờ:
        //      phí hủy = 0 -> hoàn 100% tiền cọc đã PAID.
        // - Còn trên 0 đến <= 48 giờ:
        //      phí hủy = min(tiền cọc đã PAID, 3 * đơn giá/ngày).
        //      tiền hoàn = tiền cọc đã PAID - phí hủy.
        // - Đã đến/quá giờ nhận xe (no-show):
        //      phí hủy = toàn bộ tiền cọc -> hoàn 0.
        //
        // Trường hợp lỗi cửa hàng/xe được xử lý theo nghiệp vụ riêng:
        // hoàn 100% cọc, không áp dụng phí hủy khách hàng.
        // =====================================================
        var paidDeposit = await _context.ThanhToans
            .Where(x =>
                x.IdHopDong == hopDong.Id &&
                x.LoaiThanhToan == "TIEN_COC" &&
                x.TrangThai == "PAID")
            .SumAsync(x => (decimal?)x.SoTien) ?? 0m;

        var cancellationRequestedAt = entity.ThoiGianTao;
        var hoursBeforePickup =
            (yeuCauThue.ThoiGianNhan - cancellationRequestedAt).TotalHours;

        decimal cancellationFee;
        decimal refundAmount;

        if (paidDeposit <= 0m)
        {
            cancellationFee = 0m;
            refundAmount = 0m;
        }
        else if (hoursBeforePickup > 48d)
        {
            // Hủy sớm: hoàn toàn bộ cọc.
            cancellationFee = 0m;
            refundAmount = paidDeposit;
        }
        else if (hoursBeforePickup > 0d)
        {
            // Hủy sát ngày nhận: khấu trừ tối đa tương đương 3 ngày thuê,
            // nhưng không bao giờ vượt quá số tiền cọc khách đã thanh toán.
            decimal threeDayRentalFee =
                Math.Max(0m, hopDong.DonGiaNgay * 3m);

            cancellationFee =
                Math.Min(paidDeposit, threeDayRentalFee);

            refundAmount =
                Math.Max(0m, paidDeposit - cancellationFee);
        }
        else
        {
            // Đến/quá giờ nhận xe: xem là no-show, không hoàn cọc.
            cancellationFee = paidDeposit;
            refundAmount = 0m;
        }

        entity.SoTienHoanDuKien =
            Math.Round(refundAmount, 0, MidpointRounding.AwayFromZero);

        entity.SoTienPhat =
            Math.Round(cancellationFee, 0, MidpointRounding.AwayFromZero);

        _context.YeuCauHuyHopDongs.Add(entity);

        _context.AuditLogs.Add(new AuditLog
        {
            IdNguoiDung = currentUserId,
            HanhDong = "CREATE_CANCELLATION_REQUEST",
            LoaiDoiTuong = "HOP_DONG",
            IdDoiTuong = hopDong.Id,
            DuLieuCu = $"Contract={hopDong.TrangThai}",
            DuLieuMoi = "CancellationRequest=PENDING",
            MoTa = $"Khách hàng tạo yêu cầu hủy hợp đồng #{hopDong.Id}.",
            ThoiGian = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return MapToResponse(entity);
    }

    // =========================================================
    // NHÂN VIÊN DUYỆT / TỪ CHỐI
    // =========================================================
    public async Task<CancellationResponse> ProcessAsync(
        int id,
        ProcessCancellationRequest request,
        int currentUserId)
    {
        if (id <= 0)
            throw new ArgumentException(
                "ID yêu cầu hủy không hợp lệ.");

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
                "Trạng thái chỉ được là APPROVED hoặc REJECTED.");
        }

        var cancellation =
            await _context.YeuCauHuyHopDongs
                .FirstOrDefaultAsync(x =>
                    x.Id == id);

        if (cancellation == null)
            throw new KeyNotFoundException(
                "Không tìm thấy yêu cầu hủy.");

        if (!string.Equals(
                cancellation.TrangThai,
                "PENDING",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Yêu cầu hủy này đã được xử lý.");
        }

        var hopDong = await _context.HopDongs
            .Include(x => x.IdYeuCauThueNavigation)
                .ThenInclude(x => x.IdXeNavigation)
            .FirstOrDefaultAsync(x =>
                x.Id == cancellation.IdHopDong);

        if (hopDong == null)
            throw new KeyNotFoundException(
                "Không tìm thấy hợp đồng.");

        if (string.Equals(
                hopDong.TrangThai,
                "COMPLETED",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                hopDong.TrangThai,
                "RETURNED",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                hopDong.TrangThai,
                "CANCELLED",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                hopDong.TrangThai,
                "IN_PROGRESS",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Không thể xử lý hủy khi hợp đồng đang ở trạng thái {hopDong.TrangThai}.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            cancellation.TrangThai =
                newStatus;

            // Không lấy IdNguoiXuLy từ body
            cancellation.IdNguoiXuLy =
                currentUserId;

            cancellation.ThoiGianXuLy =
                DateTime.UtcNow;

            // =================================================
            // REJECTED
            // Chỉ cập nhật request, không thay đổi hợp đồng
            // =================================================
            if (newStatus == "REJECTED")
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    IdNguoiDung = currentUserId,
                    HanhDong = "REJECT_CANCELLATION_REQUEST",
                    LoaiDoiTuong = "YEU_CAU_HUY_HOP_DONG",
                    IdDoiTuong = cancellation.Id,
                    DuLieuCu = "TrangThai=PENDING",
                    DuLieuMoi = "TrangThai=REJECTED",
                    MoTa = $"Nhân viên từ chối yêu cầu hủy #{cancellation.Id} của hợp đồng #{hopDong.Id}.",
                    ThoiGian = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return MapToResponse(cancellation);
            }

            // =================================================
            // APPROVED
            // =================================================
            var yeuCauThue =
                hopDong.IdYeuCauThueNavigation;

            if (yeuCauThue == null)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy yêu cầu thuê của hợp đồng.");
            }

            var xe =
                yeuCauThue.IdXeNavigation;

            // =================================================
            // CONTRACT -> CANCELLED
            // =================================================
            string? oldContractStatus =
                hopDong.TrangThai;

            hopDong.TrangThai =
                "CANCELLED";

            hopDong.ThoiGianCapNhat =
                DateTime.UtcNow;

            _context.LichSuTrangThaiHopDongs.Add(
                new LichSuTrangThaiHopDong
                {
                    IdHopDong =
                        hopDong.Id,

                    TrangThaiCu =
                        oldContractStatus,

                    TrangThaiMoi =
                        "CANCELLED",

                    IdNguoiThayDoi =
                        currentUserId,

                    LyDo =
                        "Nhân viên phê duyệt yêu cầu hủy hợp đồng.",

                    ThoiGianThayDoi =
                        DateTime.UtcNow
                });

            // =================================================
            // RENTAL REQUEST -> CANCELLED
            // =================================================
            yeuCauThue.TrangThai =
                "CANCELLED";

            yeuCauThue.LyDoHuy =
                cancellation.LyDo;

            yeuCauThue.ThoiGianCapNhat =
                DateTime.UtcNow;

            // =================================================
            // GIẢI PHÓNG XE
            //
            // Chỉ đổi RESERVED -> AVAILABLE.
            // Không tự ý đổi RENTING/MAINTENANCE...
            // =================================================
            if (xe != null &&
                string.Equals(
                    xe.TrangThai,
                    "RESERVED",
                    StringComparison.OrdinalIgnoreCase))
            {
                string oldVehicleStatus =
                    xe.TrangThai;

                xe.TrangThai =
                    "AVAILABLE";

                _context.LichSuTrangThaiXes.Add(
                    new LichSuTrangThaiXe
                    {
                        IdXe =
                            xe.Id,

                        TrangThaiCu =
                            oldVehicleStatus,

                        TrangThaiMoi =
                            "AVAILABLE",

                        IdNguoiThayDoi =
                            currentUserId,

                        LyDo =
                            $"Hủy hợp đồng #{hopDong.Id}.",

                        ThoiGianThayDoi =
                            DateTime.UtcNow
                    });
            }

            // =================================================
            // HOÀN TIỀN
            // Giữ nguyên TIEN_COC=PAID để bảo toàn lịch sử.
            // Tạo một giao dịch HOAN_TIEN riêng ở REFUND_PENDING.
            // =================================================
            if (cancellation.SoTienHoanDuKien > 0)
            {
                bool refundExists = await _context.ThanhToans.AnyAsync(x =>
                    x.IdHopDong == hopDong.Id && x.LoaiThanhToan == "HOAN_TIEN" &&
                    (x.TrangThai == "REFUND_PENDING" || x.TrangThai == "REFUNDED"));
                if (!refundExists)
                {
                    _context.ThanhToans.Add(new ThanhToan
                    {
                        IdHopDong = hopDong.Id,
                        LoaiThanhToan = "HOAN_TIEN",
                        SoTien = cancellation.SoTienHoanDuKien,
                        PhuongThuc = "CHUYEN_KHOAN",
                        MaGiaoDich = $"RF-CANCEL-{cancellation.Id}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                        TrangThai = "REFUND_PENDING",
                        GhiChu = "Hoàn tiền do hủy hợp đồng. Thông tin nhận tiền lấy từ hồ sơ khách hàng.",
                        ThoiGianThanhToan = null
                    });
                }
            }

            _context.AuditLogs.Add(new AuditLog
            {
                IdNguoiDung = currentUserId,
                HanhDong = "APPROVE_CANCELLATION_REQUEST",
                LoaiDoiTuong = "YEU_CAU_HUY_HOP_DONG",
                IdDoiTuong = cancellation.Id,
                DuLieuCu = $"TrangThai=PENDING; Contract={oldContractStatus}",
                DuLieuMoi = "TrangThai=APPROVED; Contract=CANCELLED",
                MoTa = $"Nhân viên phê duyệt yêu cầu hủy #{cancellation.Id} của hợp đồng #{hopDong.Id}.",
                ThoiGian = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return MapToResponse(cancellation);
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
    public async Task<CancellationResponse?> GetByIdAsync(
        int id)
    {
        if (id <= 0)
            return null;

        var entity =
            await _context.YeuCauHuyHopDongs
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
    public async Task<List<CancellationResponse>>
        GetByContractIdAsync(int idHopDong)
    {
        if (idHopDong <= 0)
            return new List<CancellationResponse>();

        var entities =
            await _context.YeuCauHuyHopDongs
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

    // =========================================================
    // GET MY BY CONTRACT - kiểm tra quyền sở hữu theo JWT user id
    // =========================================================
    public async Task<List<CancellationResponse>> GetMyByContractIdAsync(
        int idHopDong, int currentUserId)
    {
        if (idHopDong <= 0 || currentUserId <= 0)
            return new List<CancellationResponse>();

        bool owned = await _context.HopDongs
            .AsNoTracking()
            .AnyAsync(h => h.Id == idHopDong
                && h.IdYeuCauThueNavigation.IdKhachHangNavigation.IdNguoiDung == currentUserId);

        if (!owned)
            throw new UnauthorizedAccessException("Bạn không có quyền xem yêu cầu hủy của hợp đồng này.");

        return await GetByContractIdAsync(idHopDong);
    }

    // =========================================================
    // ENTITY -> DTO
    // =========================================================
    private static CancellationResponse MapToResponse(
        YeuCauHuyHopDong entity)
    {
        return new CancellationResponse
        {
            Id =
                entity.Id,

            IdHopDong =
                entity.IdHopDong,

            IdNguoiYeuCau =
                entity.IdNguoiYeuCau,

            LyDo =
                entity.LyDo,

            TrangThai =
                entity.TrangThai,

            SoTienHoanDuKien =
                entity.SoTienHoanDuKien,

            SoTienPhat =
                entity.SoTienPhat,

            IdNguoiXuLy =
                entity.IdNguoiXuLy,

            ThoiGianTao =
                entity.ThoiGianTao,

            ThoiGianXuLy =
                entity.ThoiGianXuLy
        };
    }
}