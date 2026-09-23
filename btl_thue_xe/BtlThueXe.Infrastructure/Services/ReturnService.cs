using BtlThueXe.Core.DTOs.Returns;
using BtlThueXe.Core.Interfaces;
using BtlThueXe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BtlThueXe.Infrastructure.Services;

public class ReturnService : IReturnService
{
    private readonly ApplicationDbContext _context;

    public ReturnService(ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // TẠO PHIẾU TRẢ XE
    // =========================================================
    public async Task<ReturnResponse> CreateAsync(
        CreateReturnRequest request,
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

        if (request.PhiTraMuon < 0 ||
            request.PhiPhatSinh < 0)
        {
            throw new ArgumentException(
                "Phí phát sinh không được nhỏ hơn 0.");
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
        {
            throw new KeyNotFoundException(
                "Không tìm thấy hợp đồng.");
        }

        // =====================================================
        // CHỈ ĐƯỢC TRẢ XE KHI HỢP ĐỒNG ĐANG IN_PROGRESS
        // =====================================================
        if (!string.Equals(
                hopDong.TrangThai,
                "IN_PROGRESS",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Không thể trả xe khi hợp đồng đang ở trạng thái " +
                $"{hopDong.TrangThai}. Hợp đồng phải ở IN_PROGRESS.");
        }

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

        // =====================================================
        // KHÔNG TIN IdXe TỪ CLIENT
        // =====================================================
        if (request.IdXe > 0 &&
            request.IdXe != xe.Id)
        {
            throw new InvalidOperationException(
                "Xe trả không đúng với xe của hợp đồng.");
        }

        // =====================================================
        // XE PHẢI ĐANG RENTING
        // =====================================================
        if (!string.Equals(
                xe.TrangThai,
                "RENTING",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Không thể trả xe vì xe đang ở trạng thái " +
                $"{xe.TrangThai}. Xe phải ở trạng thái RENTING.");
        }

        // =====================================================
        // PHẢI CÓ BẢN GHI BÀN GIAO
        // =====================================================
        var handover = await _context.BanGiaoXes
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.IdHopDong == hopDong.Id);

        if (handover == null)
        {
            throw new InvalidOperationException(
                "Hợp đồng chưa có thông tin bàn giao xe.");
        }

        // =====================================================
        // KM TRẢ KHÔNG THỂ NHỎ HƠN KM LÚC GIAO
        // =====================================================
        if (request.SoKm < handover.SoKm)
        {
            throw new InvalidOperationException(
                $"Số km khi trả ({request.SoKm}) không thể nhỏ hơn " +
                $"số km lúc bàn giao ({handover.SoKm}).");
        }

        // =====================================================
        // KHÔNG CHO TRẢ XE 2 LẦN
        // =====================================================
        bool returnExisted = await _context.TraXes
            .AnyAsync(x =>
                x.IdHopDong == hopDong.Id);

        if (returnExisted)
        {
            throw new InvalidOperationException(
                "Hợp đồng này đã có thông tin trả xe.");
        }

        // =====================================================
        // THỜI GIAN TRẢ THỰC TẾ
        // LẤY TỪ SERVER, KHÔNG TIN CLIENT
        // =====================================================
        var now = DateTime.UtcNow;

        decimal tongPhiPhatSinh =
            request.PhiTraMuon +
            request.PhiPhatSinh;

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // =================================================
            // TẠO PHIẾU TRẢ XE
            // =================================================
            var returnEntity = new TraXe
            {
                IdHopDong = hopDong.Id,

                // Xe thực tế lấy từ hợp đồng
                IdXe = xe.Id,

                // Nhân viên lấy từ JWT
                IdNhanVien = currentUserId,

                // Thời gian dự kiến lấy từ hợp đồng.
                // Nếu hợp đồng đã gia hạn thì đây là thời gian mới nhất.
                ThoiGianTraDuKien =
                    hopDong.ThoiGianTraDuKien,

                // Thời gian trả thực tế lấy từ server
                ThoiGianTraThucTe =
                    now,

                SoKm =
                    request.SoKm,

                MucNhienLieu =
                    request.MucNhienLieu,

                TinhTrangXe =
                    string.IsNullOrWhiteSpace(request.TinhTrangXe)
                        ? null
                        : request.TinhTrangXe.Trim(),

                GhiChu =
                    string.IsNullOrWhiteSpace(request.GhiChu)
                        ? null
                        : request.GhiChu.Trim(),

                PhiTraMuon =
                    request.PhiTraMuon,

                PhiPhatSinh =
                    request.PhiPhatSinh,

                TongPhiPhatSinh =
                    tongPhiPhatSinh,

                TrangThai =
                    "COMPLETED",

                ThoiGianTao =
                    DateTime.UtcNow
            };

            _context.TraXes.Add(returnEntity);

            // =================================================
            // CẬP NHẬT HỢP ĐỒNG
            // IN_PROGRESS -> RETURNED
            // =================================================
            string? oldContractStatus =
                hopDong.TrangThai;

            hopDong.TrangThai =
                "RETURNED";

            hopDong.ThoiGianTraThucTe =
                now;

            _context.LichSuTrangThaiHopDongs.Add(
                new LichSuTrangThaiHopDong
                {
                    IdHopDong =
                        hopDong.Id,

                    TrangThaiCu =
                        oldContractStatus,

                    TrangThaiMoi =
                        "RETURNED",

                    IdNguoiThayDoi =
                        currentUserId,

                    LyDo =
                        "Nhân viên đã tiếp nhận xe trả.",

                    ThoiGianThayDoi =
                        DateTime.UtcNow
                });

            // =================================================
            // XE RENTING -> AVAILABLE
            // =================================================
            string? oldVehicleStatus =
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
                        $"Khách hàng trả xe của hợp đồng #{hopDong.Id}.",

                    ThoiGianThayDoi =
                        DateTime.UtcNow
                });

