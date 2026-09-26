using BtlThueXe.Core.DTOs.Evaluations;
using BtlThueXe.Core.Interfaces;
using BtlThueXe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BtlThueXe.Infrastructure.Services;

public class EvaluationService : IEvaluationService
{
    private readonly ApplicationDbContext _context;

    public EvaluationService(ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // KHÁCH HÀNG TẠO ĐÁNH GIÁ
    // =========================================================
    public async Task<EvaluationResponse> CreateAsync(
        CreateEvaluationRequest request,
        int currentUserId)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (currentUserId <= 0)
        {
            throw new UnauthorizedAccessException(
                "Không xác định được người dùng.");
        }

        if (request.IdHopDong <= 0)
        {
            throw new ArgumentException(
                "ID hợp đồng không hợp lệ.");
        }

        if (request.DiemDanhGia < 1 ||
            request.DiemDanhGia > 5)
        {
            throw new ArgumentException(
                "Điểm đánh giá phải nằm trong khoảng từ 1 đến 5.");
        }

        if (!string.IsNullOrWhiteSpace(request.NhanXet) &&
            request.NhanXet.Length > 2000)
        {
            throw new ArgumentException(
                "Nhận xét không được vượt quá 2000 ký tự.");
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
        {
            throw new KeyNotFoundException(
                "Không tìm thấy hợp đồng.");
        }

        // =====================================================
        // CHỈ ĐÁNH GIÁ KHI HỢP ĐỒNG COMPLETED
        // =====================================================
        if (!string.Equals(
                hopDong.TrangThai,
                "COMPLETED",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Không thể đánh giá khi hợp đồng đang ở trạng thái " +
                $"{hopDong.TrangThai}. Hợp đồng phải ở COMPLETED.");
        }

        var yeuCauThue =
            hopDong.IdYeuCauThueNavigation;

        if (yeuCauThue == null)
        {
            throw new InvalidOperationException(
                "Không tìm thấy yêu cầu thuê của hợp đồng.");
        }

        var khachHang =
            yeuCauThue.IdKhachHangNavigation;

        if (khachHang == null)
        {
            throw new InvalidOperationException(
                "Không tìm thấy khách hàng của hợp đồng.");
        }

        // =====================================================
        // KIỂM TRA QUYỀN SỞ HỮU
        //
        // JWT:
        // currentUserId = NguoiDung.Id
        //
        // DanhGia:
        // IdKhachHang = KhachHang.Id
        //
        // Vì vậy KHÔNG gán:
        // IdKhachHang = currentUserId
        // =====================================================
        if (khachHang.IdNguoiDung != currentUserId)
        {
            throw new UnauthorizedAccessException(
                "Bạn không có quyền đánh giá hợp đồng này.");
        }

        // =====================================================
        // MỖI HỢP ĐỒNG CHỈ ĐƯỢC ĐÁNH GIÁ 1 LẦN
        // =====================================================
        bool evaluationExists =
            await _context.DanhGias.AnyAsync(x =>
                x.IdHopDong == hopDong.Id);

        if (evaluationExists)
        {
            throw new InvalidOperationException(
                "Hợp đồng này đã được đánh giá.");
        }

        // =====================================================
        // LẤY XE THỰC TẾ TỪ HỢP ĐỒNG
        // Không tin IdXe client gửi lên.
        // =====================================================
        int idXe = yeuCauThue.IdXe;

        if (request.IdXe.HasValue &&
            request.IdXe.Value > 0 &&
            request.IdXe.Value != idXe)
        {
            throw new InvalidOperationException(
                "Xe đánh giá không đúng với xe của hợp đồng.");
        }

        // =====================================================
        // TẠO ĐÁNH GIÁ
        // =====================================================
        var evaluation = new DanhGia
        {
            IdHopDong =
                hopDong.Id,

            // Đây là KhachHang.Id, KHÔNG phải NguoiDung.Id
            IdKhachHang =
                khachHang.Id,

            IdXe =
                idXe,

            DiemDanhGia =
                request.DiemDanhGia,

            NhanXet =
                string.IsNullOrWhiteSpace(request.NhanXet)
                    ? null
                    : request.NhanXet.Trim(),

            ThoiGianTao =
                DateTime.UtcNow,

            HienThi = true
        };

        _context.DanhGias.Add(evaluation);

        _context.AuditLogs.Add(new AuditLog
        {
            IdNguoiDung = currentUserId,
            HanhDong = "CREATE_EVALUATION",
            LoaiDoiTuong = "HOP_DONG",
            IdDoiTuong = hopDong.Id,
            DuLieuCu = null,
            DuLieuMoi = $"DiemDanhGia={request.DiemDanhGia}; IdXe={idXe}",
            MoTa = $"Khách hàng đánh giá hợp đồng #{hopDong.Id}.",
            ThoiGian = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return MapToResponse(evaluation);
    }

    // =========================================================
    // LẤY ĐÁNH GIÁ THEO ID
    // =========================================================
    public async Task<EvaluationResponse?> GetByIdAsync(int id)
    {
        if (id <= 0)
            return null;

        var evaluation =
            await _context.DanhGias
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == id);

        return evaluation == null
            ? null
            : MapToResponse(evaluation);
    }

    // =========================================================
    // LẤY ĐÁNH GIÁ THEO HỢP ĐỒNG
    // =========================================================
    public async Task<EvaluationResponse?> GetByContractIdAsync(
        int idHopDong)
    {
        if (idHopDong <= 0)
            return null;

        var evaluation =
            await _context.DanhGias
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.IdHopDong == idHopDong);

        return evaluation == null
            ? null
            : MapToResponse(evaluation);
    }

    // =========================================================
    // KHÁCH HÀNG XEM ĐÁNH GIÁ CỦA CHÍNH HỢP ĐỒNG MÌNH
    // =========================================================
    public async Task<EvaluationResponse?> GetMyByContractIdAsync(
        int idHopDong,
        int currentUserId)
    {
        if (idHopDong <= 0 || currentUserId <= 0)
            return null;

        var hopDong = await _context.HopDongs
            .AsNoTracking()
            .Include(x => x.IdYeuCauThueNavigation)
                .ThenInclude(x => x.IdKhachHangNavigation)
            .FirstOrDefaultAsync(x => x.Id == idHopDong);

        if (hopDong == null)
            throw new KeyNotFoundException("Không tìm thấy hợp đồng.");

        var khachHang = hopDong.IdYeuCauThueNavigation?.IdKhachHangNavigation;
        if (khachHang == null || khachHang.IdNguoiDung != currentUserId)
            throw new UnauthorizedAccessException("Bạn không có quyền xem đánh giá của hợp đồng này.");

        var evaluation = await _context.DanhGias
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdHopDong == idHopDong);

        return evaluation == null ? null : MapToResponse(evaluation);
    }

