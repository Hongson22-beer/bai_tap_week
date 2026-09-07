using BtlThueXe.Core.DTOs.Evaluations;
using BtlThueXe.Core.DTOs.AuditLogs;
using BtlThueXe.Core.Entities;
using BtlThueXe.Core.Interfaces;
using BtlThueXe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BtlThueXe.Infrastructure.Services;

public class EvaluationService : IEvaluationService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public EvaluationService(
        ApplicationDbContext context,
        IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    // =========================================================
    // TẠO ĐÁNH GIÁ
    // =========================================================
    public async Task<EvaluationResponse> CreateAsync(
        CreateEvaluationRequest request)
    {
        if (request.IdHopDong <= 0)
        {
            throw new ArgumentException(
                "ID hợp đồng không hợp lệ.");
        }

        if (request.IdKhachHang <= 0)
        {
            throw new ArgumentException(
                "ID khách hàng không hợp lệ.");
        }

        if (request.IdXe.HasValue &&
            request.IdXe.Value <= 0)
        {
            throw new ArgumentException(
                "ID xe không hợp lệ.");
        }

        if (request.DiemDanhGia < 1 ||
            request.DiemDanhGia > 5)
        {
            throw new ArgumentException(
                "Điểm đánh giá phải từ 1 đến 5.");
        }

        // Một hợp đồng chỉ được đánh giá một lần
        var evaluationExists =
            await _context.DanhGias
                .AnyAsync(x =>
                    x.IdHopDong == request.IdHopDong);

        if (evaluationExists)
        {
            throw new ArgumentException(
                "Hợp đồng này đã được đánh giá.");
        }

        var evaluation = new DanhGia
        {
            IdHopDong =
                request.IdHopDong,

            IdKhachHang =
                request.IdKhachHang,

            IdXe =
                request.IdXe,

            DiemDanhGia =
                request.DiemDanhGia,

            NhanXet =
                string.IsNullOrWhiteSpace(request.NhanXet)
                    ? null
                    : request.NhanXet.Trim(),

            ThoiGianTao =
                DateTime.UtcNow
        };

        // =====================================================
        // LƯU ĐÁNH GIÁ
        // =====================================================
        _context.DanhGias.Add(evaluation);

        await _context.SaveChangesAsync();

        // =====================================================
        // GHI AUDIT LOG TỰ ĐỘNG
        // =====================================================
        var duLieuMoi = JsonSerializer.Serialize(new
        {
            idHopDong =
                evaluation.IdHopDong,

            idKhachHang =
                evaluation.IdKhachHang,

            idXe =
                evaluation.IdXe,

            diemDanhGia =
                evaluation.DiemDanhGia,

            nhanXet =
                evaluation.NhanXet
        });

        await _auditLogService.CreateAsync(
            new CreateAuditLogRequest
            {
                /*
                 * IdKhachHang KHÔNG phải IdNguoiDung.
                 * Sau khi merge Auth/RBAC sẽ lấy
                 * id_nguoi_dung từ JWT.
                 */
                IdNguoiDung =
                    null,

                HanhDong =
                    "EVALUATION_CREATED",

                LoaiDoiTuong =
                    "EVALUATION",

                IdDoiTuong =
                    evaluation.Id,

                DuLieuCu =
                    null,

                DuLieuMoi =
                    duLieuMoi,

                MoTa =
                    $"Khách hàng #{evaluation.IdKhachHang} " +
                    $"đánh giá hợp đồng #{evaluation.IdHopDong} " +
                    $"{evaluation.DiemDanhGia} sao",

                IpAddress =
                    null
            });

        return MapToResponse(evaluation);
    }

    // =========================================================
    // LẤY ĐÁNH GIÁ THEO ID
    // =========================================================
    public async Task<EvaluationResponse?> GetByIdAsync(
        int id)
    {
        if (id <= 0)
            return null;

        var evaluation =
            await _context.DanhGias
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == id);

        if (evaluation == null)
            return null;

        return MapToResponse(evaluation);
    }

    // =========================================================
    // LẤY ĐÁNH GIÁ THEO HỢP ĐỒNG
    // =========================================================
    public async Task<EvaluationResponse?>
        GetByContractIdAsync(int idHopDong)
    {
        if (idHopDong <= 0)
            return null;

        var evaluation =
            await _context.DanhGias
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.IdHopDong == idHopDong);

        if (evaluation == null)
            return null;

        return MapToResponse(evaluation);
    }

    // =========================================================
    // DANH SÁCH TẤT CẢ ĐÁNH GIÁ
    // =========================================================
    public async Task<List<EvaluationResponse>>
        GetAllAsync()
    {
        var evaluations =
            await _context.DanhGias
                .AsNoTracking()
                .OrderByDescending(
                    x => x.ThoiGianTao)
                .ToListAsync();

        return evaluations
            .Select(MapToResponse)
            .ToList();
    }

    // =========================================================
    // ENTITY -> DTO
    // =========================================================
    private static EvaluationResponse MapToResponse(
        DanhGia evaluation)
    {
        return new EvaluationResponse
        {
            Id =
                evaluation.Id,

            IdHopDong =
                evaluation.IdHopDong,

            IdKhachHang =
                evaluation.IdKhachHang,

            IdXe =
                evaluation.IdXe,

            DiemDanhGia =
                evaluation.DiemDanhGia,

            NhanXet =
                evaluation.NhanXet,

            ThoiGianTao =
                evaluation.ThoiGianTao
        };
    }
}