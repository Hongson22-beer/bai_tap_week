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

        if (request.SoTienHoanDuKien < 0 ||
            request.SoTienPhat < 0)
        {
            throw new ArgumentException(
                "Số tiền hoàn và tiền phạt không được nhỏ hơn 0.");
        }

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
            // PAYMENT REFUND
            //
            // Nếu có khoản PAID và có tiền cần hoàn,
            // chuyển sang REFUND_PENDING.
            // =================================================
            if (cancellation.SoTienHoanDuKien > 0)
            {
                var paidPayments =
                    await _context.ThanhToans
                        .Where(x =>
                            x.IdHopDong == hopDong.Id &&
                            x.TrangThai == "PAID")
                        .ToListAsync();

                foreach (var payment in paidPayments)
                {
                    payment.TrangThai =
                        "REFUND_PENDING";
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