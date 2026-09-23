using BtlThueXe.Core.DTOs.AuditLogs;
using BtlThueXe.Core.DTOs.Payments;
using BtlThueXe.Core.Interfaces;
using BtlThueXe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BtlThueXe.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public PaymentService(
        ApplicationDbContext context,
        IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    // =========================================================
    // TẠO THANH TOÁN
    // =========================================================
    public async Task<PaymentResponse> CreateAsync(
        CreatePaymentRequest request,
        int currentUserId,
        string? currentUserRole)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (currentUserId <= 0)
            throw new UnauthorizedAccessException(
                "Không xác định được người dùng.");

        if (request.IdHopDong <= 0)
            throw new ArgumentException(
                "ID hợp đồng không hợp lệ.");

        if (request.SoTien <= 0)
            throw new ArgumentException(
                "Số tiền thanh toán phải lớn hơn 0.");

        if (string.IsNullOrWhiteSpace(request.LoaiThanhToan))
            throw new ArgumentException(
                "Loại thanh toán không được để trống.");

        if (string.IsNullOrWhiteSpace(request.PhuongThuc))
            throw new ArgumentException(
                "Phương thức thanh toán không được để trống.");

        var loaiThanhToan =
            request.LoaiThanhToan.Trim().ToUpperInvariant();

        var phuongThuc =
            request.PhuongThuc.Trim().ToUpperInvariant();

        string[] validTypes =
        {
            "TIEN_THUE",
            "TIEN_COC",
            "PHI_PHAT_SINH"
        };

        string[] validMethods =
        {
            "TIEN_MAT",
            "CHUYEN_KHOAN",
            "MO_PHONG"
        };

        if (!validTypes.Contains(loaiThanhToan))
            throw new ArgumentException(
                "Loại thanh toán không hợp lệ.");

        if (!validMethods.Contains(phuongThuc))
            throw new ArgumentException(
                "Phương thức thanh toán không hợp lệ.");

        // =====================================================
        // TÌM HỢP ĐỒNG
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
        // KHÁCH CHỈ ĐƯỢC THANH TOÁN HỢP ĐỒNG CỦA MÌNH
        // =====================================================

        if (IsCustomer(currentUserRole))
        {
            var customer =
                hopDong.IdYeuCauThueNavigation
                    ?.IdKhachHangNavigation;

            if (customer == null)
                throw new InvalidOperationException(
                    "Không xác định được khách hàng của hợp đồng.");

            if (customer.IdNguoiDung != currentUserId)
                throw new UnauthorizedAccessException(
                    "Bạn không có quyền thanh toán hợp đồng này.");
        }

        // =====================================================
        // KIỂM TRA TRẠNG THÁI HỢP ĐỒNG
        // =====================================================

        if (loaiThanhToan == "TIEN_THUE" ||
            loaiThanhToan == "TIEN_COC")
        {
            if (!string.Equals(
                    hopDong.TrangThai,
                    "CUSTOMER_CONFIRMED",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Chỉ được thanh toán tiền thuê hoặc tiền cọc " +
                    "khi hợp đồng ở trạng thái CUSTOMER_CONFIRMED.");
            }
        }

        if (loaiThanhToan == "PHI_PHAT_SINH")
        {
            bool validState =
                string.Equals(
                    hopDong.TrangThai,
                    "IN_PROGRESS",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    hopDong.TrangThai,
                    "RETURNED",
                    StringComparison.OrdinalIgnoreCase);

            if (!validState)
                throw new InvalidOperationException(
                    "Hợp đồng chưa ở trạng thái cho phép " +
                    "thanh toán phí phát sinh.");
        }

        // =====================================================
        // CHỐNG GIAO DỊCH PENDING TRÙNG
        // =====================================================

        bool hasPending =
            await _context.ThanhToans.AnyAsync(x =>
                x.IdHopDong == request.IdHopDong &&
                x.LoaiThanhToan == loaiThanhToan &&
                x.TrangThai == "PENDING");

        if (hasPending)
            throw new InvalidOperationException(
                $"Đã tồn tại giao dịch {loaiThanhToan} " +
                "đang chờ xử lý.");

        // =====================================================
        // MÃ GIAO DỊCH
        // =====================================================

        var maGiaoDich =
            string.IsNullOrWhiteSpace(request.MaGiaoDich)
                ? null
                : request.MaGiaoDich.Trim();

        if (!string.IsNullOrWhiteSpace(maGiaoDich))
        {
            bool existed =
                await _context.ThanhToans.AnyAsync(x =>
                    x.MaGiaoDich == maGiaoDich);

            if (existed)
                throw new InvalidOperationException(
                    "Mã giao dịch đã tồn tại.");
        }
        else
        {
            maGiaoDich =
                $"TX-{DateTime.UtcNow:yyyyMMddHHmmss}-" +
                Guid.NewGuid()
                    .ToString("N")[..8]
                    .ToUpperInvariant();
        }

        // =====================================================
        // TẠO PAYMENT
        // =====================================================

        var payment = new ThanhToan
        {
            IdHopDong = request.IdHopDong,
            LoaiThanhToan = loaiThanhToan,
            SoTien = request.SoTien,
            PhuongThuc = phuongThuc,
            MaGiaoDich = maGiaoDich,

            GhiChu =
                string.IsNullOrWhiteSpace(request.GhiChu)
                    ? null
                    : request.GhiChu.Trim(),

            TrangThai = "PENDING",
            ThoiGianThanhToan = null
        };

        // Bản demo:
        // tiền mặt / mô phỏng coi như thanh toán thành công ngay.
        if (phuongThuc == "TIEN_MAT" ||
            phuongThuc == "MO_PHONG")
        {
            payment.TrangThai = "PAID";
            payment.ThoiGianThanhToan = DateTime.UtcNow;
        }

        _context.ThanhToans.Add(payment);

        await _context.SaveChangesAsync();

        // =====================================================
        // NẾU THANH TOÁN THÀNH CÔNG
        // KIỂM TRA ĐÃ ĐỦ TIỀN THUÊ + CỌC CHƯA
        // =====================================================

        if (payment.TrangThai == "PAID")
        {
            await UpdateContractAfterPaymentAsync(
                hopDong,
                currentUserId);

            await CompleteReturnedContractAfterExtraFeeAsync(
                hopDong,
                currentUserId);
        }

        await _context.SaveChangesAsync();

        // =====================================================
        // AUDIT
        // =====================================================

        await WriteAuditAsync(
            currentUserId,
            payment,
            payment.TrangThai == "PAID"
                ? "PAYMENT_PAID"
                : "PAYMENT_CREATED");

        return MapToResponse(payment);
    }

    // =========================================================
    // WEBHOOK THANH TOÁN
    // =========================================================

    public async Task<PaymentResponse> ProcessWebhookAsync(
        PaymentWebhookRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.MaGiaoDich))
            throw new ArgumentException(
                "Mã giao dịch không được để trống.");

        if (string.IsNullOrWhiteSpace(request.TrangThai))
            throw new ArgumentException(
                "Trạng thái không được để trống.");

        var status =
            request.TrangThai.Trim().ToUpperInvariant();

        if (status != "PAID" &&
            status != "FAILED")
        {
            throw new ArgumentException(
                "Trạng thái webhook chỉ được là PAID hoặc FAILED.");
        }

        var payment =
            await _context.ThanhToans
                .FirstOrDefaultAsync(x =>
                    x.MaGiaoDich ==
                    request.MaGiaoDich.Trim());

        if (payment == null)
            throw new KeyNotFoundException(
                "Không tìm thấy giao dịch.");

        if (!string.Equals(
                payment.PhuongThuc,
                "CHUYEN_KHOAN",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Webhook chỉ xử lý giao dịch chuyển khoản.");
        }

        // Webhook gọi lại cùng trạng thái -> không xử lý lại.
        if (string.Equals(
                payment.TrangThai,
                status,
                StringComparison.OrdinalIgnoreCase))
        {
            return MapToResponse(payment);
        }

        if (!string.Equals(
                payment.TrangThai,
                "PENDING",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Không thể chuyển thanh toán từ " +
                $"{payment.TrangThai} sang {status}.");
        }

        payment.TrangThai = status;

        if (status == "PAID")
            payment.ThoiGianThanhToan = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // =====================================================
        // NẾU WEBHOOK BÁO PAID
        // =====================================================

        if (status == "PAID")
        {
            var hopDong =
                await _context.HopDongs
                    .Include(x =>
                        x.IdYeuCauThueNavigation)
                        .ThenInclude(x =>
                            x.IdKhachHangNavigation)
                    .Include(x =>
                        x.IdYeuCauThueNavigation)
                        .ThenInclude(x =>
                            x.IdXeNavigation)
                    .FirstOrDefaultAsync(x =>
                        x.Id == payment.IdHopDong);

            if (hopDong == null)
                throw new KeyNotFoundException(
                    "Không tìm thấy hợp đồng của giao dịch.");

            await UpdateContractAfterPaymentAsync(
                hopDong,
                null);

            await CompleteReturnedContractAfterExtraFeeAsync(
                hopDong,
                null);

            await _context.SaveChangesAsync();
        }

        await WriteAuditAsync(
            null,
            payment,
            status == "PAID"
                ? "PAYMENT_WEBHOOK_PAID"
                : "PAYMENT_WEBHOOK_FAILED");

        return MapToResponse(payment);
    }

    // =========================================================
    // HOÀN TIỀN: REFUND_PENDING -> REFUNDED
    // Đồng thời tạo bản ghi HOAN_TIEN để truy vết đúng loại nghiệp vụ BA.
    // =========================================================
    public async Task<PaymentResponse> ProcessRefundAsync(
        int paymentId,
        int currentUserId)
    {
        if (currentUserId <= 0)
            throw new UnauthorizedAccessException("Không xác định được nhân viên.");

        var original = await _context.ThanhToans
            .FirstOrDefaultAsync(x => x.Id == paymentId);

        if (original == null)
            throw new KeyNotFoundException("Không tìm thấy thanh toán cần hoàn.");

        if (!string.Equals(original.TrangThai, "REFUND_PENDING", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ được hoàn giao dịch ở trạng thái REFUND_PENDING.");

        var refundCode = $"RF-{original.Id}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        var now = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            original.TrangThai = "REFUNDED";

            var refund = new ThanhToan
            {
                IdHopDong = original.IdHopDong,
                LoaiThanhToan = "HOAN_TIEN",
                SoTien = original.SoTien,
                PhuongThuc = original.PhuongThuc,
                MaGiaoDich = refundCode,
                TrangThai = "REFUNDED",
                GhiChu = $"Hoàn tiền cho giao dịch #{original.Id} ({original.MaGiaoDich}).",
                ThoiGianThanhToan = now
            };

            _context.ThanhToans.Add(refund);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await WriteAuditAsync(currentUserId, refund, "PAYMENT_REFUNDED");
            return MapToResponse(refund);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // =========================================================
    // LẤY PAYMENT THEO ID
    // =========================================================

    public async Task<PaymentResponse?> GetByIdAsync(
        int id)
    {
        if (id <= 0)
            return null;

        var payment =
            await _context.ThanhToans
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == id);

        return payment == null
            ? null
            : MapToResponse(payment);
    }

    // =========================================================
    // LẤY PAYMENT THEO HỢP ĐỒNG
    // =========================================================

    public async Task<List<PaymentResponse>>
        GetByContractIdAsync(
            int idHopDong)
    {
        if (idHopDong <= 0)
            return new List<PaymentResponse>();

        var payments =
            await _context.ThanhToans
                .AsNoTracking()
                .Where(x =>
                    x.IdHopDong == idHopDong)
                .OrderByDescending(x =>
                    x.Id)
                .ToListAsync();

        return payments
            .Select(MapToResponse)
            .ToList();
    }

    // =========================================================
    // KIỂM TRA THANH TOÁN ĐỦ TIỀN THUÊ + TIỀN CỌC
    // =========================================================

    private async Task UpdateContractAfterPaymentAsync(
        HopDong hopDong,
        int? currentUserId)
    {
        // Chỉ xử lý bước chuẩn bị giao xe ở trạng thái này.
        if (!string.Equals(
                hopDong.TrangThai,
                "CUSTOMER_CONFIRMED",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        decimal paidRent =
            await _context.ThanhToans
                .Where(x =>
                    x.IdHopDong == hopDong.Id &&
                    x.LoaiThanhToan == "TIEN_THUE" &&
                    x.TrangThai == "PAID")
                .SumAsync(x =>
                    (decimal?)x.SoTien)
                ?? 0m;

        decimal paidDeposit =
            await _context.ThanhToans
                .Where(x =>
                    x.IdHopDong == hopDong.Id &&
                    x.LoaiThanhToan == "TIEN_COC" &&
                    x.TrangThai == "PAID")
                .SumAsync(x =>
                    (decimal?)x.SoTien)
                ?? 0m;

        bool rentPaid =
            paidRent >= hopDong.TienThue;

        bool depositPaid =
            paidDeposit >= hopDong.TienCoc;

        if (!rentPaid || !depositPaid)
            return;

        var yeuCauThue =
            hopDong.IdYeuCauThueNavigation;

        if (yeuCauThue == null)
            throw new InvalidOperationException(
                "Hợp đồng không có yêu cầu thuê.");

        var xe =
            yeuCauThue.IdXeNavigation;

        if (xe == null)
            throw new InvalidOperationException(
                "Không tìm thấy xe của hợp đồng.");

        if (!string.Equals(
                xe.TrangThai,
                "AVAILABLE",
                StringComparison.OrdinalIgnoreCase)
            &&
            !string.Equals(
                xe.TrangThai,
                "RESERVED",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Xe đang ở trạng thái {xe.TrangThai}, " +
                "không thể chuẩn bị bàn giao.");
        }

        var now = DateTime.UtcNow;

        // =====================================================
        // CONTRACT:
        // CUSTOMER_CONFIRMED -> PAID
        // =====================================================

        hopDong.TrangThai = "PAID";

        _context.LichSuTrangThaiHopDongs.Add(
            new LichSuTrangThaiHopDong
            {
                IdHopDong = hopDong.Id,
                TrangThaiCu = "CUSTOMER_CONFIRMED",
                TrangThaiMoi = "PAID",
                IdNguoiThayDoi = currentUserId,
                LyDo =
                    "Đã thanh toán đủ tiền thuê và tiền cọc.",
                ThoiGianThayDoi = now
            });

        // =====================================================
        // VEHICLE:
        // AVAILABLE -> RESERVED
        // =====================================================

        if (!string.Equals(
                xe.TrangThai,
                "RESERVED",
                StringComparison.OrdinalIgnoreCase))
        {
            var oldVehicleStatus =
                xe.TrangThai;

            xe.TrangThai = "RESERVED";

            _context.LichSuTrangThaiXes.Add(
                new LichSuTrangThaiXe
                {
                    IdXe = xe.Id,
                    TrangThaiCu = oldVehicleStatus,
                    TrangThaiMoi = "RESERVED",
                    IdNguoiThayDoi = currentUserId,
                    LyDo =
                        $"Hợp đồng #{hopDong.Id} đã thanh toán đủ.",
                    ThoiGianThayDoi = now
                });
        }

        // =====================================================
        // CONTRACT:
        // PAID -> READY_FOR_PICKUP
        // =====================================================

        hopDong.TrangThai =
            "READY_FOR_PICKUP";

        _context.LichSuTrangThaiHopDongs.Add(
            new LichSuTrangThaiHopDong
            {
                IdHopDong = hopDong.Id,
                TrangThaiCu = "PAID",
                TrangThaiMoi =
                    "READY_FOR_PICKUP",
                IdNguoiThayDoi =
                    currentUserId,
                LyDo =
                    "Hợp đồng đã sẵn sàng bàn giao xe.",
                ThoiGianThayDoi =
                    now
            });
    }

    // =========================================================
    // SAU KHI TRẢ XE CÓ PHÍ: chỉ COMPLETED khi đã thanh toán đủ phí.
    // =========================================================
    private async Task CompleteReturnedContractAfterExtraFeeAsync(
        HopDong hopDong,
        int? currentUserId)
    {
        if (!string.Equals(hopDong.TrangThai, "RETURNED", StringComparison.OrdinalIgnoreCase))
            return;

        var returnInfo = await _context.TraXes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdHopDong == hopDong.Id);

        if (returnInfo == null)
            return;

        decimal requiredFee = returnInfo.TongPhiPhatSinh;
        if (requiredFee <= 0)
            return;

        decimal paidFee = await _context.ThanhToans
            .Where(x => x.IdHopDong == hopDong.Id &&
                        x.LoaiThanhToan == "PHI_PHAT_SINH" &&
                        x.TrangThai == "PAID")
            .SumAsync(x => (decimal?)x.SoTien) ?? 0m;

        if (paidFee < requiredFee)
            return;

        hopDong.TrangThai = "COMPLETED";
        hopDong.ThoiGianCapNhat = DateTime.UtcNow;

        _context.LichSuTrangThaiHopDongs.Add(
            new LichSuTrangThaiHopDong
            {
                IdHopDong = hopDong.Id,
                TrangThaiCu = "RETURNED",
                TrangThaiMoi = "COMPLETED",
                IdNguoiThayDoi = currentUserId,
                LyDo = "Đã thanh toán đủ phí phát sinh sau khi trả xe.",
                ThoiGianThayDoi = DateTime.UtcNow
            });
    }

    // =========================================================
    // KIỂM TRA ROLE
    // =========================================================

    private static bool IsCustomer(
        string? role)
    {
        return string.Equals(
            role,
            "KHACH_HANG",
            StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================
    // AUDIT
    // =========================================================

    private async Task WriteAuditAsync(
        int? userId,
        ThanhToan payment,
        string action)
    {
        await _auditLogService.CreateAsync(
            new CreateAuditLogRequest
            {
                IdNguoiDung = userId,

                HanhDong = action,

                LoaiDoiTuong =
                    "PAYMENT",

                IdDoiTuong =
                    payment.Id,

                MoTa =
                    $"Thanh toán #{payment.Id} - " +
                    $"Hợp đồng #{payment.IdHopDong} - " +
                    $"Trạng thái {payment.TrangThai}"
            });
    }

    // =========================================================
    // ENTITY -> RESPONSE
    // =========================================================

    private static PaymentResponse MapToResponse(
        ThanhToan payment)
    {
        return new PaymentResponse
        {
            Id = payment.Id,
            IdHopDong = payment.IdHopDong,
            LoaiThanhToan =
                payment.LoaiThanhToan,
            SoTien = payment.SoTien,
            PhuongThuc =
                payment.PhuongThuc,
            MaGiaoDich =
                payment.MaGiaoDich,
            TrangThai =
                payment.TrangThai,
            GhiChu =
                payment.GhiChu,
            ThoiGianThanhToan =
                payment.ThoiGianThanhToan
        };
    }
}