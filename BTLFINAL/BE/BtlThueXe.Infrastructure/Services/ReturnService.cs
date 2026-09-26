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

        // QUYẾT TOÁN MỘT LẦN KHI TRẢ XE:
        // tiền thuê gốc + phí gia hạn + phí trả muộn/hư hỏng/khác - các khoản đã PAID.
        decimal phiGiaHan = await _context.YeuCauGiaHans
            .Where(x => x.IdHopDong == hopDong.Id && x.TrangThai == "APPROVED")
            .SumAsync(x => (decimal?)x.TienPhatSinh) ?? 0m;

        decimal daThanhToan = await _context.ThanhToans
            .Where(x => x.IdHopDong == hopDong.Id &&
                        x.TrangThai == "PAID" &&
                        (x.LoaiThanhToan == "TIEN_COC" ||
                         x.LoaiThanhToan == "TIEN_THUE" ||
                         x.LoaiThanhToan == "PHI_PHAT_SINH"))
            .SumAsync(x => (decimal?)x.SoTien) ?? 0m;

        decimal tongGiaTriCuoi =
            hopDong.TienThue + phiGiaHan + request.PhiTraMuon + request.PhiPhatSinh;

        decimal tongPhiPhatSinh = Math.Max(0m, tongGiaTriCuoi - daThanhToan);

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

                    LyDo = request.CanBaoTri
                        ? "Nhân viên tiếp nhận xe trả và ghi nhận xe hư hỏng cần bảo trì."
                        : "Nhân viên đã tiếp nhận xe trả, xe sẵn sàng cho lượt thuê tiếp theo.",

                    ThoiGianThayDoi =
                        DateTime.UtcNow
                });

            // =================================================
            // XE RENTING -> AVAILABLE / MAINTENANCE
            // Xe hư hỏng cần bảo trì không được đưa trở lại danh sách sẵn sàng.
            // =================================================
            string? oldVehicleStatus =
                xe.TrangThai;

            string nextVehicleStatus = request.CanBaoTri
                ? "MAINTENANCE"
                : "AVAILABLE";

            xe.TrangThai = nextVehicleStatus;

            _context.LichSuTrangThaiXes.Add(
                new LichSuTrangThaiXe
                {
                    IdXe =
                        xe.Id,

                    TrangThaiCu =
                        oldVehicleStatus,

                    TrangThaiMoi =
                        nextVehicleStatus,

                    IdNguoiThayDoi =
                        currentUserId,

                    LyDo = request.CanBaoTri
                        ? $"Xe trả từ hợp đồng #{hopDong.Id} có hư hỏng, chuyển sang bảo trì."
                        : $"Khách hàng trả xe của hợp đồng #{hopDong.Id}, xe sẵn sàng.",

                    ThoiGianThayDoi =
                        DateTime.UtcNow
                });

            // =================================================
            // RETURNED -> COMPLETED chỉ khi KHÔNG có phí phát sinh.
            // Nếu có phí, giữ RETURNED để khách/nhân viên thanh toán
            // PHI_PHAT_SINH; PaymentService sẽ hoàn tất hợp đồng sau.
            // =================================================
            if (tongPhiPhatSinh <= 0)
            {
                hopDong.TrangThai = "COMPLETED";

                _context.LichSuTrangThaiHopDongs.Add(
                    new LichSuTrangThaiHopDong
                    {
                        IdHopDong = hopDong.Id,
                        TrangThaiCu = "RETURNED",
                        TrangThaiMoi = "COMPLETED",
                        IdNguoiThayDoi = currentUserId,
                        LyDo = "Hoàn tất quy trình trả xe, không có phí phát sinh.",
                        ThoiGianThayDoi = DateTime.UtcNow
                    });
            }

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
                    $"Contract={hopDong.TrangThai}; Vehicle=AVAILABLE; " +
                    $"QuyetToanConLai={tongPhiPhatSinh}; PhiGiaHan={phiGiaHan}; DaThanhToan={daThanhToan}",
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
    // GET RETURN BY CONTRACT CÓ KIỂM TRA QUYỀN
    // KHÁCH HÀNG CHỈ ĐƯỢC XEM PHIẾU TRẢ XE CỦA CHÍNH MÌNH
    // =========================================================
    public async Task<ReturnResponse?> GetByContractForUserAsync(
        int idHopDong, int currentUserId, bool isStaff)
    {
        if (idHopDong <= 0)
            return null;

        var entity = await _context.TraXes
            .AsNoTracking()
            .Include(x => x.IdHopDongNavigation)
                .ThenInclude(x => x.IdYeuCauThueNavigation)
                    .ThenInclude(x => x.IdKhachHangNavigation)
            .FirstOrDefaultAsync(x => x.IdHopDong == idHopDong);

        if (entity == null)
            return null;

        if (!isStaff)
        {
            var ownerUserId = entity.IdHopDongNavigation?
                .IdYeuCauThueNavigation?
                .IdKhachHangNavigation?
                .IdNguoiDung;

            if (ownerUserId != currentUserId)
                throw new UnauthorizedAccessException(
                    "Bạn không có quyền xem thông tin trả xe của hợp đồng này.");
        }

        return MapToResponse(entity);
    }

    // =========================================================
    // KHÁCH HÀNG GỬI YÊU CẦU TRẢ XE
    // =========================================================
    public async Task<ReturnIntentResponse> RequestReturnAsync(
        int idHopDong,
        CreateReturnIntentRequest request,
        int currentUserId)
    {
        if (idHopDong <= 0) throw new ArgumentException("ID hợp đồng không hợp lệ.");
        if (currentUserId <= 0) throw new UnauthorizedAccessException("Không xác định được khách hàng.");

        var customer = await _context.KhachHangs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdNguoiDung == currentUserId)
            ?? throw new UnauthorizedAccessException("Không tìm thấy hồ sơ khách hàng của tài khoản này.");

        var contract = await _context.HopDongs
            .AsNoTracking()
            .Include(x => x.IdYeuCauThueNavigation)
            .FirstOrDefaultAsync(x => x.Id == idHopDong)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");

        if (contract.IdYeuCauThueNavigation?.IdKhachHang != customer.Id)
            throw new UnauthorizedAccessException("Bạn không có quyền yêu cầu trả xe cho hợp đồng này.");

        if (!string.Equals(contract.TrangThai, "IN_PROGRESS", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ có thể yêu cầu trả xe khi hợp đồng đang trong thời gian thuê.");

        var existed = await GetReturnRequestInternalAsync(idHopDong);
        if (existed != null)
            return existed;

        var note = string.IsNullOrWhiteSpace(request?.GhiChu) ? null : request.GhiChu.Trim();
        var now = DateTime.UtcNow;
        var pendingStatus = "PENDING";

        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO yeu_cau_tra_xe
                (id_hop_dong, id_khach_hang, ghi_chu, trang_thai, thoi_gian_yeu_cau)
            VALUES
                ({idHopDong}, {customer.Id}, {note}, {pendingStatus}, {now})");

        return await GetReturnRequestInternalAsync(idHopDong)
            ?? throw new InvalidOperationException("Không thể tạo yêu cầu trả xe.");
    }

    public async Task<ReturnIntentResponse?> GetReturnRequestAsync(
        int idHopDong,
        int currentUserId,
        bool isStaffOrAdmin)
    {
        if (idHopDong <= 0) return null;

        if (!isStaffOrAdmin)
        {
            var customer = await _context.KhachHangs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdNguoiDung == currentUserId)
                ?? throw new UnauthorizedAccessException("Không tìm thấy hồ sơ khách hàng.");

            var owns = await _context.HopDongs
                .AsNoTracking()
                .Include(x => x.IdYeuCauThueNavigation)
                .AnyAsync(x => x.Id == idHopDong &&
                               x.IdYeuCauThueNavigation.IdKhachHang == customer.Id);
            if (!owns) throw new UnauthorizedAccessException("Bạn không có quyền xem yêu cầu trả xe này.");
        }

        return await GetReturnRequestInternalAsync(idHopDong);
    }

    private async Task<ReturnIntentResponse?> GetReturnRequestInternalAsync(int idHopDong)
    {
        var connection = _context.Database.GetDbConnection();
        var mustClose = connection.State != System.Data.ConnectionState.Open;
        if (mustClose) await connection.OpenAsync();
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT id, id_hop_dong, id_khach_hang, ghi_chu, trang_thai,
                       thoi_gian_yeu_cau, thoi_gian_xu_ly
                FROM yeu_cau_tra_xe
                WHERE id_hop_dong = @id
                LIMIT 1";
            var p = cmd.CreateParameter(); p.ParameterName = "@id"; p.Value = idHopDong; cmd.Parameters.Add(p);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return new ReturnIntentResponse
            {
                Id = reader.GetInt32(0),
                IdHopDong = reader.GetInt32(1),
                IdKhachHang = reader.GetInt32(2),
                GhiChu = reader.IsDBNull(3) ? null : reader.GetString(3),
                TrangThai = reader.GetString(4),
                ThoiGianYeuCau = reader.GetDateTime(5),
                ThoiGianXuLy = reader.IsDBNull(6) ? null : reader.GetDateTime(6)
            };
        }
        finally { if (mustClose) await connection.CloseAsync(); }
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