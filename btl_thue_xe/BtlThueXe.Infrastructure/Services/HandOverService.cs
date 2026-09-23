using BtlThueXe.Core.DTOs.Handovers;
using BtlThueXe.Core.Interfaces;
using BtlThueXe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BtlThueXe.Infrastructure.Services;

public class HandOverService : IHandOverService
{
    private readonly ApplicationDbContext _context;

    public HandOverService(ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // TẠO BÀN GIAO XE
    // =========================================================
    public async Task<HandoverResponse> CreateAsync(
        CreateHandoverRequest request,
        int currentUserId)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (currentUserId <= 0)
            throw new UnauthorizedAccessException(
                "Không xác định được nhân viên.");

        if (request.IdHopDong <= 0)
            throw new ArgumentException(
                "ID hợp đồng không hợp lệ.");

        if (request.SoKm < 0)
            throw new ArgumentException(
                "Số km không được nhỏ hơn 0.");

        if (request.MucNhienLieu < 0 ||
            request.MucNhienLieu > 100)
        {
            throw new ArgumentException(
                "Mức nhiên liệu phải nằm trong khoảng từ 0 đến 100.");
        }

        // =====================================================
        // LẤY HỢP ĐỒNG + YÊU CẦU THUÊ + XE
        // =====================================================
        var hopDong = await _context.HopDongs
            .Include(x => x.IdYeuCauThueNavigation)
                .ThenInclude(x => x.IdXeNavigation)
            .FirstOrDefaultAsync(x =>
                x.Id == request.IdHopDong);

        if (hopDong == null)
            throw new KeyNotFoundException(
                "Không tìm thấy hợp đồng.");

        // =====================================================
        // CHỈ BÀN GIAO KHI READY_FOR_PICKUP
        // =====================================================
        if (!string.Equals(
                hopDong.TrangThai,
                "READY_FOR_PICKUP",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Không thể bàn giao xe khi hợp đồng đang ở trạng thái " +
                $"{hopDong.TrangThai}. Hợp đồng phải ở READY_FOR_PICKUP.");
        }

        // =====================================================
        // MỖI HỢP ĐỒNG CHỈ ĐƯỢC BÀN GIAO 1 LẦN
        // =====================================================
        bool existed = await _context.BanGiaoXes
            .AnyAsync(x =>
                x.IdHopDong == request.IdHopDong);

        if (existed)
        {
            throw new InvalidOperationException(
                "Hợp đồng này đã có thông tin bàn giao xe.");
        }

        // =====================================================
        // LẤY XE TỪ YÊU CẦU THUÊ
        // =====================================================
        var yeuCauThue =
            hopDong.IdYeuCauThueNavigation;

        if (yeuCauThue == null)
        {
            throw new InvalidOperationException(
                "Không tìm thấy yêu cầu thuê của hợp đồng.");
        }

        var xe = yeuCauThue.IdXeNavigation;

        if (xe == null)
        {
            throw new InvalidOperationException(
                "Không tìm thấy xe của hợp đồng.");
        }

        // Không tin IdXe do client gửi lên.
        // Nếu client gửi IdXe thì phải đúng xe của hợp đồng.
        if (request.IdXe > 0 &&
            request.IdXe != xe.Id)
        {
            throw new InvalidOperationException(
                "Xe bàn giao không đúng với xe của hợp đồng.");
        }

        // =====================================================
        // XE PHẢI RESERVED
        // =====================================================
        if (!string.Equals(
                xe.TrangThai,
                "RESERVED",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Không thể bàn giao xe vì xe đang ở trạng thái " +
                $"{xe.TrangThai}. Xe phải ở trạng thái RESERVED.");
        }

        var now = DateTime.UtcNow;

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // =================================================
            // TẠO BÀN GIAO
            // =================================================
            var handover = new BanGiaoXe
            {
                IdHopDong = hopDong.Id,

                // Lấy xe thực tế từ hợp đồng
                IdXe = xe.Id,

                // Lấy nhân viên từ JWT,
                // không sử dụng IdNhanVien client gửi lên
                IdNhanVien = currentUserId,

                SoKm = request.SoKm,

                MucNhienLieu = request.MucNhienLieu,

                TinhTrangXe =
                    string.IsNullOrWhiteSpace(request.TinhTrangXe)
                        ? null
                        : request.TinhTrangXe.Trim(),

                GhiChu =
                    string.IsNullOrWhiteSpace(request.GhiChu)
                        ? null
                        : request.GhiChu.Trim(),

                ThoiGianGiao = now
            };

            _context.BanGiaoXes.Add(handover);

            // =================================================
            // CONTRACT
            // READY_FOR_PICKUP -> IN_PROGRESS
            // =================================================
            string? oldContractStatus =
                hopDong.TrangThai;

            hopDong.TrangThai =
                "IN_PROGRESS";

            hopDong.ThoiGianNhanThucTe =
                now;

            _context.LichSuTrangThaiHopDongs.Add(
                new LichSuTrangThaiHopDong
                {
                    IdHopDong = hopDong.Id,

                    TrangThaiCu =
                        oldContractStatus,

                    TrangThaiMoi =
                        "IN_PROGRESS",

                    IdNguoiThayDoi =
                        currentUserId,

                    LyDo =
                        "Nhân viên bàn giao xe cho khách hàng.",

                    ThoiGianThayDoi =
                        now
                });

            // =================================================
            // VEHICLE
            // RESERVED -> RENTING
            // =================================================
            string? oldVehicleStatus =
                xe.TrangThai;

            xe.TrangThai =
                "RENTING";

            _context.LichSuTrangThaiXes.Add(
                new LichSuTrangThaiXe
                {
                    IdXe = xe.Id,

                    TrangThaiCu =
                        oldVehicleStatus,

                    TrangThaiMoi =
                        "RENTING",

                    IdNguoiThayDoi =
                        currentUserId,

                    LyDo =
                        $"Bàn giao xe cho hợp đồng #{hopDong.Id}.",

                    ThoiGianThayDoi =
                        now
                });

            // =================================================
            // AUDIT LOG
            // =================================================
            _context.AuditLogs.Add(new AuditLog
            {
                IdNguoiDung = currentUserId,
                HanhDong = "HANDOVER_VEHICLE",
                LoaiDoiTuong = "HOP_DONG",
                IdDoiTuong = hopDong.Id,
                DuLieuCu = $"Contract={oldContractStatus}; Vehicle={oldVehicleStatus}",
                DuLieuMoi = "Contract=IN_PROGRESS; Vehicle=RENTING",
                MoTa = $"Bàn giao xe #{xe.Id} cho hợp đồng #{hopDong.Id}.",
                ThoiGian = now
            });

            // =================================================
            // LƯU TOÀN BỘ TRONG TRANSACTION
            // =================================================
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return MapToResponse(handover);
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
    public async Task<HandoverResponse?> GetByIdAsync(
        int id)
    {
        if (id <= 0)
            return null;

        var handover = await _context.BanGiaoXes
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == id);

        if (handover == null)
            return null;

        return MapToResponse(handover);
    }

    // =========================================================
    // GET BY CONTRACT
    // =========================================================
    public async Task<HandoverResponse?> GetByContractIdAsync(
        int idHopDong)
    {
        if (idHopDong <= 0)
            return null;

        var handover = await _context.BanGiaoXes
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.IdHopDong == idHopDong);

        if (handover == null)
            return null;

        return MapToResponse(handover);
    }

    // =========================================================
    // ENTITY -> DTO
    // =========================================================
    private static HandoverResponse MapToResponse(
        BanGiaoXe handover)
    {
        return new HandoverResponse
        {
            Id = handover.Id,

            IdHopDong =
                handover.IdHopDong,

            IdXe =
                handover.IdXe,

            SoKm =
                handover.SoKm,

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