            // =================================================
            // RETURNED -> COMPLETED
            // Sau khi tiếp nhận xe thành công, hợp đồng hoàn tất.
            // =================================================
            hopDong.TrangThai =
                "COMPLETED";

            _context.LichSuTrangThaiHopDongs.Add(
                new LichSuTrangThaiHopDong
                {
                    IdHopDong =
                        hopDong.Id,

                    TrangThaiCu =
                        "RETURNED",

                    TrangThaiMoi =
                        "COMPLETED",

                    IdNguoiThayDoi =
                        currentUserId,

                    LyDo =
                        "Hoàn tất quy trình trả xe.",

                    ThoiGianThayDoi =
                        DateTime.UtcNow
                });

            // =================================================
            // AUDIT LOG
            // =================================================
            _context.AuditLogs.Add(new AuditLog
            {
                IdNguoiDung = currentUserId,
                HanhDong = "RETURN_VEHICLE",
                LoaiDoiTuong = "HOP_DONG",
                IdDoiTuong = hopDong.Id,
                DuLieuCu =
                    $"Contract={oldContractStatus}; Vehicle={oldVehicleStatus}",
                DuLieuMoi =
                    $"Contract=COMPLETED; Vehicle=AVAILABLE; " +
                    $"TongPhiPhatSinh={tongPhiPhatSinh}",
                MoTa =
                    $"Tiếp nhận xe #{xe.Id} trả cho hợp đồng #{hopDong.Id}.",
                ThoiGian =
                    DateTime.UtcNow
            });

            // =================================================
            // SAVE
            // =================================================
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return MapToResponse(returnEntity);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // =========================================================
    // GET RETURN BY ID
    // =========================================================
    public async Task<ReturnResponse?> GetByIdAsync(
        int id)
    {
        if (id <= 0)
            return null;

        var entity = await _context.TraXes
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == id);

        return entity == null
            ? null
            : MapToResponse(entity);
    }

    // =========================================================
    // GET RETURN BY CONTRACT
    // =========================================================
    public async Task<ReturnResponse?> GetByContractIdAsync(
        int idHopDong)
    {
        if (idHopDong <= 0)
            return null;

        var entity = await _context.TraXes
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.IdHopDong == idHopDong);

        return entity == null
            ? null
            : MapToResponse(entity);
    }

    // =========================================================
    // ENTITY -> DTO
    // =========================================================
    private static ReturnResponse MapToResponse(
        TraXe entity)
    {
        return new ReturnResponse
        {
            Id =
                entity.Id,

            IdHopDong =
                entity.IdHopDong,

            IdXe =
                entity.IdXe,

            ThoiGianTraDuKien =
                entity.ThoiGianTraDuKien,

            ThoiGianTraThucTe =
                entity.ThoiGianTraThucTe,

            SoKm =
                entity.SoKm,

            MucNhienLieu =
                entity.MucNhienLieu,

            TinhTrangXe =
                entity.TinhTrangXe,

            GhiChu =
                entity.GhiChu,

            PhiTraMuon =
                entity.PhiTraMuon,

            PhiPhatSinh =
                entity.PhiPhatSinh,

            TongPhiPhatSinh =
                entity.TongPhiPhatSinh,

            IdNhanVien =
                entity.IdNhanVien,

            TrangThai =
                entity.TrangThai,

            ThoiGianTao =
                entity.ThoiGianTao
        };
    }
}