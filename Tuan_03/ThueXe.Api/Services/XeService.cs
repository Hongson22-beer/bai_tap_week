using Microsoft.EntityFrameworkCore;
using ThueXe.Api.Data;
using ThueXe.Api.Dtos;
using ThueXe.Api.Models;

namespace ThueXe.Api.Services;

public sealed class XeService(AppDbContext db) : IXeService
{
    public async Task<IReadOnlyList<XeResponseDto>> GetAllAsync(CancellationToken ct) =>
        await db.Xes.AsNoTracking()
            .OrderBy(x => x.BienSoXe)
            .Select(x => new XeResponseDto(
                x.Id, x.IdHangXe, x.HangXe.TenHang,
                x.IdLoaiXe, x.LoaiXe.TenLoai,
                x.BienSoXe, x.MauXe, x.NamSanXuat,
                x.DonGiaNgay, x.TrangThai, x.MoTa))
            .ToListAsync(ct);

    public async Task<XeResponseDto?> GetByIdAsync(int id, CancellationToken ct) =>
        await db.Xes.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new XeResponseDto(
                x.Id, x.IdHangXe, x.HangXe.TenHang,
                x.IdLoaiXe, x.LoaiXe.TenLoai,
                x.BienSoXe, x.MauXe, x.NamSanXuat,
                x.DonGiaNgay, x.TrangThai, x.MoTa))
            .SingleOrDefaultAsync(ct);

    public async Task<XeResponseDto> CreateAsync(XeCreateDto dto, CancellationToken ct)
    {
        if (!await db.HangXes.AnyAsync(h => h.Id == dto.IdHangXe, ct))
            throw new KeyNotFoundException("Hãng xe không tồn tại.");

        if (!await db.LoaiXes.AnyAsync(l => l.Id == dto.IdLoaiXe, ct))
            throw new KeyNotFoundException("Loại xe không tồn tại.");

        var bienSo = dto.BienSoXe.Trim().ToUpperInvariant();
        if (await db.Xes.AnyAsync(x => x.BienSoXe == bienSo, ct))
            throw new InvalidOperationException("Biển số xe đã tồn tại trong hệ thống (BR-02).");

        var entity = new Xe
        {
            IdHangXe = dto.IdHangXe,
            IdLoaiXe = dto.IdLoaiXe,
            BienSoXe = bienSo,
            MauXe = dto.MauXe?.Trim(),
            NamSanXuat = dto.NamSanXuat,
            DonGiaNgay = dto.DonGiaNgay,
            TrangThai = string.IsNullOrWhiteSpace(dto.TrangThai) ? "AVAILABLE" : dto.TrangThai.Trim().ToUpperInvariant(),
            MoTa = dto.MoTa?.Trim()
        };

        db.Xes.Add(entity);
        await db.SaveChangesAsync(ct);

        return (await GetByIdAsync(entity.Id, ct))!;
    }

    public async Task<XeResponseDto?> UpdateAsync(int id, XeUpdateDto dto, CancellationToken ct)
    {
        var entity = await db.Xes.FindAsync([id], ct);
        if (entity is null) return null;

        if (!await db.HangXes.AnyAsync(h => h.Id == dto.IdHangXe, ct))
            throw new KeyNotFoundException("Hãng xe không tồn tại.");

        if (!await db.LoaiXes.AnyAsync(l => l.Id == dto.IdLoaiXe, ct))
            throw new KeyNotFoundException("Loại xe không tồn tại.");

        var bienSo = dto.BienSoXe.Trim().ToUpperInvariant();
        if (await db.Xes.AnyAsync(x => x.Id != id && x.BienSoXe == bienSo, ct))
            throw new InvalidOperationException("Biển số xe đã tồn tại trên xe khác (BR-02).");

        entity.IdHangXe = dto.IdHangXe;
        entity.IdLoaiXe = dto.IdLoaiXe;
        entity.BienSoXe = bienSo;
        entity.MauXe = dto.MauXe?.Trim();
        entity.NamSanXuat = dto.NamSanXuat;
        entity.DonGiaNgay = dto.DonGiaNgay;
        entity.TrangThai = dto.TrangThai.Trim().ToUpperInvariant();
        entity.MoTa = dto.MoTa?.Trim();

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var entity = await db.Xes.FindAsync([id], ct);
        if (entity is null) return false;

        db.Xes.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}