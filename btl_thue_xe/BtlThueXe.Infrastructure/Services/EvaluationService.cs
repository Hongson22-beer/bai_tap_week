using BtlThueXe.Core.DTOs.Evaluations;
using BtlThueXe.Core.Entities;
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
            await _context.DanhGia
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

        _context.DanhGia.Add(evaluation);

        await _context.SaveChangesAsync();

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
            await _context.DanhGia
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
            await _context.DanhGia
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
            await _context.DanhGia
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