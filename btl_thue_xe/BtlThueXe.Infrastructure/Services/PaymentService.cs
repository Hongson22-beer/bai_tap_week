using BtlThueXe.Core.DTOs.Payments;
using BtlThueXe.Core.DTOs.AuditLogs;
using BtlThueXe.Infrastructure;
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
        CreatePaymentRequest request)
    {
        string[] validTypes =
        {
            "TIEN_THUE",
            "TIEN_COC",
            "PHI_PHAT_SINH",
            "HOAN_TIEN"
        };

        string[] validMethods =
        {
            "TIEN_MAT",
            "CHUYEN_KHOAN",
            "MO_PHONG"
        };

        string loaiThanhToan =
            request.LoaiThanhToan.Trim().ToUpperInvariant();

        string phuongThuc =
            request.PhuongThuc.Trim().ToUpperInvariant();

        if (!validTypes.Contains(loaiThanhToan))
        {
            throw new ArgumentException(
                "Loại thanh toán không hợp lệ.");
        }

        if (!validMethods.Contains(phuongThuc))
        {
            throw new ArgumentException(
                "Phương thức thanh toán không hợp lệ.");
        }

        if (request.SoTien < 0)
        {
            throw new ArgumentException(
                "Số tiền thanh toán không được âm.");
        }

        if (request.IdHopDong <= 0)
        {
            throw new ArgumentException(
                "ID hợp đồng không hợp lệ.");
        }

        /*
         * Sau khi merge phần Người 2 sẽ kiểm tra
         * hợp đồng trực tiếp bằng DbSet<HopDong>.
         */

        var payment = new ThanhToan
        {
            IdHopDong = request.IdHopDong,

            LoaiThanhToan = loaiThanhToan,

            SoTien = request.SoTien,

            PhuongThuc = phuongThuc,

            MaGiaoDich =
                string.IsNullOrWhiteSpace(request.MaGiaoDich)
                    ? null
                    : request.MaGiaoDich.Trim(),

            GhiChu =
                string.IsNullOrWhiteSpace(request.GhiChu)
                    ? null
                    : request.GhiChu.Trim(),

            TrangThai = "PENDING",

            ThoiGianThanhToan = null
        };

        /*
         * TIEN_MAT và MO_PHONG:
         * coi như thanh toán thành công ngay.
         *
         * CHUYEN_KHOAN:
         * giữ trạng thái PENDING.
         */
        if (phuongThuc == "TIEN_MAT" ||
            phuongThuc == "MO_PHONG")
        {
            payment.TrangThai = "PAID";
            payment.ThoiGianThanhToan = DateTime.UtcNow;
        }

        // =====================================================
        // LƯU THANH TOÁN
        // =====================================================
        _context.ThanhToans.Add(payment);

        await _context.SaveChangesAsync();

        // =====================================================
        // GHI AUDIT LOG TỰ ĐỘNG
        // =====================================================
        await _auditLogService.CreateAsync(
            new CreateAuditLogRequest
            {
                // Tạm thời chưa lấy user từ JWT.
                // Sau khi merge Auth/RBAC sẽ bổ sung.
                IdNguoiDung = null,

                HanhDong = "PAYMENT_CREATED",

                LoaiDoiTuong = "PAYMENT",

                IdDoiTuong = payment.Id,

                DuLieuCu = null,

                DuLieuMoi =
                    $"{{\"idHopDong\":{payment.IdHopDong}," +
                    $"\"loaiThanhToan\":\"{payment.LoaiThanhToan}\"," +
                    $"\"soTien\":{payment.SoTien}," +
                    $"\"phuongThuc\":\"{payment.PhuongThuc}\"," +
                    $"\"trangThai\":\"{payment.TrangThai}\"}}",

                MoTa =
                    $"Tạo thanh toán cho hợp đồng #{payment.IdHopDong}",

                IpAddress = null
            });

        return MapToResponse(payment);
    }

    // =========================================================
    // LẤY THANH TOÁN THEO ID
    // =========================================================
    public async Task<PaymentResponse?> GetByIdAsync(int id)
    {
        if (id <= 0)
            return null;

        var payment = await _context.ThanhToans
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (payment == null)
            return null;

        return MapToResponse(payment);
    }

    // =========================================================
    // LẤY DANH SÁCH THANH TOÁN THEO HỢP ĐỒNG
    // =========================================================
    public async Task<List<PaymentResponse>>
        GetByContractIdAsync(int idHopDong)
    {
        if (idHopDong <= 0)
        {
            return new List<PaymentResponse>();
        }

        var payments = await _context.ThanhToans
            .AsNoTracking()
            .Where(x => x.IdHopDong == idHopDong)
            .OrderByDescending(x => x.Id)
            .ToListAsync();

        return payments
            .Select(MapToResponse)
            .ToList();
    }

    // =========================================================
    // ENTITY -> RESPONSE DTO
    // =========================================================
    private static PaymentResponse MapToResponse(
        ThanhToan payment)
    {
        return new PaymentResponse
        {
            Id = payment.Id,

            IdHopDong = payment.IdHopDong,

            LoaiThanhToan = payment.LoaiThanhToan,

            SoTien = payment.SoTien,

            PhuongThuc = payment.PhuongThuc,

            MaGiaoDich = payment.MaGiaoDich,

            TrangThai = payment.TrangThai,

            GhiChu = payment.GhiChu,

            ThoiGianThanhToan =
                payment.ThoiGianThanhToan
        };
    }
}