    // =========================================================
    // LẤY TẤT CẢ ĐÁNH GIÁ
    // =========================================================
    public async Task<List<EvaluationResponse>> GetAllAsync()
    {
        var evaluations =
            await _context.DanhGias
                .AsNoTracking()
                .Include(x => x.IdKhachHangNavigation)
                    .ThenInclude(x => x.IdNguoiDungNavigation)
                .OrderByDescending(x => x.ThoiGianTao)
                .ToListAsync();

        return evaluations
            .Select(MapToResponse)
            .ToList();
    }

    // =========================================================
    // CÔNG KHAI: ĐÁNH GIÁ ĐANG HIỂN THỊ THEO XE
    // =========================================================
    public async Task<List<EvaluationResponse>> GetVisibleByVehicleIdAsync(int idXe)
    {
        if (idXe <= 0) return new List<EvaluationResponse>();

        var evaluations = await _context.DanhGias
            .AsNoTracking()
            .Include(x => x.IdKhachHangNavigation)
                .ThenInclude(x => x.IdNguoiDungNavigation)
            .Where(x => x.IdXe == idXe && x.HienThi)
            .OrderByDescending(x => x.ThoiGianTao)
            .ToListAsync();

        return evaluations.Select(MapToResponse).ToList();
    }

    // =========================================================
    // ADMIN ẨN / HIỆN ĐÁNH GIÁ
    // Không xóa dữ liệu để bảo toàn lịch sử.
    // =========================================================
    public async Task<EvaluationResponse> SetVisibilityAsync(
        int id, bool hienThi, int currentUserId)
    {
        var evaluation = await _context.DanhGias
            .Include(x => x.IdKhachHangNavigation)
                .ThenInclude(x => x.IdNguoiDungNavigation)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (evaluation == null)
            throw new KeyNotFoundException("Không tìm thấy đánh giá.");

        bool old = evaluation.HienThi;
        evaluation.HienThi = hienThi;

        _context.AuditLogs.Add(new AuditLog
        {
            IdNguoiDung = currentUserId,
            HanhDong = hienThi ? "SHOW_EVALUATION" : "HIDE_EVALUATION",
            LoaiDoiTuong = "DANH_GIA",
            IdDoiTuong = evaluation.Id,
            DuLieuCu = $"HienThi={old}",
            DuLieuMoi = $"HienThi={hienThi}",
            MoTa = hienThi
                ? $"Admin hiển thị lại đánh giá #{evaluation.Id}."
                : $"Admin ẩn đánh giá #{evaluation.Id} khỏi trang chi tiết xe.",
            ThoiGian = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return MapToResponse(evaluation);
    }

    // =========================================================
    // ENTITY -> DTO
    // =========================================================
    private static EvaluationResponse MapToResponse(
        DanhGia evaluation)
    {
        return new EvaluationResponse
        {
            Id = evaluation.Id,
            IdHopDong = evaluation.IdHopDong,
            IdKhachHang = evaluation.IdKhachHang,
            IdXe = evaluation.IdXe,
            DiemDanhGia = evaluation.DiemDanhGia,
            NhanXet = evaluation.NhanXet,
            ThoiGianTao = evaluation.ThoiGianTao,
            HienThi = evaluation.HienThi,
            TenKhachHang = evaluation.IdKhachHangNavigation?.IdNguoiDungNavigation?.HoTen
        };
    }
}