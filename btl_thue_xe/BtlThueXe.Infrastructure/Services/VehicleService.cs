using BtlThueXe.Core.DTOs.Vehicles;
using BtlThueXe.Core.Interfaces;
using BtlThueXe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BtlThueXe.Infrastructure.Services;

public class VehicleService : IVehicleService
{
    private readonly ApplicationDbContext _context;

    public VehicleService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<VehicleResponseDto>> GetAllAsync()
    {
        return await _context.Xes
            .Include(x => x.IdHangXeNavigation)
            .Include(x => x.IdLoaiXeNavigation)
            .Select(x => new VehicleResponseDto
            {
                Id = x.Id,
                BienSoXe = x.BienSoXe,
                MauXe = x.MauXe,
                NamSanXuat = x.NamSanXuat,
                DonGiaNgay = x.DonGiaNgay,
                TrangThai = x.TrangThai,
                HangXe = x.IdHangXeNavigation.Ten,
                LoaiXe = x.IdLoaiXeNavigation.Ten,
                SoCho = x.IdLoaiXeNavigation.SoCho,
                MoTa = x.MoTa
            })
            .ToListAsync();
    }

    public async Task<VehicleResponseDto?> GetByIdAsync(int id)
    {
        var x = await _context.Xes
            .Include(x => x.IdHangXeNavigation)
            .Include(x => x.IdLoaiXeNavigation)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (x == null) return null;

        return new VehicleResponseDto
        {
            Id = x.Id,
            BienSoXe = x.BienSoXe,
            MauXe = x.MauXe,
            NamSanXuat = x.NamSanXuat,
            DonGiaNgay = x.DonGiaNgay,
            TrangThai = x.TrangThai,
            HangXe = x.IdHangXeNavigation.Ten,
            LoaiXe = x.IdLoaiXeNavigation.Ten,
            SoCho = x.IdLoaiXeNavigation.SoCho,
            MoTa = x.MoTa
        };
    }

    public async Task<List<VehicleResponseDto>> GetAvailableVehiclesAsync(CheckAvailableQueryDto query)
    {
        // BR-03: Thời gian trả dự kiến phải lớn hơn thời gian nhận
        if (query.ThoiGianTra <= query.ThoiGianNhan)
            throw new Exception("Thời gian trả phải sau thời gian nhận.");

        // DRV-01 & BR-05: Lấy danh sách ID các xe bị trùng lịch
        var busyVehicleIds = await _context.YeuCauThues
            .Where(r => r.TrangThai == "APPROVED" || r.TrangThai == "PENDING" || r.TrangThai == "CONFIRMED")
            .Where(r => !(query.ThoiGianTra <= r.ThoiGianNhan || query.ThoiGianNhan >= r.ThoiGianTraDuKien))
            .Select(r => r.IdXe)
            .Distinct()
            .ToListAsync();

        // BR-04 & BR-35: Chỉ lấy xe AVAILABLE và không bị trùng lịch
        var queryable = _context.Xes
            .Include(x => x.IdHangXeNavigation)
            .Include(x => x.IdLoaiXeNavigation)
            .Where(x => x.TrangThai == "AVAILABLE" && !busyVehicleIds.Contains(x.Id));

        if (query.IdLoaiXe.HasValue)
            queryable = queryable.Where(x => x.IdLoaiXe == query.IdLoaiXe.Value);

        if (query.IdHangXe.HasValue)
            queryable = queryable.Where(x => x.IdHangXe == query.IdHangXe.Value);

        return await queryable.Select(x => new VehicleResponseDto
        {
            Id = x.Id,
            BienSoXe = x.BienSoXe,
            MauXe = x.MauXe,
            NamSanXuat = x.NamSanXuat,
            DonGiaNgay = x.DonGiaNgay,
            TrangThai = x.TrangThai,
            HangXe = x.IdHangXeNavigation.Ten,
            LoaiXe = x.IdLoaiXeNavigation.Ten,
            SoCho = x.IdLoaiXeNavigation.SoCho,
            MoTa = x.MoTa
        }).ToListAsync();
    }

    public async Task<VehicleResponseDto> CreateAsync(CreateVehicleDto dto)
    {
        // BR-02: Biển số xe phải duy nhất
        if (await _context.Xes.AnyAsync(x => x.BienSoXe == dto.BienSoXe))
            throw new Exception("Biển số xe đã tồn tại trong hệ thống.");

        var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

        var xe = new Xe
        {
            IdHangXe = dto.IdHangXe,
            IdLoaiXe = dto.IdLoaiXe,
            BienSoXe = dto.BienSoXe,
            MauXe = dto.MauXe,
            NamSanXuat = dto.NamSanXuat,
            DonGiaNgay = dto.DonGiaNgay,
            TrangThai = "AVAILABLE",
            MoTa = dto.MoTa,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Xes.Add(xe);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(xe.Id))!;
    }

    public async Task<VehicleResponseDto> UpdateAsync(int id, UpdateVehicleDto dto)
    {
        var xe = await _context.Xes.FindAsync(id);
        if (xe == null) throw new Exception("Không tìm thấy xe.");

        // BR-02: Kiểm tra biển số duy nhất nếu có đổi biển
        if (xe.BienSoXe != dto.BienSoXe && await _context.Xes.AnyAsync(x => x.BienSoXe == dto.BienSoXe))
            throw new Exception("Biển số xe đã tồn tại.");

        xe.IdHangXe = dto.IdHangXe;
        xe.IdLoaiXe = dto.IdLoaiXe;
        xe.BienSoXe = dto.BienSoXe;
        xe.MauXe = dto.MauXe;
        xe.NamSanXuat = dto.NamSanXuat;
        xe.DonGiaNgay = dto.DonGiaNgay;
        xe.MoTa = dto.MoTa;
        xe.UpdatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

        await _context.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

   public async Task<VehicleResponseDto> ChangeStatusAsync(int id, UpdateVehicleStatusDto dto, int idNguoiDung)
    {
        var xe = await _context.Xes.FindAsync(id);
        if (xe == null) throw new Exception("Không tìm thấy xe.");

        var validStatuses = new[] { "AVAILABLE", "MAINTENANCE", "INACTIVE" };
        if (!validStatuses.Contains(dto.TrangThai.ToUpper()))
            throw new Exception("Trạng thái xe không hợp lệ (Chỉ chấp nhận AVAILABLE, MAINTENANCE, INACTIVE).");

        string trangThaiCu = xe.TrangThai;
        xe.TrangThai = dto.TrangThai.ToUpper();
        xe.UpdatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

        // Ghi nhận lịch sử thay đổi trạng thái xe khớp với bảng lich_su_trang_thai_xe
        var lichSu = new LichSuTrangThaiXe
        {
            IdXe = xe.Id,
            TrangThaiCu = trangThaiCu,
            TrangThaiMoi = xe.TrangThai,
            LyDo = dto.GhiChu,
            ThoiGianThayDoi = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
        };
        _context.LichSuTrangThaiXes.Add(lichSu);

        await _context.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<List<VehicleStatusHistoryDto>> GetStatusHistoryAsync(int id)
    {
        return await _context.LichSuTrangThaiXes
            .Where(h => h.IdXe == id)
            .OrderByDescending(h => h.ThoiGianThayDoi)
            .Select(h => new VehicleStatusHistoryDto
            {
                Id = h.Id,
                TrangThaiCu = h.TrangThaiCu,
                TrangThaiMoi = h.TrangThaiMoi,
                LyDo = h.LyDo,
                CreatedAt = h.ThoiGianThayDoi
            })
            .ToListAsync();
    }
}